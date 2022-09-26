// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace NotNot.Bcl.Diagnostics.Advanced;

/// <summary>
/// extra info about the debugger
/// </summary>
public static class DebuggerInfo
{

	/// <summary>
	/// helper to detect when a debugger is attached and actively stepping through code.
	/// Useful for preventing subsystem timeouts due to a paused debugger.
	/// </summary>
	/// <remarks>
	/// I hacked together a "solution" that fires up a long-lived task that resets a stopwatch every 250ms.   so if the time between resets exceeds 1 second I consider the app "paused".   Not very elegant so I'd love to hear suggestions.
	///	I think I need this Debugger.IsPaused thingy so I can Assert if a task may be deadlocked(runs too long )...   basically some debugging helper for a multithreading game engine I'm writing
	///So if I screw up and deadlock somewhere I don't have to stare at a blank screen wondering what I did wrong.
	///Not having a Debugger.IsPaused solution means that whenever I step through in a debugger, my "deadlock assert" code would trigger because everything runs too long
	/// </remarks>
	public static bool IsPaused { get; private set; }

	static DebuggerInfo()
	{
		//new Task<int>(() => { }).
		Task.Factory.StartNew()
		Task.Factory.StartNew()
		
		//Task.Run()
		//new Task(() => { }).st
		//start a long-lived task that just loops, looking for when the application is paused (being stepped-into in the debugger)
		new Task(async () =>
		{
			var heartbeatSw = Stopwatch.StartNew();
			while (true)
			{
				heartbeatSw.Restart();
				await Task.Delay(TimeSpan.FromSeconds(0.25));
				var elapsed = heartbeatSw.Elapsed;
				if (IsPaused == true)
				{
					//already flagged as debugging, so need to run "fast" to unflag
					if (elapsed < TimeSpan.FromSeconds(0.3))
					{
						IsPaused = false;
					}
				}
				else
				{
					//we are not flagged as active debugging, so only if the pause exceeds 1 second we say debugging.
					if (elapsed > TimeSpan.FromSeconds(1))
					{
						IsPaused = true;
					}
				}
			}
		}, TaskCreationOptions.LongRunning).Start();
	}
}



