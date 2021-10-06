using System.Collections.Generic;
using NotNot.Bcl.Diagnostics;

namespace NotNot.Bcl.Collections._unused
{
	/// <summary>
	/// An array backed storage where you can free up individual slots for reuse.    When it runs out of capacity, the backing array will be resized.
	/// <para>thread safe writes and non-blocking reads if not using `ref return` accessors</para>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SlotStore<T>
	{



		private ResizableArray<T> _storage;
		public int Count => _storage.Length - _freeSlots.Count;
		//public int CurrentCapacity => this._storage.Length;
		public int FreeCount => _freeSlots.Count;


		/// <summary>
		/// can be used to detect when items are alloc or dealloc
		/// </summary>
		public int Version { get; private set; }

#if CHECKED
		private System.Collections.Concurrent.ConcurrentDictionary<int, bool> _CHECKED_allocationTracker;
#endif

		private readonly Stack<int> _freeSlots;

		private readonly object _lock = new();


		public SlotStore(int initialCapacity = 10)
		{

			this._storage = new(initialCapacity);
			this._storage.Clear();
			this._freeSlots = new(initialCapacity);
#if CHECKED
			this._CHECKED_allocationTracker = new();
#endif
			for (var i = 0; i < initialCapacity; i++)
			{
				this._freeSlots.Push(initialCapacity - i);
			}
		}

		//public T this[int slot]
		//{
		//	get
		//	{
		//		__CHECKED.Throw(this._CHECKED_allocationTracker.ContainsKey(slot), "slot is not allocated and you are using it");
		//		return _storage[slot];
		//	}
		//	set
		//	{
		//		lock (this._lock)
		//		{
		//			__CHECKED.Throw(this._CHECKED_allocationTracker.ContainsKey(slot), "slot is not allocated and you are using it");
		//			_storage[slot] = value;
		//		}
		//	}
		//}
		public T this[int slot]
		{
			get
			{
#if CHECKED
				__CHECKED.Throw(this._CHECKED_allocationTracker.ContainsKey(slot), "slot is not allocated and you are using it");
#endif
				return _storage[slot];
			}
			set
			{
				lock (this._lock)
				{
					_storage.Set(slot, value);
				}
			}
		}




		public int Alloc(T data)
		{
			var slot = Alloc();
			_storage.Set(slot, data);
			return slot;


		}

		public int Alloc(ref T data)
		{
			var slot = Alloc();
			_storage.Set(slot, data);
			return slot;
		}

		public int Alloc()
		{
			lock (this._lock)
			{
				this.Version++;

				int slot;
				if (this._freeSlots.Count > 0)
				{
					slot = this._freeSlots.Pop();
#if CHECKED
					__CHECKED.Throw(this._CHECKED_allocationTracker.TryAdd(slot, true), "slot already allocated");
#endif
				}
				else
				{
					//need to allocate a new slot
					slot = _storage.Grow(1);
				}
				return slot;
			}

		}

		public void Free(int slot)
		{
			lock (this._lock)
			{
				this.Version++;
#if CHECKED
				__CHECKED.Throw(this._CHECKED_allocationTracker.TryRemove(slot, out var temp), "slot is not allocated but trying to remove");
#endif
				this._freeSlots.Push(slot);
				_storage.Set(slot, default(T));
			}
		}

	}
}