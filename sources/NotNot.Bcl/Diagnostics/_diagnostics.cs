// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using ANSIConsole;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;

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
		public static void Assert(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.Assert(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
		}

		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.AssertOnce(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
		}

		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.Throw(condition, message, conditionName, memberName, sourceFilePath, sourceLineNumber);
		}

		[Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.WriteLine(message, conditionName, memberName, sourceFilePath, sourceLineNumber);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("CHECKED")]
		public static void WriteLine(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.WriteLine(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
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
		[DebuggerNonUserCode] //, DebuggerHidden]
		public static void Assert(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.Assert(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
		}

		[Conditional("DEBUG"), Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.AssertOnce(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
		}

		[Conditional("DEBUG"), Conditional("CHECKED")]
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.Throw(condition, message, conditionName, memberName, sourceFilePath, sourceLineNumber);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("DEBUG"), Conditional("CHECKED")]
		public static void WriteLine(string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.WriteLine(message, conditionName, memberName, sourceFilePath, sourceLineNumber);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		[Conditional("DEBUG"), Conditional("CHECKED")]
		public static void WriteLine(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.WriteLine(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
		}
	}

	[DebuggerNonUserCode]
	public static class __ERROR
	{
		[DebuggerNonUserCode, DebuggerHidden]
		public static void Assert(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.Assert(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		public static void AssertOnce(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.AssertOnce(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		public static void Throw(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.Throw(condition, message, conditionName, memberName, sourceFilePath, sourceLineNumber);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.WriteLine(message, conditionName, memberName, sourceFilePath, sourceLineNumber);
		}

		[DebuggerNonUserCode, DebuggerHidden]
		public static void WriteLine(bool condition, string message = null,
			[CallerArgumentExpression("condition")] string? conditionName = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			_internal.DiagHelper.WriteLine(condition, message, conditionName, memberName, sourceFilePath,
				sourceLineNumber);
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


			//private static System.Diagnostics.DebugProvider _provider;
			[DebuggerNonUserCode] //, DebuggerHidden]
			public static void Assert(bool condition, string message = null,
				[CallerArgumentExpression("condition")] string? conditionName = null,
				[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
				[CallerLineNumber] int sourceLineNumber = 0)
			{
				if (condition)
				{
					return;
				}

				message ??= "Assert condition failed";

				//Debug.Assert(false, (string)$"ASSERT({conditionName}) {message}");
				//DoAssertFail($"ASSERT({conditionName}) {message}");
				DoAssertFail("ASSERT", message, conditionName, memberName, sourceFilePath, sourceLineNumber);

			}


			private static HashSet<string> _assertOnceLookup = new();


			/// <summary>
			/// assert for the given message only once
			/// </summary>
			/// <param name="condition"></param>
			/// <param name="message"></param>
			[DebuggerNonUserCode, DebuggerHidden]
			public static void AssertOnce(bool condition, string message = null,
				[CallerArgumentExpression("condition")] string? conditionName = null,
				[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
				[CallerLineNumber] int sourceLineNumber = 0)
			{
				if (condition)
				{
					return;
				}

				//message ??= "Assert condition failed";
				var assertKey = $"{sourceFilePath}:{sourceLineNumber}:{message}";
				lock (_assertOnceLookup)
				{
					if (_assertOnceLookup.Add(assertKey) == false)
					{
						return;
					}
				}

				//Debug.Assert(false, "ASSERT ONCE: " + message);
				//Debug.Assert(false,(string)$"ASSERT_ONCE({conditionName}) {message}");
				//DoAssertFail($"ASSERT_ONCE({conditionName}) {message}");
				DoAssertFail("ASSERT_ONCE", message, conditionName, memberName, sourceFilePath, sourceLineNumber);
			}

			[DebuggerNonUserCode, DebuggerHidden]
			public static void Throw(bool condition, string message = null,
				[CallerArgumentExpression("condition")] string? conditionName = null,
				[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
				[CallerLineNumber] int sourceLineNumber = 0)
			{
				if (condition == true)
				{
					return;
				}

				message ??= "Throw condition failed";

				//Assert(false, message, conditionName, memberName, sourceFilePath,sourceLineNumber);
				//throw new(message);
				throw new($"THROW({conditionName}) {message}");
			}

			[DebuggerNonUserCode, DebuggerHidden]
			public static void WriteLine(string message,
				[CallerArgumentExpression("condition")] string? conditionName = null,
				[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
				[CallerLineNumber] int sourceLineNumber = 0)
			{
				//Console.WriteLine(message);
				DoWrite("WriteLine", message, conditionName, memberName, sourceFilePath, sourceLineNumber);
			}

			[DebuggerNonUserCode, DebuggerHidden]
			public static void WriteLine(bool condition, string message = null,
				[CallerArgumentExpression("condition")] string? conditionName = null,
				[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
				[CallerLineNumber] int sourceLineNumber = 0)
			{
				if (condition == true)
				{
					return;
				}

				DoWrite("WriteLine", message, conditionName, memberName, sourceFilePath, sourceLineNumber);
			}

			[DebuggerNonUserCode, DebuggerHidden]
			private static void DoWrite(string eventName, string message, string? conditionName = null,
				string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
			{
				//pretty color printout to console using the ANSI.Console nuget package.   
				var timeFormat = DateTime.Now.ToString("HH:mm:ss.ffff").Color(ConsoleColor.Gray).Bold();
				var eventFormat = $"{eventName}".Color(ConsoleColor.DarkBlue).Bold().Background(ConsoleColor.White);
				var conditionFormat = $"{conditionName}".Color(ConsoleColor.Red).Bold().Background(ConsoleColor.Black);
				var callsiteFormat = $"{sourceFilePath._GetAfter('\\', true)}:{sourceLineNumber}({memberName})"
					.Color(ConsoleColor.Magenta).Background(ConsoleColor.Black).Bold();
				var messageFormat = message.Color(ConsoleColor.White).Bold().Background(ConsoleColor.Black);
				Console.WriteLine($"{timeFormat}-{callsiteFormat}[{eventFormat}({conditionFormat})]{messageFormat}");

				// If a different ansi color package is needed, can try Spectre.Console.  https://spectreconsole.net/
				//new Spectre.Console.Markup("test",
				//	Spectre.Console.Style.WithForeground(Spectre.Console.Color.Grey)
				//		.Combine(Spectre.Console.Style.WithDecoration(Spectre.Console.Decoration.Bold)));

				//Spectre.Console.AnsiConsole.
			}


			[DebuggerNonUserCode, DebuggerHidden]
			[DoesNotReturn]
			private static void DoAssertFail(string eventName, string message, string? conditionName = null,
				string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
			{
				//log that an assert occurs to the console
				DoWrite(eventName, message, conditionName, memberName, sourceFilePath, sourceLineNumber);
				if (Debugger.IsAttached == false)
				{
					Debugger.Launch();
				}

				if (Debugger.IsAttached)
				{
					Debugger.Break();
				}
				else
				{
					//failed to break into debugger.  crash the app instead.
					var ex = new Exception(message);
					Environment.FailFast(ex.Message, ex);
				}
			}
		}
	}

	/// <summary>
	/// help do complex tasks with the GC.
	/// </summary>
	public static class __GcHelper
	{
		public static void ForceFullCollect()
		{
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}


		private static GcTimingDetails _lastGcTimingDetails = new();

		public static ref GcTimingDetails GetGcTimings()
		{
			var gcDetails = new GcTimingDetails();
			gcDetails.g0Count = GC.CollectionCount(0);
			gcDetails.g1Count = GC.CollectionCount(1);
			gcDetails.g2Count = GC.CollectionCount(2);


			gcDetails.currentGcCount = gcDetails.g0Count + gcDetails.g1Count + gcDetails.g2Count;
			if (gcDetails.currentGcCount == _lastGcTimingDetails.currentGcCount)
			{
				return ref _lastGcTimingDetails;
			}


			var lifetimeAllocBytes = GC.GetTotalAllocatedBytes();
			var currentAllocBytes = GC.GetTotalMemory(false);

			//see https://devblogs.microsoft.com/dotnet/the-updated-getgcmemoryinfo-api-in-net-5-0-and-how-it-can-help-you/

			//get info on different kinds of gc https://docs.microsoft.com/en-us/dotnet/api/system.gckind?f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.GCKind);k(DevLang-csharp)%26rd%3Dtrue&view=net-6.0
			{
				gcDetails.infoEphemeral = GC.GetGCMemoryInfo(GCKind.Ephemeral);
				gcDetails.infoBackground = GC.GetGCMemoryInfo(GCKind.Background);
				gcDetails.infoFullBlocking = GC.GetGCMemoryInfo(GCKind.FullBlocking);
			}

			_lastGcTimingDetails = gcDetails;
			return ref _lastGcTimingDetails;
		}

		public struct GcTimingDetails
		{
			public int g0Count, g1Count, g2Count, currentGcCount;
			public GCMemoryInfo infoEphemeral, infoBackground, infoFullBlocking;
			private string cachedString;

			public override string ToString()
			{
				if (cachedString == null)
				{
					var counts = $"{currentGcCount} (0={g0Count}/1={g1Count}/2={g2Count})";
					var pauses =
						$"{(infoEphemeral.PauseDurations._Sum() + infoBackground.PauseDurations._Sum() + infoFullBlocking.PauseDurations._Sum()).TotalMilliseconds:00.0}ms(EP={infoEphemeral.PauseDurations._Sum().TotalMilliseconds:00}/BG={infoBackground.PauseDurations._Sum().TotalMilliseconds:00}/FB={infoFullBlocking.PauseDurations._Sum().TotalMilliseconds:00})";

					cachedString = $"counts={counts} pause={pauses}";
				}

				return cachedString;
			}

		}
	}


	/// <summary>
	/// a stopwatch that checks for spikes (2x percentile 100 sample) and logs it.
	/// <para>use via the .Lap() method</para>
	/// <para>Only writes to console every pollSkipFrequency (ie:100 calls) AND ONLY IF the consoleWriteSensitivityFactor and consoleWriteThreshholdMs parameters are fulfilled</para>
	/// </summary>
	public class PerfSpikeWatch
	{
		public string Name { get; init; }

		public double consoleWriteSensitivityFactor, consoleWriteThreshholdMs;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name">name displayed when console output</param>
		/// <param name="consoleWriteSensitivityFactor">default x2.0.  if the p100 sample isn't this times the p50 or more, it won't be displayed to console.</param>
		/// <param name="consoleWriteThreshholdMs">default 1ms, if the p100 sample is not more than this much greater than average, it won't be displayed to console</param>
		/// <param name="pollSkipFrequency">how often to show results to console.  Default of 100 means every 100 calls to LapAndReset() will write a summary of results to console.</param>
		public PerfSpikeWatch(string? name = null, double consoleWriteSensitivityFactor = 2.0,
			double consoleWriteThreshholdMs = 1.0, int pollSkipFrequency = 100)
		{
			if (name == null)
			{
				name = "";
			}

			this.consoleWriteSensitivityFactor = consoleWriteSensitivityFactor;
			this.consoleWriteThreshholdMs = consoleWriteThreshholdMs;
			this.pollSkipFrequency = pollSkipFrequency;
			//name += $"({sourceFilePath._GetAfter('\\', true)}:{sourceLineNumber})";

			Name = name.PadRight(20);
		}

		public Stopwatch sw = new Stopwatch();
		public PercentileSampler800<TimeSpan> sampler = new();

		/// <summary>
		/// how often to show results to console.  Default of 100 means every 100 calls to LapAndReset() will write a summary of results to console.
		/// </summary>
		public int pollSkipFrequency;

		private int _lapCount = 0;
		private Percentiles<TimeSpan> _lastPollPercentiles;

		private string _caller;

		/////// <summary>
		/////// if the absolute difference in the p100 sample from median is less than this, we don't report a p100 spike in the summary.
		/////// </summary>
		////public double p100SpikeIgnorePaddingMs = 3.0;
		//private static string _spikeP100MessageDefault = " "._Repeat(18);
		public void Start()
		{
			sw.Start();
		}

		public void Stop()
		{
			sw.Stop();
		}


		public void Restart()
		{
			sw.Restart();
		}

		public void Reset()
		{
			sw.Reset();
		}

		/// <summary>
		/// mark and store the current time, and restart the counter, restarting immediately.
		/// <para>Only writes to console every pollSkipFrequency (ie:100 calls) AND ONLY IF the p100 is 2x or more the p50 times, unless you specify otherwise via the alwaysShow=true parameter</para>
		/// </summary>
		/// <param name="memberName"></param>
		/// <param name="sourceFilePath"></param>
		/// <param name="sourceLineNumber"></param>
		//[Conditional("CHECKED")]
		public void Lap([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			var elapsed = sw.Elapsed;
			sw.Restart();
			sampler.RecordSample(elapsed);
			_lapCount++;

			//debugging scratch
			//if (Name.StartsWith("[-----Bogus---]")==false)
			//{
			//	return;
			//}
			//once we fill up, do logging if circumstances dictate
			if (sampler.IsFilled && (_lapCount % pollSkipFrequency == 0))
			{
				var percentiles = sampler.GetPercentiles();
				if (_lastPollPercentiles.sampleCount == 0)
				{
					_lastPollPercentiles = percentiles;
					return;
				}

				if ((percentiles.p100 >= percentiles.p50 * this.consoleWriteSensitivityFactor &&
				     percentiles.p100 > _lastPollPercentiles.p100 * this.consoleWriteSensitivityFactor)
				    && ((percentiles.p100 - percentiles.p50).TotalMilliseconds >= this.consoleWriteThreshholdMs)
				   )
				{
					if (_caller == null)
					{
						_caller = $"{sourceFilePath._GetAfter('\\', true)}:{sourceLineNumber}";
					}

					//var spikeP100Message = _spikeP100MessageDefault;
					//if ((percentiles.p100 - percentiles.p50).TotalMilliseconds > p100SpikeIgnorePaddingMs)
					//{
					var spikeP100Message = $"spike p100={percentiles.p100.TotalMilliseconds._Round(2)}ms.";
					//}

					var message =
						//$"PERFSPIKEWATCH {Name}({_caller}): spike p100={percentiles.p100.TotalMilliseconds._Round(2)}ms.  " +
						$"{Name}: {spikeP100Message}  " +
						$"currentStats={percentiles.ToString((val) => val.TotalMilliseconds._Round(2))}   " +
						$"priorStats={_lastPollPercentiles.ToString((val) => val.TotalMilliseconds._Round(2))} " +
						$"gcTimings={__GcHelper.GetGcTimings()}";
					__ERROR.WriteLine(message, conditionName: "PERFSPIKEWATCH Lap", memberName: memberName,
						sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
				}

				_lastPollPercentiles = percentiles;
			}
		}

		//[Conditional("CHECKED")]
		/// <summary>
		/// records the lap, stops and resets the counter.    Be sure to call .Start() after this.
		/// <para>Only writes to console every pollSkipFrequency (ie:100 calls) AND ONLY IF the p100 is 2x or more the p50 times, unless you specify otherwise via the alwaysShow=true parameter</para>
		/// </summary>
		/// <param name="alwaysShow">Only writes to console every pollSkipFrequency (ie:100 calls) AND ONLY IF the p100 is 2x or more the p50 times, unless you specify otherwise via the alwaysShow=true parameter</param>
		/// <param name="memberName"></param>
		/// <param name="sourceFilePath"></param>
		/// <param name="sourceLineNumber"></param>
		public void LapAndReset([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Lap(memberName, sourceFilePath, sourceLineNumber);
			sw.Reset();
		}
	}

}
