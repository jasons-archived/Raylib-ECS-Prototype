﻿// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using NotNot.Bcl.Diagnostics;

namespace NotNot.Bcl.Collections._unused
{
	/// <summary>
	/// allows embedding a string of length 10 into a struct/class without GC overhead
	/// </summary>
	public unsafe struct Char10
	{
		public const int SIZEOF = 10;
		private fixed char _storage[10];
		private int _length;

		public ref char this[int index] => ref _storage[index];

		public void Clear()
		{
			fixed (char* ptr = _storage)
			{
				var target = new Span<char>(ptr, SIZEOF);
				target.Clear();
				_length = 0;
			}
		}

		public void From(ref Span<char> value, int length)
		{
			fixed (char* ptr = _storage)
			{
				__CHECKED.Assert(length <= SIZEOF, "warning, data length truncated when storing into this struct");
				_length = length < SIZEOF ? length : SIZEOF;
				//var target = new Span<char>(ptr, SIZEOF);
				var target = new Span<char>(ptr, _length);
				value.CopyTo(target);


			}
		}
		public void From(ref ReadOnlySpan<char> value, int length)
		{
			fixed (char* ptr = _storage)
			{
				__CHECKED.Assert(length <= SIZEOF, "warning, data length truncated when storing into this struct");
				_length = length < SIZEOF ? length : SIZEOF;
				//var target = new Span<char>(ptr, SIZEOF);
				var target = new Span<char>(ptr, _length);
				value.CopyTo(target);
			}
		}

		public void From(char* value, int length)
		{
			var span = new Span<char>(value, length);
			From(ref span, length);
		}

		public void From(string value)
		{
			var span = value.AsSpan(0, SIZEOF);
			From(ref span, value.Length < SIZEOF ? _length : SIZEOF);
		}

		public void ToSpan(ref Span<char> target)
		{
			fixed (char* ptr = _storage)
			{
				var span = new Span<char>(ptr, _length);
				span.CopyTo(target);
			}
		}

		public override string ToString()
		{
			fixed (char* ptr = _storage)
			{
				var toReturn = new string(ptr, 0, _length);
				return toReturn;
			}
		}
	}
}