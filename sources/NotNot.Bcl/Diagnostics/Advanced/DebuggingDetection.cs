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

namespace NotNot.Bcl.Diagnostics.Advanced;

/// <summary>
/// helper to detect when a debugger is attached and actively stepping through code.
/// Useful for preventing subsystem timeouts due to a paused debugger.
/// </summary>
public static class DebuggingDetection
{

	public static bool IsPaused { get; private set; }

	static DebuggingDetection()
	{

		new Task(async () =>
		{
			var heartbeatSw = Stopwatch.StartNew();
			while (true)
			{
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
				heartbeatSw.Restart();
			}
		}, TaskCreationOptions.LongRunning).Start();
	}
}



