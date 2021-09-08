using System;
using System.Diagnostics;

namespace DumDum.Bcl.Diagnostics
{
	/// <summary>
	/// Debug helper used in #CHECKED builds.  Checked builds perform extra checks to ensure thread safety and detect data corruption
	/// </summary>
	public static class __CHECKED
	{
		[Conditional("CHECKED")]
		public static void Assert(bool condition, string message = "__CHECKED condition failed")
		{
			Debug.Assert(condition, message);
		}
		[Conditional("CHECKED")]
		public static void Throw(bool condition, string message = "__CHECKED condition failed")
		{
			if (condition == true)
			{
				return;
			}

			Assert(false, message);
			throw new(message);
		}
	}
	public static class __DEBUG
	{
		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message = "__DEBUG condition failed")
		{
			Debug.Assert(condition, message);
		}
		[Conditional("DEBUG")]
		public static void Throw(bool condition, string message = "__DEBUG condition failed")
		{
			if (condition == true)
			{
				return;
			}

			Assert(false, message);
			throw new(message);
		}

		public static void WriteLine(string? value)
		{
			Console.WriteLine(value);
		}
	}
	public static class __ERROR
	{
		[Conditional("CHECKED")]
		public static void Assert(bool condition, string message = "__ERROR condition failed")
		{
			Debug.Assert(condition, message);
		}
		[Conditional("CHECKED")]
		public static void Throw(bool condition, string message = "__ERROR condition failed")
		{
			if (condition == true)
			{
				return;
			}

			Assert(false, message);
			throw new(message);
		}
	}
}