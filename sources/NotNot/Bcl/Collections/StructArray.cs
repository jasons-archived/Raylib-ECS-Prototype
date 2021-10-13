﻿// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using NotNot.Bcl.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl.Collections;


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

