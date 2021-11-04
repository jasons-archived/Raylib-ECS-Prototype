// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Generic;
using NotNot.Bcl.Diagnostics;

namespace NotNot.Bcl.Collections._unused
{
	/// <summary>
	/// array optimized for struct data storage. thread safe alloc/dealloc.
	/// <para>If you want to store objects, see the SlotStore class</para>
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	[ThreadSafety(ThreadSituation.Query, ThreadSituation.ReadExisting, ThreadSituation.Remove)]
	public class StructSlotArray<TData> //where TData : struct
	{
		public int Count => this.Capacity - this._freeSlots.Count;
		public int Capacity => this._storage.Length;
		public int FreeCount => this._freeSlots.Count;

		/// <summary>
		/// can be used to detect when items are alloc or dealloc
		/// </summary>
		public int Version { get; private set; }


#if CHECKED
		private System.Collections.Concurrent.ConcurrentDictionary<int, bool> _CHECKED_allocationTracker;
#endif

		/// <summary>
		/// available for direct access to the storage array.
		/// if you can ensure no other threads are writing, this can be resized manually. 
		/// </summary>
		public TData[] _storage;
		private readonly Stack<int> _freeSlots;
		//private readonly MinHeap _freeSlots;

		private readonly object _lock = new();


		public StructSlotArray(int capacity)
		{
			this._storage = new TData[capacity];
			this._freeSlots = new(capacity);			
#if CHECKED
			this._CHECKED_allocationTracker = new();
#endif
			for (var i = 0; i < capacity; i++)
			{
				//this._freeSlots.Push(capacity - i);
				this._freeSlots.Push(i);
			}
		}

		public ref TData GetRef(int slot)
		{
#if CHECKED
			__CHECKED.Throw(this._CHECKED_allocationTracker.ContainsKey(slot), "slot is not allocated and you are using it");

#endif

			return ref this._storage[slot];
		}
		public ref TData this[int slot]
		{
			get
			{
				return ref this.GetRef(slot);
			}
		}



		public int Alloc(ref TData data)
		{
			lock (this._lock)
			{
				__DEBUG.Throw(FreeCount > 0,"no capacity remaining");

				this.Version++;
				var slot = this._freeSlots.Pop();
#if CHECKED
				__CHECKED.Throw(this._CHECKED_allocationTracker.TryAdd(slot, true), "slot already allocated");
#endif
				this._storage[slot] = data;
				return slot;
			}
		}

		public int Alloc()
		{
			lock (this._lock)
			{
				__DEBUG.Throw(FreeCount > 0, "no capacity remaining");

				this.Version++;
				var slot = this._freeSlots.Pop();
#if CHECKED
				__CHECKED.Throw(this._CHECKED_allocationTracker.TryAdd(slot, true), "slot already allocated");
#endif
				return slot;
			}
		}
		public void Alloc(Span<int> toFill)
		{
#if CHECKED
			toFill.Clear();
#endif
			lock (this._lock)
			{
				__DEBUG.Throw(FreeCount >= toFill.Length, "no capacity remaining");

				this.Version++;

				for(var i = 0; i < toFill.Length; i++)
				{
					var slot = this._freeSlots.Pop();
#if CHECKED
					__CHECKED.Throw(this._CHECKED_allocationTracker.TryAdd(slot, true), "slot already allocated");
#endif
					toFill[i] = slot;

				}
				return;
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
				this._storage[slot] = default!;
			}
		}
		public void Free(Span<int> toFree)
		{
			lock (this._lock)
			{
				this.Version++;
				for(var i = 0; i < toFree.Length; i++)
				{
					var slot = toFree[i];

#if CHECKED
					__CHECKED.Throw(this._CHECKED_allocationTracker.TryRemove(slot, out var temp), "slot is not allocated but trying to remove");
#endif
					this._freeSlots.Push(slot);
					this._storage[slot] = default!;
				}
			}
		}
	}
}
//


//If I need to write it myself, I am thinking I need to use a normal `List<int>` that's sorted, and wrap it in a stack-like class.   When a item is pulled off the top I decrement a `int topIndex`  and when items are put back on, they go above that, incrementing a `int addCount` parameter..   Next time an item is pulled off I just sort from `topIndex` to `topIndex+addedCount` before I give an item back.  I
