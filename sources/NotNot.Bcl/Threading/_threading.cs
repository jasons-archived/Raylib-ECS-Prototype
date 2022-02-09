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
/// A custom System.Threading.Channel that recycles the data objects for reuse
/// </summary>
/// <typeparam name="T"></typeparam>
public class RecycleChannel<T> : DisposeGuard
{

	public Channel<T> _channel;
	public ChannelWriter<T> _writer;
	public ChannelReader<T> _reader;
	public int _capacity;
	public Func<T> _newFactory;
	private Func<T, T> _cleanHelper;

	public ConcurrentQueue<T> _recycled = new();
	public RecycleChannel(int capacity, Func<T> newFactory, Func<T, T> cleanHelper)
	{
		_newFactory = newFactory;
		_cleanHelper = cleanHelper;
		_capacity = capacity;
		_channel = Channel.CreateBounded<T>(capacity);
		_writer = _channel.Writer;
		_reader = _channel.Reader;
	}


	private object _writeLock = new();
	public T WriteAndSwap(T toEnqueue)
	{
		lock (_writeLock)
		{
			if (IsDisposed)
			{
				return _cleanHelper(toEnqueue);
			}
			if (_writer.TryWrite(toEnqueue))
			{
				//something to return
				if (_recycled.TryDequeue(out var toReturn))
				{
					return toReturn;
				}
				return _newFactory();
			}

			//could not write.  channel is full. need to dequeue one and try again
			{
				//get something to return
				if (_reader.TryRead(out var toReturn))
				{
					//sacrificing oldest enqueued so clean it before returning it
					_cleanHelper(toReturn);
				}
				else
				{
					//a consumer thread may have depleted our channel
					toReturn = _newFactory();
				}

				var result = _writer.TryWrite(toEnqueue);
				__DEBUG.Throw(result,
					"error in this class workflow.   we should never fail writing because this is exculsive write");
				return toReturn;

			}
		}
	}
	protected override void OnDispose()
	{
		base.OnDispose();
		lock (_writeLock)
		{
			_writer.Complete();
			_recycled.Clear();
			while (_reader.TryRead(out var enqueued))
			{
				_cleanHelper(enqueued);
			}
			//_cleanHelper = null;
			_newFactory = null;
		}
	}

	public ValueTask<T> ReadAndSwap(T toRecycle)
	{
		Recycle(toRecycle);
		return _reader.ReadAsync();
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
		return _reader.TryRead(out freshValue);
	}

}
