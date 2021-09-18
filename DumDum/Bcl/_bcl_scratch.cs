using DumDum.Bcl.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DumDum.Bcl;


public unsafe struct DebugSampler800<T> where T : unmanaged, IComparable<T>
{
	public const int BUFFER_SIZE = 800;
	public int MaxCapacity { get => BUFFER_SIZE / sizeof(T); }
	public StructArray800<T> _samples;


	private int _nextIndex;
	private bool _isCtored = true;

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

	public void RecordSample(T value)
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		fixed (byte* pBuffer = _samples._buffer)
		{
			var tBuffer = (T*)pBuffer;
			tBuffer[_nextIndex % SampleCount] = value;
		}
		_nextIndex = (_nextIndex + 1) % SampleCount;
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

	public Quartiles<T> GetQuartiles()
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		return new(_samples.AsSpan().Slice(0, SampleCount));
	}

	public override string ToString()
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		return GetQuartiles().ToString();
	}
	public string ToString<TOut>(Func<T, TOut> formater)
	{
		__CHECKED.Throw(_isCtored, "you need to use a .ctor() otherwise fields are not init");
		return GetQuartiles().ToString(formater);
	}



}

public struct Quartiles<T> where T: unmanaged, IComparable<T>
{
	public int sampleCount;
	public T q0;
	public T q5;
	public T q25;
	public T q50;
	public T q75;
	public T q95;
	public T q100;

	public Quartiles(Span<T> samples)
	{
		var len = samples.Length;
		sampleCount = len;
		Span<T> sortedSamples = stackalloc T[len];
		samples.CopyTo(sortedSamples);
		sortedSamples.Sort();
		q0 = sortedSamples[0];
		q5 = sortedSamples[5 * len / 100];
		q25 = sortedSamples[25 * len / 100];
		q50 = sortedSamples[50 * len / 100];
		q75 = sortedSamples[75 * len / 100];
		q95 = sortedSamples[95 * len / 100];
		q100 = sortedSamples[len-1];

	}

	public override string ToString()
	{
			return $"[{q0} {{{q5} ({q25} ={q50}= {q75}) {q95}}} {q100}] (samples={sampleCount})";	
	}
	public string ToString<TOut>(Func<T, TOut> formater)
	{
			return $"[{formater(q0)} {{{formater(q5)} ({formater(q25)} ={formater(q50)}= {formater(q75)}) {formater(q95)}}} {formater(q100)}] (samples={sampleCount})";		
	}
}

public unsafe struct StructArray800<T> where T : unmanaged
{
	public const int BUFFER_SIZE = 800;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T> AsSpan()
	{
		StructArray100<byte>.__TEST_Unit();

		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
}
public unsafe struct StructArray400<T> where T : unmanaged
{
	private const int BUFFER_SIZE = 400;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	public Span<T> AsSpan()
	{
		StructArray100<byte>.__TEST_Unit();

		return MemoryMarshal.Cast<byte, T>(MemoryMarshal.CreateSpan(ref _buffer[0], BUFFER_SIZE));
	}
}
public unsafe struct StructArray100<T> where T : unmanaged
{
	private const int BUFFER_SIZE = 100;
	public fixed byte _buffer[BUFFER_SIZE];

	public int Length { get => BUFFER_SIZE / sizeof(T); }

	
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
				var testArray = new StructArray100<Vector3>();

				var span = testArray.AsSpan();

				for (var i = 0; i < span.Length; i++)
				{
					span[i] = new() { X = i, Y = i + 1, Z = i + 2 };
				}
				GC.Collect();
				var span2 = testArray.AsSpan();
				for (var i = 0; i < span.Length; i++)
				{
					var testVec = new Vector3() { X = i, Y = i + 1, Z = i + 2 };
					__CHECKED.Throw(span2[i] == testVec);
				}
				__CHECKED.Throw(span._ReferenceEquals(ref span2));

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





