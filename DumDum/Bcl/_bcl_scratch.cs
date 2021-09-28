using DumDum.Bcl.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
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

namespace DumDum.Bcl;


/// <summary>
/// lolo: a static utils helper
/// </summary>
public unsafe static class __
{
	private static ThreadLocal<Random> _rand = new(() => new());
	/// <summary>
	/// get a thread-local Random
	/// </summary>
	public static Random Rand { get => _rand.Value; }
}


/// <summary>
/// sample unmanaged structs to provide statistics
/// </summary>
/// <typeparam name="T"></typeparam>
public unsafe struct PercentileSampler800<T> where T : unmanaged, IComparable<T>
{
	public const int BUFFER_SIZE = 800;
	public int MaxCapacity { get => BUFFER_SIZE / sizeof(T); }
	public StructArray800<T> _samples;


	private int _nextIndex;
	private bool _isCtored = true;
	/// <summary>
	/// if we have not filled our sample count, don't generate percentiles based on the blanks
	/// </summary>
	private int _fill;

	public int SampleCount
	{
		get => _sampleCount;
		set
		{
			__ERROR.Throw(value <= MaxCapacity, $"({value}) is too big.  Samples must be equal to or less than MaxCapacity ({MaxCapacity})");
			_sampleCount = value;
		}
	}
	private int _sampleCount = BUFFER_SIZE / sizeof(T);

	public void Clear()
	{
		_nextIndex = 0;
		_fill = 0;
		_samples.Clear();
	}

	public void RecordSample(T value)
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		fixed (byte* pBuffer = _samples._buffer)
		{
			var tBuffer = (T*)pBuffer;
			tBuffer[_nextIndex % SampleCount] = value;
		}
		_nextIndex = (_nextIndex + 1) % SampleCount;
		if (_fill < MaxCapacity)
		{
			_fill++;
		}
	}
	public T GetLastSample()
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		var lastIndex = (SampleCount + _nextIndex - 1) % SampleCount;
		fixed (byte* pBuffer = _samples._buffer)
		{
			var tBuffer = (T*)pBuffer;
			return tBuffer[_nextIndex % SampleCount];
		}
	}

	public Percentiles<T> GetPercentiles()
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");

		return new(_samples.AsSpan().Slice(0, Math.Min(SampleCount, _fill)));
	}

	public override string ToString()
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		return GetPercentiles().ToString();
	}
	public string ToString<TOut>(Func<T, TOut> formater)
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		return GetPercentiles().ToString(formater);
	}



}

/// <summary>
/// Provides a 7-number-summary of data.
/// <para>see https://en.wikipedia.org/wiki/Seven-number_summary</para>
/// <para>also includes quartiles, see: https://en.wikipedia.org/wiki/Quartile</para>
/// </summary>
/// <remarks>for a good explanation of "why", see: https://www.dynatrace.com/news/blog/why-averages-suck-and-percentiles-are-great/</remarks>
/// <typeparam name="T"></typeparam>
public struct Percentiles<T> where T : unmanaged, IComparable<T>
{
	/// <summary>
	/// how many samples were present on the input data
	/// </summary>
	public int sampleCount;
	/// <summary>
	/// the minimum.  percentile 0
	/// </summary>
	public T p0;
	/// <summary>
	/// the 5th percentile.  useful as a minimum if you want to avoid outliers.
	/// </summary>
	public T p5;
	/// <summary>
	/// 1st quartile
	/// </summary>
	public T p25;
	/// <summary>
	/// 2nd quartile, aka median
	/// </summary>
	public T p50;
	/// <summary>
	/// 3rd quartile
	/// </summary>
	public T p75;
	/// <summary>
	/// the 95th percentile.  useful as a maximum if you want to avoid outliers.
	/// </summary>
	public T p95;
	/// <summary>
	/// the maximum
	/// </summary>
	public T p100;

	public Percentiles(Span<T> samples)
	{
		if (samples.Length == 0)
		{
			this = default;
			return;
		}
		var len = samples.Length;
		sampleCount = len;
		Span<T> sortedSamples = stackalloc T[len];
		samples.CopyTo(sortedSamples);
		sortedSamples.Sort();
		p0 = sortedSamples[0];
		p5 = sortedSamples[5 * len / 100];
		p25 = sortedSamples[25 * len / 100];
		p50 = sortedSamples[50 * len / 100];
		p75 = sortedSamples[75 * len / 100];
		p95 = sortedSamples[95 * len / 100];
		p100 = sortedSamples[len - 1];

	}

	public override string ToString()
	{
		return $"[{p0} {{{p5} ({p25} ={p50}= {p75}) {p95}}} {p100}](x{sampleCount})";
	}
	/// <summary>
	/// generate string while passing a custom format function to the percentile samples
	/// </summary>
	/// <typeparam name="TOut"></typeparam>
	/// <param name="formater"></param>
	/// <returns></returns>
	public string ToString<TOut>(Func<T, TOut> formater)
	{
		return $"[{formater(p0)} {{{formater(p5)} ({formater(p25)} ={formater(p50)}= {formater(p75)}) {formater(p95)}}} {formater(p100)}](x{sampleCount})";
	}
}

public unsafe struct StructArray4096<T> where T : unmanaged
{
	public const int BUFFER_SIZE = 4096;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T>.Enumerator GetEnumerator()
	{
		return AsSpan().GetEnumerator();
	}
	public Span<T> AsSpan()
	{
		StructArray100<byte>.__TEST_Unit();

		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
	public void Clear()
	{
		AsSpan().Clear();
	}
	public ref T this[int index]
	{
		get
		{
			var span = AsSpan();
			return ref span[index];
		}
	}
}
public unsafe struct StructArray2048<T> where T : unmanaged
{
	public const int BUFFER_SIZE = 2048;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T>.Enumerator GetEnumerator()
	{
		return AsSpan().GetEnumerator();
	}
	public Span<T> AsSpan()
	{
		StructArray100<byte>.__TEST_Unit();

		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
	public void Clear()
	{
		AsSpan().Clear();
	}
	public ref T this[int index]
	{
		get
		{
			var span = AsSpan();
			return ref span[index];
		}
	}
}
public unsafe struct StructArray1024<T> where T : unmanaged
{
	public const int BUFFER_SIZE = 1024;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T>.Enumerator GetEnumerator()
	{
		return AsSpan().GetEnumerator();
	}
	public Span<T> AsSpan()
	{
		StructArray100<byte>.__TEST_Unit();

		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
	public void Clear()
	{
		AsSpan().Clear();
	}
	public ref T this[int index]
	{
		get
		{
			var span = AsSpan();
			return ref span[index];
		}
	}
}
public unsafe struct StructArray800<T> where T : unmanaged
{
	public const int BUFFER_SIZE = 800;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T>.Enumerator GetEnumerator()
	{
		return AsSpan().GetEnumerator();
	}
	public Span<T> AsSpan()
	{
		StructArray100<byte>.__TEST_Unit();

		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
	public void Clear()
	{
		AsSpan().Clear();
	}
	public ref T this[int index]
	{
		get
		{
			var span = AsSpan();
			return ref span[index];
		}
	}
}
public unsafe struct StructArray400<T> where T : unmanaged
{
	private const int BUFFER_SIZE = 400;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T>.Enumerator GetEnumerator()
	{
		return AsSpan().GetEnumerator();
	}
	public Span<T> AsSpan()
	{
		StructArray100<byte>.__TEST_Unit();

		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
	public void Clear()
	{
		AsSpan().Clear();
	}


	public ref T this[int index]
	{
		get
		{
			var span = AsSpan();
			return ref span[index];
		}
	}
}



/// <summary>
/// A struct based array, with a size of 100bytes.
/// <para>as a `long` is 8 bytes, a StructArray100{long} could hold 100/8= 12 longs</para>
/// <para>This is useful for reducing object allocations in either high-frequency or long-lived objects</para>
/// <para>If you need a throwaway temporary array, consider using stackalloc spans instead.  example:<code>Span{Timespan} samples = stackalloc Timespan[100];</code>.  But this StructArray also works for stackalloc purposes. </para>
/// </summary>
public unsafe struct StructArray100<T> where T : unmanaged
{
	private const int BUFFER_SIZE = 100;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T>.Enumerator GetEnumerator()
	{
		return AsSpan().GetEnumerator();
	}
	public Span<T> AsSpan()
	{
		//NOTE: This trick works because the CLR/GC treats Span special.  it won't move the underlying _buffer as long as the Span is in scope.

		__TEST_Unit();

		//fixed (byte* ptr = _buffer)
		//{
		//	return new Span<T>(ptr, Length);
		//}		
		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
	public void Clear()
	{
		AsSpan().Clear();
	}


	public ref T this[int index]
	{
		get
		{
			var span = AsSpan();
			return ref span[index];
		}
	}


	private static int __TEST_counter;
	/// <summary>
	/// basic unit test checking for GC race conditions
	/// </summary>
	[Conditional("TEST")]
	public unsafe static void __TEST_Unit(bool forceRunSync = false)
	{
		__TEST_counter++;
		if (__TEST_counter % 1000 == 1 || forceRunSync == true)
		{
			var test = () =>
			{
				//test this[] access
				{
					var testArray = new StructArray100<Vector3>();



					for (var i = 0; i < testArray.Length; i++)
					{
						if (i % 2 == 0)
						{
							testArray[i] = new() { X = i, Y = i + 1, Z = i + 2 };
						}
						else
						{
							ref var vec = ref testArray[i];
							__GcHelper.ForceFullCollect();
							vec = new() { X = i, Y = i + 1, Z = i + 2 };
						}
					}
					for (var i = 0; i < testArray.Length; i++)
					{
						var testVec = new Vector3() { X = i, Y = i + 1, Z = i + 2 };
						__CHECKED.Throw(testArray[i] == testVec);
					}
				}

				//test span access
				{
					var testArray = new StructArray100<Vector3>();

					var span = testArray.AsSpan();

					for (var i = 0; i < span.Length; i++)
					{
						span[i] = new() { X = i, Y = i + 1, Z = i + 2 };
					}
					__GcHelper.ForceFullCollect();
					var span2 = testArray.AsSpan();
					for (var i = 0; i < span.Length; i++)
					{
						var testVec = new Vector3() { X = i, Y = i + 1, Z = i + 2 };
						__CHECKED.Throw(span2[i] == testVec);
					}
					__CHECKED.Throw(span._ReferenceEquals(ref span2));
				}



			};
			if (forceRunSync)
			{
				test();
			}
			else
			{
				Task.Run(test);
			}
		}

	}


}



public static class ParallelFor
{

	private static SpanPool<(int startInclusive, int endExclusive)> _Range_ComputeBatches(int start, int length, float batchSizeMultipler)
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


		var owner = SpanPool<(int startInclusive, int endExclusive)>.Allocate(batchCount);
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
		var array = owner.DangerousGetArray().Array;

		Parallel.For(0, span.Length, (index) =>Unsafe.AsRef(in action).Invoke(array[index].startInclusive, array[index].endExclusive));

	}


}


public ref struct SpanPool<T>
{
	public static SpanPool<T> Allocate(int size)
	{
		return new SpanPool<T>(SpanOwner<T>.Allocate(size));
	}

	public SpanPool(SpanOwner<T> owner)
	{
		_owner = owner;
#if CHECKED
		_disposeCheck = new();
#endif
	}

	public SpanOwner<T> _owner;

	public Span<T> Span { get => _owner.Span; }
	public ArraySegment<T> DangerousGetArray()
	{
		return _owner.DangerousGetArray();
	}


#if CHECKED
	private DisposeSentinel _disposeCheck;
#endif
	public void Dispose()
	{
		_owner.Dispose();

#if CHECKED
		_disposeCheck.Dispose();
#endif
	}


}


public class DisposeSentinel : IDisposable
{

	public bool IsDisposed { get; private set; }

	public void Dispose()
	{
		if (IsDisposed)
		{
			return;
		}
		IsDisposed = true;
	}
	public string CtorStackTrace { get; private set; }
	public DisposeSentinel()
	{
		CtorStackTrace = System.Environment.StackTrace;
	}

	~DisposeSentinel()
	{
		if (!IsDisposed)
		{
			__ERROR.Assert(false, "Did not call .Dispose() of the embedding type properly.    Callstack: " + CtorStackTrace);
		}
	}
}


[StructLayout(LayoutKind.Auto,Size =64)]
public unsafe struct CacheLineRef<T>
{
	//[FieldOffset(0)]
	public T value;
	//[FieldOffset(0)]
	private fixed byte _size[60];
}

/// <summary>
/// DANGER. adapted from, and for inlining Array unbounded workflow: https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/059cf83f1fb02a4fbb4ce24249ea6e38f504983b/Microsoft.Toolkit.HighPerformance/Extensions/ArrayExtensions.cs#L86/// 
/// If you store a reference to this, it must be recreated every array resize.
/// </summary>
/// <remarks>Do not change the layout of this.  it follows the current DotNet6 layout of Array.</remarks>
[StructLayout(LayoutKind.Sequential)]
public sealed class __UNSAFE_ArrayData<T>
{
	public IntPtr Length;
	public byte Data;
	/// <summary>
	/// UNBOUNDED.  No array bounds are checked.  if you request an index past the end.... ???  Here be dragons.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>	
	public ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		get
		{
			ref T r0 = ref Unsafe.As<byte, T>(ref Data);
			ref T ri = ref Unsafe.Add(ref r0, index);
			return ref ri;
		}
	}
}
