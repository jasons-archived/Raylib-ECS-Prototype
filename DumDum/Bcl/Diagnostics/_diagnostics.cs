using System;
using System.Diagnostics;

namespace DumDum.Bcl.Diagnostics
{
	/// <summary>
	/// Debug helper used in #CHECKED builds.  Checked builds perform extra checks to ensure thread safety and detect data corruption
	/// </summary>
	[DebuggerNonUserCode]
	public static class __CHECKED
	{
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string? message = null)
		{
			_internal.DebugLogic.Assert(condition, message);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string? message = null)
		{
			_internal.DebugLogic.Throw(condition, message);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message)
		{
			_internal.DebugLogic.WriteLine(message);
		}
	}
	[DebuggerNonUserCode]
	public static class __DEBUG
	{
		[Conditional("DEBUG")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string? message=null)
		{
			_internal.DebugLogic.Assert(condition, message);
		}
		[Conditional("DEBUG")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string? message = null)
		{
			_internal.DebugLogic.Throw(condition, message);
		}
		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("DEBUG")]
		public static void WriteLine(string message)
		{
			_internal.DebugLogic.WriteLine(message);
		}
	}
	[DebuggerNonUserCode]
	public static class __ERROR
	{
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string? message = null)
		{
			_internal.DebugLogic.Assert(condition, message);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string? message = null)
		{
			_internal.DebugLogic.Throw(condition, message);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message)
		{
			_internal.DebugLogic.WriteLine(message);
		}
	}

	namespace _internal
	{
		[DebuggerNonUserCode]
		public static class DebugLogic
		{
			[DebuggerNonUserCode, DebuggerHidden]
			public static void Assert(bool condition, string message = "Assert condition failed")
			{
				Debug.Assert(condition, message);
			}
			[DebuggerNonUserCode, DebuggerHidden]
			public static void Throw(bool condition, string message = "Throw condition failed")
			{
				if (condition == true)
				{
					return;
				}

				Assert(false, message);
				throw new(message);
			}

			[DebuggerNonUserCode]
			public static void WriteLine(string message)
			{
				Console.WriteLine(message);
			}
		}
	}
}