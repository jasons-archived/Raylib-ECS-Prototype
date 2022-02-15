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

///// <summary>
///// Docs for class
///// </summary>
//public class MyClass
//{
//	/// <summary>
//	/// Docs for field
//	/// </summary>
//	public int myVal;
//	/// <summary>
//	/// Dupe Docs for class
//	/// </summary>
//	/// <param name="myVal">dupe docs for field</param>
//	public MyClass(int myVal)
//	{
//		this.myVal = myVal;
//	}
//}

///// <summary>
///// docs for class
///// </summary>
///// <param name="myVal">docs for myVal</param>
//public record class MyRecordClass(int myVal)
//{
//	public MyRecordClass(double other2) : this()
//	{

//	}

//	/// <summary>
//	/// docs for otherVal
//	/// </summary>
//	public int otherVal;
//}

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
	/// helper to dispose of data objects stored internally.
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
#if CHECKED
		//dispose of the data item to help ensure it's not accidentally reused
		_disposeHelper(toRecycle);
		return;
#endif
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
