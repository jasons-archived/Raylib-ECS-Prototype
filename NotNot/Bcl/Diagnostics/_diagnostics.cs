using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;

namespace NotNot.Bcl.Diagnostics
{

	/// <summary>
	/// simple static helper to provide unique named instances of types.
	/// <para>For example, calling .CreateName{int}() ==> "int_0".   Calling it again would return "int_1" </para>
	/// </summary>
	[ThreadSafety(ThreadSituation.Always)]
	public static class InstanceNameCounter
	{
		private static Dictionary<Type, ulong> _countTracker = new();

		/// <summary>
		/// uses Type.Name, eg return: "Int_42"
		/// </summary>
		public static string CreateName<T>()
		{
			var type = typeof(T);
			lock (_countTracker)
			{
				ref var counter = ref _countTracker._GetValueRefOrAddDefault_Unsafe(type, out _);
				return $"{type.Name}_{counter++}";
			}
		}
		public static string CreateName(Type type)
		{
			lock (_countTracker)
			{
				ref var counter = ref _countTracker._GetValueRefOrAddDefault_Unsafe(type, out _);
				return $"{type.Name}_{counter++}";
			}
		}
		/// <summary>
		/// uses Type.FullName, eg: "System.Int_42"
		/// </summary>
		public static string CreateNameFull<T>()
		{
			var type = typeof(T);
			lock (_countTracker)
			{
				ref var counter = ref _countTracker._GetValueRefOrAddDefault_Unsafe(type, out _);
				return $"{type.FullName}_{counter++}";
			}
		}

	}


	/// <summary>
	/// Debug helper used in #CHECKED builds.  Checked builds perform extra checks to ensure thread safety and detect data corruption
	/// </summary>
	[DebuggerNonUserCode]
	public static class __CHECKED
	{
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string message = null)
		{
			_internal.DiagHelper.Assert(condition, message);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message)
		{
			_internal.DiagHelper.AssertOnce(condition, message);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null)
		{
			_internal.DiagHelper.Throw(condition, message);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message)
		{
			_internal.DiagHelper.WriteLine(message);
		}
	}
	[DebuggerNonUserCode]
	public static class __DEBUG
	{
		[Conditional("DEBUG")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string message=null)
		{
			_internal.DiagHelper.Assert(condition, message);
		}
		[Conditional("DEBUG")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message)
		{
			_internal.DiagHelper.AssertOnce(condition, message);
		}
		[Conditional("DEBUG")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null)
		{
			_internal.DiagHelper.Throw(condition, message);
		}
		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("DEBUG")]
		public static void WriteLine(string message)
		{
			_internal.DiagHelper.WriteLine(message);
		}
	}
	[DebuggerNonUserCode]
	public static class __ERROR
	{
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string message = null)
		{
			_internal.DiagHelper.Assert(condition, message);
		}
	
		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message)
		{
			_internal.DiagHelper.AssertOnce(condition, message);
		}
	
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message=null)
		{
			_internal.DiagHelper.Throw(condition, message);
		}
	
		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message)
		{
			_internal.DiagHelper.WriteLine(message);
		}
	}

	namespace _internal
	{
		/// <summary>
		/// The actual implementation of the various diagnostic helpers
		/// </summary>
		[DebuggerNonUserCode]
		public static class DiagHelper
		{
			[DebuggerNonUserCode, DebuggerHidden]
			public static void Assert(bool condition, string message)
			{
				message ??= "Assert condition failed";
				
				Debug.Assert(condition, message);
			}


			private static HashSet<string> _assertOnceLookup = new();


			/// <summary>
			/// assert for the given message only once
			/// </summary>
			/// <param name="condition"></param>
			/// <param name="message"></param>
			[DebuggerNonUserCode, DebuggerHidden]
			public static void AssertOnce(bool condition, string message)
			{
				message ??= "Assert condition failed";
				if (condition)
				{
					return;
				}

				lock (_assertOnceLookup)
				{
					if (_assertOnceLookup.Add(message)==false)
					{
						return;
					}
				}

				Debug.Assert(false,"ASSERT ONCE: " + message);
			}
			[DebuggerNonUserCode, DebuggerHidden]
			public static void Throw(bool condition, string message)
			{
				message ??= "Throw condition failed";
				if (condition == true)
				{
					return;
				}

				//Assert(false, message);
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

public static class __GcHelper
{
	public static void ForceFullCollect()
	{
		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();
		GC.WaitForPendingFinalizers();
	}
}