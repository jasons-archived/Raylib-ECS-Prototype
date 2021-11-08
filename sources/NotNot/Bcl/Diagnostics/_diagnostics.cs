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
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace NotNot.Bcl.Diagnostics
{

	/// <summary>
	/// simple static helper to provide unique named instances of types.
	/// <para>For example, calling .CreateName{int}() ==> "int_0".   Calling it again would return "int_1" </para>
	/// </summary>
	[ThreadSafety(ThreadSituation.Always)]
	public static class InstanceNameHelper
	{
		private static Dictionary<string, ulong> _countTracker = new();

		/// <summary>
		/// uses Type.Name, eg return: "Int_42"
		/// </summary>
		public static string CreateName<T>()
		{
			var type = typeof(T);
			var name = type.Name;
			lock (_countTracker)
			{
				ref var counter = ref _countTracker._GetValueRefOrAddDefault_Unsafe(name, out _);
				return $"{name}_{counter++}";
			}
		}
		/// <summary>
		/// uses Type.Name, eg return: "Int_42"
		/// </summary>
		public static string CreateName(Type type)
		{
			var name = type.Name;
			lock (_countTracker)
			{
				ref var counter = ref _countTracker._GetValueRefOrAddDefault_Unsafe(name, out _);
				return $"{name}_{counter++}";
			}
		}
		/// <summary>
		/// uses Type.FullName, eg: "System.Int_42"
		/// </summary>
		public static string CreateNameFull<T>()
		{
			var type = typeof(T);
			var name = type.FullName;
			lock (_countTracker)
			{
				ref var counter = ref _countTracker._GetValueRefOrAddDefault_Unsafe(name, out _);
				return $"{name}_{counter++}";
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
		public static void Assert(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.Assert(condition, message, conditionName);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.AssertOnce(condition, message, conditionName);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.Throw(condition, message, conditionName);
		}
		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message)
		{
			_internal.DiagHelper.WriteLine(message);
		}
		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("CHECKED")]
		public static void WriteLine(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.WriteLine(condition, message, conditionName);
		}
	}
	[DebuggerNonUserCode]
	public static class __DEBUG
	{
		/// <summary>
		/// Asserts if condition evaluates to false.  
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="message"></param>
		/// <param name="conditionName"></param>
		[Conditional("DEBUG"), Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.Assert(condition, message, conditionName);
		}
		[Conditional("DEBUG"), Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.AssertOnce(condition, message, conditionName);
		}
		[Conditional("DEBUG"), Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.Throw(condition, message, conditionName);
		}
		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("DEBUG"), Conditional("CHECKED")]
		public static void WriteLine(string message)
		{
			_internal.DiagHelper.WriteLine(message);
		}
		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("DEBUG"), Conditional("CHECKED")]
		public static void WriteLine(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.WriteLine(condition, message, conditionName);
		}
	}
	[DebuggerNonUserCode]
	public static class __ERROR
	{
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.Assert(condition, message, conditionName);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.AssertOnce(condition, message, conditionName);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.Throw(condition, message, conditionName);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message)
		{
			_internal.DiagHelper.WriteLine(message);
		}
		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
		{
			_internal.DiagHelper.WriteLine(condition, message, conditionName);
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
			public static void Assert(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
			{
				if (condition)
				{
					return;
				}
				message ??= "Assert condition failed";

				Debug.Assert(false, (string)$"ASSERT({conditionName}) {message}");
			}


			private static HashSet<string> _assertOnceLookup = new();


			/// <summary>
			/// assert for the given message only once
			/// </summary>
			/// <param name="condition"></param>
			/// <param name="message"></param>
			[DebuggerNonUserCode, DebuggerHidden]
			public static void AssertOnce(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
			{
				if (condition)
				{
					return;
				}
				message ??= "Assert condition failed";

				lock (_assertOnceLookup)
				{
					if (_assertOnceLookup.Add(message) == false)
					{
						return;
					}
				}

				//Debug.Assert(false, "ASSERT ONCE: " + message);
				Debug.Assert(false,(string)$"ASSERT_ONCE({conditionName}) {message}");
			}
			[DebuggerNonUserCode, DebuggerHidden]
			public static void Throw(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
			{
				if (condition == true)
				{
					return;
				}
				message ??= "Throw condition failed";

				//Assert(false, message, conditionName);
				//throw new(message);
				throw new($"THROW({conditionName}) {message}");
			}

			[DebuggerNonUserCode, DebuggerHidden]
			public static void WriteLine(string message)
			{
				Console.WriteLine(message);
			}

			[DebuggerNonUserCode, DebuggerHidden]
			public static void WriteLine(bool condition, string message = null, [CallerArgumentExpression("condition")] string? conditionName = null)
			{
				if (condition == true)
				{
					return;
				}
				//Console.WriteLine(message);
				Console.WriteLine($"WRITE({conditionName}) {message}");
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
