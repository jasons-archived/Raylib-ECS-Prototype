using System;
using DumDum.Bcl._internal;
using DumDum.Bcl.Diagnostics;

namespace DumDum.Bcl.Collections._unused
{
	/// <summary>
	/// Functionality to automatically resize an array.
	/// Grows to 2x when out of space.  shrinks to /2 when at /4 capacity.
	/// </summary>
	/// <typeparam name="TItem"></typeparam>
	[NotThreadSafe]
	public class ResizableArray<TItem>
	{
		private TItem[] _storage;
		public int Length { get; protected set; }
		private RaceCheck _check;

		public ResizableArray(int length = 0)
		{
			_storage = new TItem[length];
			Length = length;
		}

		public void Clear()
		{
			_check.Enter();
			Array.Clear(_storage, 0, Length);
			Length = 0;
			_check.Exit();
		}

		public TItem this[int index]
		{
			get
			{
				//_check.Poke();
				__CHECKED.Throw(index < Length, "out of bounds.  the index you are using is not allocated");
				return _storage[index];
			}
			set
			{
				__CHECKED.Throw(index < Length, "out of bounds.  the index you are using is not allocated");
				_storage[index] = value;
			}
		}

		public TItem GetOrSet(int index, Func<TItem> newCtor)
		{
			if (index >= Length)
			{
				Grow(index - Length);
				this[index] = newCtor();
			}
			return this[index];
		}

		/// <summary>
		/// returns the next available index
		/// </summary>
		/// <returns></returns>
		public int Grow(int count)
		{
			_check.Enter();
			if ((count + Length) > _storage.Length)
			{
				var newCapacity = Math.Max(_storage.Length * 2, Length + count);
				this._SetCapacity(newCapacity);
			}

			var current = Length;
			Length += count;
			_check.Exit();
			return current;
		}

		public void Shrink(int count)
		{
			_check.Enter();
			Length -= count;
			Array.Clear(_storage, Length, count);
			this._TryPack();
			_check.Exit();
		}

		public void SetLength(int newLength)
		{
			_check.Enter();
			if (newLength < _storage.Length)
			{
				this._SetCapacity(newLength);
			}
			Length = newLength;
			_check.Exit();
		}

		private void _TryPack()
		{
			if (_storage.Length > __Config.ResizableArray_minShrinkSize && (Length < _storage.Length / 4))
			{
				var newCapacity = Math.Max(Length * 2, __Config.ResizableArray_minShrinkSize);
				this._SetCapacity(newCapacity);
			}
		}


		/// <summary>
		/// preallocates the capacity specified
		/// <para>does NOT increment count, just pre-alloctes to avoid resizing the internal storage array</para>
		/// </summary>
		/// <param name="capacity"></param>
		private void _SetCapacity(int capacity)
		{
			Array.Resize(ref _storage, capacity);
		}

	}
}