// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using NotNot.Bcl.Diagnostics;
using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NotNot.Bcl.Collections.Advanced;

namespace NotNot.Bcl;


/// <summary>
/// lolo: a static utils helper
/// </summary>
public unsafe static class __
{



	[ThreadStatic]
	private static Random? _rand;
	//private static ThreadLocal<Random> _rand2 = new(() => new());

	/// <summary>
	/// get a thread-local Random
	/// </summary>
	public static Random Rand
	{
		get
		{
			if (_rand == null)
			{
				_rand = new Random();
			}
			return _rand;
		}
	}

	public static string NameVal<T>(T tuple)
	{
		if (tuple == null)
		{
			return string.Empty;
		}
		return $"{typeof(T).GetProperties()[0].Name}={tuple}";
	}
}


/// <summary>
/// support hashing of multiple items
/// </summary>
/// <remarks>
/// to support hashing for a container object, that might have an arbitrary number of things inside.
/// I want to generate a hash to check if two containers are the "same"  (meaning they store the same contents).
/// </remarks>
public unsafe struct CombinedHash : IComparable<CombinedHash>, IEquatable<CombinedHash>
{
	public const int SIZE = 8;
	/// <summary>
	/// some .GetHashCode() implementations are not psudoRandom, such as for int/long.
	/// so to help prevent hash collisions these values are spread across the int spectrum
	/// </summary>
	private const ulong SALT_INCREMENT = (ulong.MaxValue / SIZE);

	/// <summary>
	/// 
	/// </summary>
	private ulong _compressedHash;
	private fixed uint _storage[SIZE];


	public CombinedHash(Span<int> hashes)
	{
		hashes.Sort();
		var loopSalt = (uint)(uint.MaxValue / hashes.Length / SIZE);


		for (int i = 0; i < hashes.Length; i++)
		{
			uint salt = (uint)(loopSalt * (i / SIZE));
			uint value = (uint)(hashes[i] + salt);
			_storage[i % SIZE] += value;
		}

		_compressedHash = 0;
		for (var i = 0; i < SIZE; i++)
		{
			_compressedHash += (ulong)(_storage[i] + (SALT_INCREMENT * (uint)i));
		}
	}

	public void AccumulateHash(int itemIndex, int hashCode)
	{

	}

	public int CompareTo(CombinedHash other)
	{
		var result = _compressedHash.CompareTo(other._compressedHash);
		if (result == 0)
		{
			for (var i = 0; i < SIZE; i++)
			{
				result = _storage[i].CompareTo(other._storage[i]);
				if (result == 0)
				{
					continue;
				}
				return result;
			}
		}
		return result;
	}

	public bool Equals(CombinedHash other)
	{
		return this.CompareTo(other) == 0;
	}
	public override int GetHashCode()
	{
		return (int)_compressedHash;
	}

	public ulong GetHashCode64()
	{
		return _compressedHash;
	}
}


public static class ParallelFor
{

	private static SpanGuard<(int startInclusive, int endExclusive)> _Range_ComputeBatches(int start, int length, float batchSizeMultipler)
	{
		__ERROR.Throw(batchSizeMultipler >= 0, $"{nameof(batchSizeMultipler)} should be greater or equal to than zero");
		var endExclusive = start + length;

		var didCount = 0;
		//number of batches we want
		var batchCount = Math.Min(length, Environment.ProcessorCount);

		//figure out batch size
		var batchSize = length / batchCount;

		batchSize = (int)(batchSize * batchSizeMultipler);
		batchSize = Math.Min(batchSize, length);
		batchSize = Math.Max(1, batchSize);

		//update batchCount bsed on actual batchSize
		if (length % batchSize == 0)
		{
			batchCount = length / batchSize;
		}
		else
		{
			batchCount = (length / batchSize) + 1;
		}


		var owner = SpanGuard<(int startInclusive, int endExclusive)>.Allocate(batchCount);
		var span = owner.Span;

		//calculate batches and put into span
		{
			var batchStartInclusive = start;
			var batchEndExclusive = batchStartInclusive + batchSize;
			var loopIndex = 0;
			while (batchEndExclusive <= endExclusive)
			{
				var thisBatchLength = batchEndExclusive - batchStartInclusive;
				__ERROR.Throw(thisBatchLength == batchSize);
				//do work:  batchStartInclusive, batchSize
				didCount += batchSize;
				span[loopIndex] = (batchStartInclusive, batchEndExclusive);

				//increment
				batchStartInclusive += batchSize;
				batchEndExclusive += batchSize;
				loopIndex++;
			}
			var remainder = endExclusive - batchStartInclusive;
			batchEndExclusive = batchStartInclusive + remainder;
			__ERROR.Throw(remainder < batchSize);
			if (remainder > 0)
			{
				//do last part:   batchStartInclusive, remainder
				didCount += remainder;
				span[loopIndex] = (batchStartInclusive, batchEndExclusive);
			}
			__ERROR.Throw(didCount == length);
		}

		return owner;
	}
	/// <summary>
	/// Range is ideal for cache coherency and lowest overhead.  If you require an action per element, it can behave like `Parallel.For` (set batchSizeMultipler=0).
	/// </summary>
	/// <param name="action">(start,endExclusive)=>ValueTask</param>
	public static ValueTask RangeAsync(int start, int length, Func<int, int, ValueTask> action) => RangeAsync(start, length, 1f, action);
	/// <summary>
	/// Range is ideal for cache coherency and lowest overhead.  If you require an action per element, it can behave like `Parallel.For` (set batchSizeMultipler=0).
	/// </summary>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <param name="batchSizeMultipler">The range is split into batches, with each batch being the total/cpu count.  The number of (and size of) batches can be modified by this parameter.
	/// <para>1 = The default. 1 batch per cpu.  Generally a good balance as it will utilize all cores if available, while not overwhelming the thread pool (allowing other work a fair chance in backlog situations) </para>
	/// <para>0.5 = 2 batches per cpu, with each batch half sized.   useful if the work required for each element is varied. </para>
	/// <para>0 = each batch is 1 element.  useful if parallelizing independent systems that are long running and use dissimilar regions of memory.</para>
	/// <para>2 = double sized batches, utilizing a maximum of half cpu cores.  Useful for offering parallel work while reducing multicore overhead.</para>
	/// <para>4 = quad size batches, use 1/4 cpu cores at max.</para></param>
	/// <param name="action">(start,endExclusive)=>ValueTask</param>
	/// <returns></returns>
	public static ValueTask RangeAsync(int start, int length, float batchSizeMultipler, Func<int, int, ValueTask> action)
	{
		if (length == 0)
		{
			return ValueTask.CompletedTask;
		}
		using var owner = _Range_ComputeBatches(start, length, batchSizeMultipler);

		return _Range_ExecuteActionAsync(owner.DangerousGetArray(), action);
	}
	private static async ValueTask _Range_ExecuteActionAsync(ArraySegment<(int start, int endExclusive)> spanOwnerDangerousArray, Func<int, int, ValueTask> action)
	{
		await Parallel.ForEachAsync(spanOwnerDangerousArray, (batch, cancelToken) => Unsafe.AsRef(in action).Invoke(batch.start, batch.endExclusive));
	}

	public static Task EachAsync<T>(IEnumerable<T> source, Func<T, CancellationToken, ValueTask> action)
	{
		return Parallel.ForEachAsync(source, action);
	}

	public static Task EachAsync<T>(IEnumerable<T> source, Func<T, ValueTask> action)
	{
		return Parallel.ForEachAsync(source, (item, cancelToken) => Unsafe.AsRef(in action).Invoke(item));
	}
	/// <summary>
	/// Range is ideal for cache coherency and lowest overhead.  If you require an action per element, it can behave like `Parallel.For` (set batchSizeMultipler=0).
	/// </summary>
	/// <param name="action">(start,endExclusive)=>ValueTask</param>
	public static void Range(int start, int length, Action<int, int> action) => Range(start, length, 1f, action);
	/// <summary>
	/// Range is ideal for cache coherency and lowest overhead.  If you require an action per element, it can behave like `Parallel.For` (set batchSizeMultipler=0).
	/// </summary>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <param name="batchSizeMultipler">The range is split into batches, with each batch being the total/cpu count.  The number of (and size of) batches can be modified by this parameter.
	/// <para>1 = The default. 1 batch per cpu.  Generally a good balance as it will utilize all cores if available, while not overwhelming the thread pool (allowing other work a fair chance in backlog situations) </para>
	/// <para>0.5 = 2 batches per cpu, with each batch half sized.   useful if the work required for each element is varied. </para>
	/// <para>0 = each batch is 1 element.  useful if parallelizing independent systems that are long running and use dissimilar regions of memory.</para>
	/// <para>2 = double sized batches, utilizing a maximum of half cpu cores.  Useful for offering parallel work while reducing multicore overhead.</para>
	/// <para>4 = quad size batches, use 1/4 cpu cores at max.</para></param>
	/// <param name="action">(start,endExclusive)=>ValueTask</param>
	/// <returns></returns>
	public static void Range(int start, int length, float batchSizeMultipler, Action<int, int> action)
	{
		if (length == 0)
		{
			return;
		}
		using var owner = _Range_ComputeBatches(start, length, batchSizeMultipler);
		var span = owner.Span;
		var array = owner.DangerousGetArray().Array!;

		Parallel.For(0, span.Length, (index) => Unsafe.AsRef(in action).Invoke(array[index].startInclusive, array[index].endExclusive));
	}


}



/// <summary>
/// use this instead of <see cref="SpanOwner{T}"/>.  This will alert you if you do not dispose properly.  
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct SpanGuard<T>
{
	public static SpanGuard<T> Allocate(int size)
	{
		return new SpanGuard<T>(SpanOwner<T>.Allocate(size));
	}

	public SpanGuard(SpanOwner<T> owner)
	{
		_owner = owner;
#if CHECKED
		_disposeGuard = new();
#endif
	}

	public SpanOwner<T> _owner;

	public Span<T> Span { get => _owner.Span; }
	public ArraySegment<T> DangerousGetArray()
	{
		return _owner.DangerousGetArray();
	}


#if CHECKED
	private DisposeGuard _disposeGuard;
#endif
	public void Dispose()
	{
		_owner.Dispose();

#if CHECKED
		_disposeGuard.Dispose();
#endif
	}
}

/// <summary>
/// helpers to allocate a WriteMem instance
/// </summary>
public static class Mem
{
	public static Mem<T> CreateUsing<T>(ArraySegment<T> backingStore) => Mem<T>.CreateUsing(backingStore);
	//public static WriteMem<T> Allocate<T>(MemoryOwnerCustom<T> MemoryOwnerNew) => WriteMem<T>.Allocate(MemoryOwnerNew);
	public static Mem<T> CreateUsing<T>(T[] array) => Mem<T>.CreateUsing(array);
	public static Mem<T> Allocate<T>(int count, bool clearOnDispose) => Mem<T>.Allocate(count, clearOnDispose);
	public static Mem<T> Allocate<T>(ReadOnlySpan<T> span, bool clearOnDispose) => Mem<T>.Allocate(span, clearOnDispose);
	public static Mem<T> CreateUsing<T>(Mem<T> writeMem) => writeMem;
	public static Mem<T> CreateUsing<T>(ReadMem<T> readMem) => Mem<T>.CreateUsing(readMem);

}
/// <summary>
/// helpers to allocate a ReadMem instance
/// </summary>
public static class ReadMem
{
	public static ReadMem<T> CreateUsing<T>(ArraySegment<T> backingStore) => ReadMem<T>.CreateUsing(backingStore);
	//public static ReadMem<T> Allocate<T>(MemoryOwnerCustom<T> MemoryOwnerNew) => ReadMem<T>.Allocate(MemoryOwnerNew);
	public static ReadMem<T> CreateUsing<T>(T[] array) => ReadMem<T>.CreateUsing(array);

	public static ReadMem<T> Allocate<T>(int count, bool clearOnDispose) => ReadMem<T>.Allocate(count, clearOnDispose);
	public static ReadMem<T> Allocate<T>(ReadOnlySpan<T> span, bool clearOnDispose) => ReadMem<T>.Allocate(span, clearOnDispose);
	public static ReadMem<T> CreateUsing<T>(Mem<T> writeMem) => ReadMem<T>.CreateUsing(writeMem);
}
/// <summary>
/// a write capable view into an array/span
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Mem<T>
{
	private readonly MemoryOwner_Custom<T>? _owner;
	private readonly ArraySegment<T> _segment;
	private readonly T[] _array;
	private readonly int _offset;
	public readonly int length;

	public static readonly Mem<T> Empty = new(null, null, null, 0, 0);
	internal Mem(MemoryOwner_Custom<T> owner, ArraySegment<T> segment, T[] array, int offset, int length)
	{
		_owner = owner;
		_segment = segment;
		_array = array;
		_offset = offset;
		this.length = length;
	}

	internal Mem(ArraySegment<T> segment)
	{
		_owner = null;
		_segment = segment;
		_array = segment.Array!;
		_offset = segment.Offset;
		length = segment.Count;
	}
	internal Mem(MemoryOwner_Custom<T> owner) : this(owner.DangerousGetArray())
	{
		_owner = owner;
	}
	/// <summary>
	/// allocate memory from the shared pool.
	/// If your Type is a reference type or contains references, be sure to use clearOnDispose otherwise you will have memory leaks.
	/// also note that the memory is not cleared by default.
	/// </summary>
	public static Mem<T> Allocate(int size, bool clearOnDispose)
	{
		__DEBUG.AssertOnce(System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>() == false || clearOnDispose, "alloc of classes via memPool can/will cause leaks");	
		var mo = MemoryOwner_Custom<T>.Allocate(size,clearOnDispose? AllocationMode.Clear: AllocationMode.Default);
		mo.ClearOnDispose = clearOnDispose;
		return new Mem<T>(mo);
	}
	/// <summary>
	/// allocate memory from the shared pool and copy the contents of the specified span into it
	/// </summary>
	public static Mem<T> Allocate(ReadOnlySpan<T> span, bool clearOnDispose)
	{
		var toReturn = Allocate(span.Length, clearOnDispose);
		span.CopyTo(toReturn.Span);
		return toReturn;
	}

	public static Mem<T> CreateUsing(T[] array)
	{
		return new Mem<T>(new ArraySegment<T>(array));
	}
	public static Mem<T> CreateUsing(T[] array, int offset, int count)
	{
		return new Mem<T>(new ArraySegment<T>(array, offset, count));
	}
	public static Mem<T> CreateUsing(ArraySegment<T> backingStore)
	{
		return new Mem<T>(backingStore);
	}
	internal static Mem<T> CreateUsing(MemoryOwner_Custom<T> MemoryOwnerNew)
	{
		return new Mem<T>(MemoryOwnerNew);
	}
	public static Mem<T> CreateUsing(ReadMem<T> readMem)
	{
		return readMem.AsWriteMem();
	}



	public Mem<T> Slice(int offset, int count)
	{
		var toReturn = new Mem<T>(_owner, new(_array, _offset + offset, count), _array, _offset + offset, count);
		return toReturn;
	}



	/// <summary>
	/// beware: the size of the array allocated may be larger than the size requested by this Mem.  
	/// As such, beware if using the backing Array directly.  respect the offset+length described in this segment.
	/// </summary>
	public ArraySegment<T> DangerousGetArray()
	{
		return _segment;
	}

	public Span<T> Span
	{
		get
		{
			return new Span<T>(_array, _offset, length);
		}
	}
	public Memory<T> Memory
	{
		get
		{
			return new Memory<T>(_array, _offset, length);
		}
	}

	public int Length
	{
		get
		{
			return length;
		}
	}

	public void Dispose()
	{
		if (_owner != null)
		{
			_owner.Dispose();
		}
#if DEBUG
		Array.Clear(_array, _offset, Length);
#endif
	}

	public ref T this[int index]
	{
		get
		{
			__DEBUG.Throw(index >= 0 && index < length);
			return ref _array[_offset + index];
		}
	}
	public Span<T>.Enumerator GetEnumerator()
	{
		return Span.GetEnumerator();
	}

	public ReadMem<T> AsReadMem()
	{
		return new ReadMem<T>(_owner,_segment,_array,_offset,length);
	}
	public override string ToString()
	{
		return $"{this.GetType().Name}<{typeof(T).Name}>[{this.length}]";
	}
}
/// <summary>
///  a read-only capable view into an array/span
/// </summary>
/// <typeparam name="T"></typeparam>
//[DebuggerTypeProxy(typeof(NotNot.Bcl.Collections.Advanced.CollectionDebugView<>))]
//[DebuggerDisplay("{ToString(),raw}")]
[DebuggerDisplay("{ToString(),raw,nq}")]
public readonly struct ReadMem<T>
{
	private readonly MemoryOwner_Custom<T>? _owner;
	private readonly ArraySegment<T> _segment;
	private readonly T[] _array;
	private readonly int _offset;
	public readonly int length;

	
	public override string ToString()
	{
		return $"{this.GetType().Name}<{typeof(T).Name}>[{this.length}]";
	}

	public static readonly ReadMem<T> Empty = new(null, null, null, 0, 0);
	internal ReadMem(MemoryOwner_Custom<T> owner, ArraySegment<T> segment, T[] array, int offset, int length)
	{
		_owner = owner;
		_segment = segment;
		_array = array;
		_offset = offset;
		this.length = length;
	}
	internal ReadMem(ArraySegment<T> segment)
	{
		_owner = null;
		_segment = segment;
		_array = segment.Array;
		_offset = segment.Offset;
		length = segment.Count;
	}
	internal ReadMem(MemoryOwner_Custom<T> owner) : this(owner.DangerousGetArray())
	{
		_owner = owner;
	}

	public static ReadMem<T> Allocate(int size, bool clearOnDispose)
	{
		__DEBUG.AssertOnce(System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>() || clearOnDispose, "alloc of classes via memPool can/will cause leaks");
		var mo = MemoryOwner_Custom<T>.Allocate(size);
		mo.ClearOnDispose = clearOnDispose;
		return new ReadMem<T>(mo);
	}
	public static ReadMem<T> Allocate(ReadOnlySpan<T> span, bool clearOnDispose)
	{
		var toReturn = Allocate(span.Length, clearOnDispose);
		span.CopyTo(toReturn.AsWriteSpan());
		return toReturn;
	}
	public static ReadMem<T> CreateUsing(T[] array)
	{
		return new ReadMem<T>(new ArraySegment<T>(array));
	}
	public static ReadMem<T> CreateUsing(T[] array, int offset, int count)
	{
		return new ReadMem<T>(new ArraySegment<T>(array, offset, count));
	}
	public static ReadMem<T> CreateUsing(ArraySegment<T> backingStore)
	{
		return new ReadMem<T>(backingStore);
	}
	internal static ReadMem<T> CreateUsing(MemoryOwner_Custom<T> MemoryOwnerNew)
	{
		return new ReadMem<T>(MemoryOwnerNew);
	}

	public static ReadMem<T> CreateUsing(Mem<T> writeMem)
	{
		return writeMem.AsReadMem();
	}

	public ReadMem<T> Slice(int offset, int count)
	{
		var toReturn = new ReadMem<T>(_owner, new(_array, _offset + offset, count), _array, _offset + offset, count);
		return toReturn;
	}

	/// <summary>
	/// <para>Returns the backing array segment, NOT READONLY protected.</para>
	/// beware: the size of the array allocated may be larger than the size requested by this Mem.  
	/// As such, beware if using the backing Array directly.  respect the offset+length described in this segment.
	/// </summary>
	public ArraySegment<T> DangerousGetArray()
	{
		return _segment;
	}

	public ReadOnlySpan<T> Span
	{
		get
		{
			//var x = new ReadOnlySpan<T>(_array, _offset, length);
			//for(var i = 0; i < x.Length; i++)
			//{
			//	ref readonly var item = ref x[i];
			//	//do stuff

			//}
			//foreach(ref readonly var item in x)
			//{
			//	//do stuff
			//}

			return new ReadOnlySpan<T>(_array, _offset, length);
		}
	}
	public ReadOnlyMemory<T> Memory
	{
		get
		{
			return new ReadOnlyMemory<T>(_array, _offset, length);
		}
	}

	public void Dispose()
	{
		if (_owner != null)
		{
			_owner.Dispose();
		}
	}
#if DEBUG
	public readonly T this[int index]
	{
		get
		{
			__DEBUG.Throw(index >= 0 && index < length);
			return _array[_offset + index];
		}
	}
#else
public ref readonly T this[int index]
	{
		get
		{
			__DEBUG.Throw(index >= 0 && index < length);
			return ref _array[_offset + index];
		}
	}
#endif
	public ReadOnlySpan<T>.Enumerator GetEnumerator()
	{
		return Span.GetEnumerator();
	}
	public Mem<T> AsWriteMem()
	{
		return new Mem<T>(_owner, _segment, _array, _offset, length);
	}
	public Span<T> AsWriteSpan()
	{
		return new Span<T>(_array, _offset, length);
	}


}


/// <summary>
/// helper to ensure object gets disposed properly.   can either be used as a base class, or as a member.
/// </summary>
public class DisposeGuard : IDisposable
{

	public bool IsDisposed { get; private set; }

	public void Dispose()
	{
		if (IsDisposed)
		{
			__ERROR.Assert(false, "why is dipose called twice?");
			return;
		}
		IsDisposed = true;
		OnDispose();
	}

	protected virtual void OnDispose()
	{

	}


	private string CtorStackTrace { get; set; } = "Callstack is only set in #DEBUG";
	public DisposeGuard()
	{
#if DEBUG
		CtorStackTrace = System.Environment.StackTrace;
#endif
	}

	~DisposeGuard()
	{
		if (!IsDisposed)
		{
			__ERROR.Assert(false, $"Did not call {this.GetType().Name}.Dispose() of the embedding type properly.    Callstack: " + CtorStackTrace);
		}
	}
}


/// <summary>
/// efficiently get/set a value for a given type. 
/// <para>similar use as a <see cref="ThreadLocal{T}"/></para>
/// </summary>
/// <remarks>because of implementation, should only be used for a max of about 100 types, otherwise storage gets large</remarks>
/// <typeparam name="TValue"></typeparam>

public struct TypeLocal<TValue>
{
	private static volatile int _typeCounter = -1;


	private static class TypeSlot<TType>
	{
		internal static readonly int _index = Interlocked.Increment(ref _typeCounter);
	}

	/// <summary>
	/// A small inefficiency:  will have 1 slot for each TType ever used for a TypeLocal call, regardless of if it's used in this instance or not
	/// </summary>
	private TValue[] _storage;

	public TypeLocal()
	{
		_storage = new TValue[Math.Max(10, _typeCounter + 1)];
	}

	private TValue[] EnsureStorageCapacity<TType>()
	{
		if (TypeSlot<TType>._index >= _storage.Length)
		{
			Array.Resize(ref _storage, (_typeCounter + 1) * 2);
		}
		return _storage;
	}

	public void Set<TType>(TValue value)
	{
		//Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(EnsureStorageCapacity<T>()), TypeSlot<T>.Index) = value;
		var storage = EnsureStorageCapacity<TType>();
		storage[TypeSlot<TType>._index] = value;
	}

	public TValue Get<TType>()
	{
		//return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(EnsureStorageCapacity<T>()), TypeSlot<T>.Index);
		//return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_storage), TypeSlot<T>.Index);
		return _storage[TypeSlot<TType>._index];
	}

	public ref TValue GetRef<TType>()
	{
		//return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_storage), TypeSlot<T>.Index);
		//return ref _storage[TypeSlot<TType>._index].value;

		return ref _storage[TypeSlot<TType>._index];
	}
}

