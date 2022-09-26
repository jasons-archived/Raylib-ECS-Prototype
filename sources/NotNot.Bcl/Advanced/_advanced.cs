// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using NotNot.Bcl.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl.Advanced;


/// <summary>
/// holdover from Raylib work.  keeping cuz not sure if still needed?
/// </summary>
public abstract class FramePacketBase
{
	/// <summary>
	/// internal helper used to track writes, to help catch race conditions (misuse)
	/// </summary>
	public int _version;
	public bool IsInitialized { get; private set; }

	public bool IsSealed { get; private set; }

	public void NotifyWrite()
	{
		__ERROR.Throw(IsInitialized && _version > 0 && IsSealed == false);
		_version++;
	}

	public void Seal()
	{
		IsSealed = true;
	}

	public void Recycle()
	{
		__ERROR.Throw(IsInitialized && _version > 0);
		IsInitialized = false;
		_version = -1;
		OnRecycle();
	}

	public void Initialize()
	{
		__ERROR.Throw(IsInitialized == false && _version <= 0);
		IsInitialized = true;
		_version = 1;
		IsSealed = false;
		OnInitialize();
	}

	protected abstract void OnRecycle();
	protected abstract void OnInitialize();
}


/// <summary>
/// efficiently get/set a value for a given type.
/// This should be used in singleton type lookups.
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


/// <summary>
/// allows extraction of the Sign, Exponent, and Mantissa from a float.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
struct FloatInspector
{
	[FieldOffset(0)]
	public float val;


	[FieldOffset(0)]
	public int bitfield;


	// public FloatInspector(float value)
	// {
	// 	this = new FloatInspector();
	// 	val = value;
	// }

	public int Sign
	{
		get
		{
			return bitfield >> 31;
		}
		set
		{
			bitfield |= value << 31;
		}
	}
	public int Exponent
	{
		get
		{
			return (bitfield & 0x7f800000) >> 23;
			// var e = bitfield & 0x7f800000;
			// e >>= 23;
			// return e;
		}
		set
		{
			bitfield |= (value << 23) & 0x7f800000;
		}
	}

	public int Mantissa
	{
		get
		{
			return bitfield & 0x007fffff;
		}
		set
		{
			bitfield |= value & 0x007fffff;
		}
	}

	public override string ToString()
	{
		var floatBytes = BitConverter.GetBytes(this.val);
		var bitBytes = BitConverter.GetBytes(this.bitfield);
		var floatBytesStr = string.Join(",", floatBytes.Select(b => b.ToString("X")));
		var bitBytesStr = string.Join(",", bitBytes.Select(b => b.ToString("X")));


		//return String.Format("FLOAT={0:G9} RT={0:R} FIELD={1:G},  HEXTEST={1:X8}, fBytes={2}, bBytes={3}", this.val, this.bitfield, floatBytesStr, bitBytesStr); //HEXTEST={1:X}, 
		return String.Format("FLOAT={0:G9} BITFIELD={1:G},  HEX={1:X}, S={2}, E={3:X}, M={4:X}", this.val, this.bitfield, this.Sign, this.Exponent, this.Mantissa); //HEXTEST={1:X}, 
	}


}

[StructLayout(LayoutKind.Explicit)]
struct Vector3Inspector
{
	[FieldOffset(0)]
	public Vector3 val;
	[FieldOffset(0)]
	public FloatInspector x;
	[FieldOffset(4)]
	public FloatInspector y;
	[FieldOffset(8)]
	public FloatInspector z;

	public override string ToString()
	{

		return String.Format("x=[{0}], \n\ty=[{1}], \n\tz=[{2}]", x, y, z);
	}

}

/// <summary>
/// Pin managed objects
/// </summary>
/// <remarks> from @Zombie on C# Discord #LowLevel channel.  2022-01-30
///<para>other "interesting" ideas such as Modifying an objects type can be pondered at https://github.com/WhiteBlackGoose/VeryUnsafe/blob/master/VeryUnsafe/VeryUnsafe.cs#L36</para>
/// </remarks>
public static class Unsafe2
{

	/// <summary>
	/// use to obtain raw access to a managed object.  allowing `fixed` pinning.
	/// <para>
	/// Usage:<code>fixed (byte* data = [AND_OPERATOR]GetRawObjectData(managed)){  }</code>
	/// </para>
	/// </summary>
	public static ref byte GetRawObjectData(object o)
	{
		//usage:  fixed (byte* data = &GetRawObjectData(managed)) { }
		return ref new PinnableUnion(o).Pinnable.Data;
	}

	[StructLayout(LayoutKind.Sequential)]
	sealed class Pinnable
	{
		public byte Data;
	}

	[StructLayout(LayoutKind.Explicit)]
	struct PinnableUnion
	{
		[FieldOffset(0)]
		public object Object;

		[FieldOffset(0)]
		public Pinnable Pinnable;

		public PinnableUnion(object o)
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out this);
			Object = o;
		}
	}

	/// <summary>
	/// list has an array located inside it. this method will return you a reference to it.
	/// Keep in mind that if the list is modified, this reference may nolonger be valid.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="list"></param>
	/// <returns></returns>
	public static unsafe T[] GetArray<T>(List<T> list)
	{
		//get span from list
		var span = CollectionsMarshal.AsSpan(list);
		//ref to first element of the array
		ref T r0 = ref MemoryMarshal.GetReference(span);
		//pointer to location 2 references backward in memory.   This is the location of the method table  (what object references point to).
		ref T r1 = ref Unsafe.SubtractByteOffset(ref r0, (nuint)(IntPtr.Size * 2));


		return Unsafe.As<T, T[]>(ref r1);
		//fancy coersion if the above fails at runtime
		//var cast = (delegate*<ref T, T[]>)(delegate*<ref byte, ref byte>)&Unsafe.As<byte, byte>;
		//return cast(ref r1);
	}
}
