// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using NotNot.Bcl.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NotNot.Bcl.Threading;



/// <summary>
/// an async Message Channel specialized for sending inter-system messages, aggregrated by simulation frame
/// <para>allows multiple Writers, Single Reader.  Producer/Consumer pattern.</para>
/// </summary>
/// <typeparam name="T"></typeparam>
public class FrameDataChannel<T> : DisposeGuard
{
	/// <summary>
	/// PRIVATE helper: wraps the queue with some simple validation logic (make sure count doesn't change when inside the channel)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	private struct _FramePacketWrapper<T>
	{

		private ConcurrentQueue<T> framePacket;
		private int queueCount;


		public _FramePacketWrapper(ConcurrentQueue<T> framePacket)
		{
			this.framePacket = framePacket;
			queueCount = framePacket.Count;
		}

		public ConcurrentQueue<T> getQueue()
		{
			//__DEBUG.Throw(_frameVersion == currentFrameVersion,"race condition, frames do not match.  is this framePacket being used improperly?  use-after-enqueue or use-after-recycle");
			VerifyPacket();
			return framePacket;
		}

		public void VerifyPacket()
		{
			__DEBUG.Throw(framePacket != null, "disposed or not initalized");
			__DEBUG.Throw(framePacket.Count == queueCount,
				"race condition, queue count at dequeue time does not match count when created.  is this framePacket being used improperly?  use-after-enqueue or use-after-recycle");
		}

		public void Clear()
		{
			VerifyPacket();
			//_frameVersion = -1;
			queueCount = 0;
			framePacket.Clear();
		}
	}


	private RecycleChannel<_FramePacketWrapper<T>> _recycleChannel;
	/// <summary>
	/// debug logic: checks to make sure a thread isn't writing while the end frame swap is occuring.
	/// </summary>
	private bool _allowWriteWhileEndingFrame;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="maxFrames">how many simulation frames worth of data to keep, if the reader systems don't process them in a timely fashion.
	/// <para>for example, a value of 1 means that only 1 FramePacket (queue) is stored, allowing reader systems to never be more than 1 frame out of data from the writer systems.
	/// If the reader gets further out of date and 2 frames finish, the oldest frame will be discarded</para></param>
	/// <param name="allowWriteWhileEndingFrame"></param>
	public FrameDataChannel(int maxFrames = 1, bool allowWriteWhileEndingFrame=false)//, int maxPacketsPerFrame = int.MaxValue)
	{
		_allowWriteWhileEndingFrame = allowWriteWhileEndingFrame;
		_recycleChannel = new RecycleChannel<_FramePacketWrapper<T>>(maxFrames,
			newFactory: () => new(new()),
			cleanHelper: (framePacket) =>
			{
				//this FramePacketChannel class should already clear, so lets just check to verify it's clear
				__DEBUG.Throw(framePacket.getQueue().Count == 0);
				//framePacket.Clear();
				return framePacket;
			},
			disposeHelper: (framePacket) =>
		   {
			   framePacket.Clear();
		   }
			);
	}

	private ConcurrentQueue<T> _currentFramePacket = new();
	private object _writeLock = new object();

	/// <summary>
	/// For the current frame being written, how many data items are written to the FramePacket (queue)
	/// </summary>
	public int CurrentFramePacketDataCount => _currentFramePacket.Count;


	/// <summary>
	/// write data items associated with the current frame.  these will be bundled together as a FramePacket (queue) and sent to the reader system
	/// </summary>
	/// <param name="dataItem"></param>
	public void WriteFramePacketData(T dataItem)
	{
		lock (_writeLock)
		{
			_currentFramePacket.Enqueue(dataItem);
		}
	}

	/// <summary>
	/// signal that current frame is finished, moving current FramePacket into the channel for reading by consumer systems.
	/// </summary>
	public void EndFrameAndEnqueue()
	{
		if (_allowWriteWhileEndingFrame == false)
		{
			if (Monitor.TryEnter(_writeLock) == false)
			{
				__ERROR.Throw(false, "could not enter write lock, another thread is writing");
			}
		}
		else
		{
			Monitor.Enter(_writeLock);
		}

		try
		{
			_FramePacketWrapper<T> toEnqueue = new _FramePacketWrapper<T>(_currentFramePacket);
			_recycleChannel.WriteAndSwap(toEnqueue, out var recycledPacket);
			_currentFramePacket = recycledPacket.getQueue();
			__DEBUG.Assert(_currentFramePacket.Count == 0);
		}
		finally
		{
			Monitor.Exit(_writeLock);
		}
	}

	/// <summary>
	/// Read the FramePacket (queue) for a previous frame.  The consumer system should call this
	/// <para>Async, blocks until complete</para>
	/// </summary>
	/// <param name="doneFramePacketToRecycle">recycle the queue for reuse, to avoid GC pressure</param>
	/// <returns></returns>
	public async ValueTask<ConcurrentQueue<T>> ReadFrame(ConcurrentQueue<T> doneFramePacketToRecycle)
	{
		//if (doneFramePacketToRecycle == null)
		//{
		//	doneFramePacketToRecycle = new();
		//}

		__ERROR.Throw(doneFramePacketToRecycle.Count == 0,"expect queue being recycled to be clear/unused");
		var recyclePacket = new _FramePacketWrapper<T>(doneFramePacketToRecycle);
		var dequeuedPacket = await _recycleChannel.ReadAndSwap(recyclePacket);
		return dequeuedPacket.getQueue();
	}

	public bool TryReadFrame(out ConcurrentQueue<T> framePacket)
	{
		if(_recycleChannel.TryRead(out var queueWrapper))
		{
			framePacket = queueWrapper.getQueue();
			return true;
		}
		framePacket = null;
		return false;
	}

	/// <summary>
	/// If you have a FramePacket(queue) that can be recycled can put it here
	/// </summary>
	/// <param name="doneFramePacketToRecycle"></param>
	public void Recycle(ConcurrentQueue<T> doneFramePacketToRecycle)
	{
		__ERROR.Throw(doneFramePacketToRecycle.Count == 0, "expect queue being recycled to be clear/unused");
		_recycleChannel.Recycle(new _FramePacketWrapper<T>(doneFramePacketToRecycle));
	}

}


/// <summary>
/// A custom System.Threading.Channel that recycles the data objects for reuse
/// </summary>
/// <typeparam name="T"></typeparam>
public class RecycleChannel<T> : DisposeGuard
{

	public Channel<T> _channel;
	//public ChannelWriter<T> _writer;
	//public ChannelReader<T> _reader;
	/// <summary>
	/// How many pending items this channel can hold.  If more than this are enqueued, the oldest is replaced.
	/// </summary>
	public int _capacity;
	/// <summary>
	/// a helper to create new data items.  These helpers are needed because we allow custom generic data items, so we don't know their interface.
	/// </summary>
	public Func<T> _newFactory;
	/// <summary>
	/// a custom callback to recycle the data object.  usually just .Clear() it
	/// </summary>
	private Func<T, T> _cleanHelper;
	/// <summary>
	/// helper to dispose of data objects stored internally.  called when disposed and items still exist in the channel.
	/// </summary>
	public Action<T> _disposeHelper;


	/// <summary>
	/// unused data items
	/// </summary>
	public ConcurrentQueue<T> _recycled = new();

	public RecycleChannel(int capacity, Func<T> newFactory, Func<T, T> cleanHelper, Action<T> disposeHelper)
	{
		_newFactory = newFactory;
		_cleanHelper = cleanHelper;
		_capacity = capacity;
		_channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
		{
			AllowSynchronousContinuations = true,
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
			SingleWriter = true,
		});
		_disposeHelper = disposeHelper;
	}


	private object _writeLock = new();

	public void WriteAndSwap(T toEnqueue, out T recycled)
	{
		lock (_writeLock)
		{
			if (IsDisposed)
			{
				recycled = _cleanHelper(toEnqueue);
				return;
			}
			if (_channel.Writer.TryWrite(toEnqueue))
			{
				//something to return
				if (_recycled.TryDequeue(out recycled))
				{
					return;
				}
				recycled = _newFactory();
				return;
			}

			//could not write.  channel is full. need to dequeue one and try again
			{
				//get something to return
				if (_channel.Reader.TryRead(out var toReturn))
				{
					//sacrificing oldest enqueued so clean it before returning it
					_cleanHelper(toReturn);
				}
				else
				{
					//a consumer thread may have depleted our channel
					toReturn = _newFactory();
				}

				var result = _channel.Writer.TryWrite(toEnqueue);
				__ERROR.Throw(result,
					"error in this class workflow.   we should never fail writing because this is exculsive write");
				recycled = toReturn;
				return;
			}
		}
	}
	protected override void OnDispose()
	{
		base.OnDispose();
		lock (_writeLock)
		{
			_channel.Writer.Complete();
			_recycled.Clear();
			while (_channel.Reader.TryRead(out var enqueued))
			{
				_cleanHelper(enqueued);
				_disposeHelper(enqueued);
			}
			//_cleanHelper = null;
			_newFactory = null;
		}
	}

	/// <summary>
	/// blocks until a data item is available to read
	/// </summary>
	/// <param name="toRecycle"></param>
	/// <returns></returns>
	public ValueTask<T> ReadAndSwap(T toRecycle)
	{
		Recycle(toRecycle);
		return _channel.Reader.ReadAsync();
	}


	/// <summary>
	/// use if you want to return a value without getting a new one
	/// </summary>
	public void Recycle(T toRecycle)
	{
		//clean it first
		_cleanHelper(toRecycle);
		_recycled.Enqueue(toRecycle);
	}

	/// <summary>
	/// get a value without returning one
	/// </summary>
	/// <param name="freshValue"></param>
	/// <returns></returns>
	public bool TryRead(out T freshValue)
	{
		return _channel.Reader.TryRead(out freshValue);
	}

}
