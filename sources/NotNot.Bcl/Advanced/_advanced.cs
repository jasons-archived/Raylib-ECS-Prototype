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
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl._advanced;



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
