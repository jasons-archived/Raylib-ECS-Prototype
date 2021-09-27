using System.Collections.Generic;
using DumDum.Bcl.Diagnostics;

namespace DumDum.Bcl.Collections._unused
{
	/// <summary>
	/// fixed size array optimized for struct data storage. thread safe alloc/dealloc.
	/// <para>Not resizable as is optimized for storage of large numbers of structs</para>
	/// <para>If you want to store objects, see the SlotStore class</para>
	/// </summary>
	/// <typeparam name="TData"></typeparam>
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

		private readonly TData[] _storage;
		private readonly Stack<int> _freeSlots;

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
				this._freeSlots.Push(capacity - i);
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
				this.Version++;
				var slot = this._freeSlots.Pop();
				#if CHECKED
				__CHECKED.Throw(this._CHECKED_allocationTracker.TryAdd(slot, true), "slot already allocated");
#endif
				this._storage[slot] = data;
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
				this._storage[slot] = default;
			}
		}

	}
}