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
using System.Text;
using System.Threading.Tasks;

namespace Tests.Internals;


[TestClass]
public class BclBasic
{

	[TestMethod]
	public async Task DotNextAsyncAutoResetEvent2()
	{
		//DotNext.Threading.AsyncAutoResetEvent autoResetEvent = new(false);
		Nito.AsyncEx.AsyncAutoResetEvent autoResetEvent = new(false);

		var loopCount = 0;
		var setCount = 0;
		var consumedCount = 0;
		var timer = Stopwatch.StartNew();
		var lastSecondReported = 0;
		var producerTask = Task.Run(async () =>
		{
			try
			{
				while (true)
				{
					loopCount++;
					//var didSet = autoResetEvent.Set();
					var didSet = autoResetEvent.IsSet == false;										
					if (didSet)
					{
						autoResetEvent.Set();
						setCount++;
					}

					if (timer.Elapsed > TimeSpan.FromSeconds(lastSecondReported))
					{
						var tup1 = new { loopCount };
						var tup = new { loopCount, setCount, consumedCount };
						Console.WriteLine($"t={lastSecondReported}sec:  {new { loopCount, setCount, consumedCount }}");
						lastSecondReported++;
					}

					if (timer.Elapsed > TimeSpan.FromSeconds(30))
					{
						break;
					}
					if (__.Rand._NextBoolean())
					{
						//extra wait
						System.Threading.Thread.SpinWait(__.Rand.Next(10000));
						//await Task.Delay(__.Rand.Next(10));
						await Task.Delay(0);						
					}
				}
			}
			finally
			{
				Console.WriteLine("ProducerTask end");
			}
		});


		var consumerTask = Task.Run(async () =>
		{
			try
			{
				while (true)
				{
					await autoResetEvent.WaitAsync();
					//var success = await autoResetEvent.WaitAsync(TimeSpan.FromMilliseconds(1));
					var success = true;
					if (success)
					{
						consumedCount++;
					}
					if (producerTask.IsCompleted)
					{
						break;
					}
					if (__.Rand._NextBoolean())
					{
						//extra wait
						System.Threading.Thread.SpinWait(__.Rand.Next(10000));
						//await Task.Delay(__.Rand.Next(10));
						await Task.Delay(0);
					}
				}
			}
			finally
			{
				Console.WriteLine("ConsumerTask end");
			}
		});


		await producerTask;
		autoResetEvent.Set();
		await consumerTask;

	}

	/// <summary>
	/// this deadlocks, illustrating failure or DotNext.
	/// </summary>
	/// <returns></returns>
	public async Task DotNextAsyncAutoResetEvent()
	{
		DotNext.Threading.AsyncAutoResetEvent autoResetEvent = new(false);

		var loopCount = 0;
		var setCount = 0;
		var consumedCount = 0;
		var timer = Stopwatch.StartNew();
		var lastSecondReported = 0;
		var producerTask = Task.Run(() =>
		{
			try
			{
				while (true)
				{
					loopCount++;
					var didSet = autoResetEvent.Set();
					if (didSet)
					{
						setCount++;
					}

					if (timer.Elapsed > TimeSpan.FromSeconds(lastSecondReported))
					{
						var tup1 = new { loopCount };
						var tup = new { loopCount, setCount, consumedCount };
						Console.WriteLine($"t={lastSecondReported}sec:  {new { loopCount, setCount, consumedCount }}");
						lastSecondReported++;
					}

					if (timer.Elapsed > TimeSpan.FromSeconds(30))
					{
						break;
					}
				}
			}
			finally
			{
				Console.WriteLine("ProducerTask end");
			}
		});


		var consumerTask = Task.Run(async () =>
		{
			try
			{
				while (true)
				{
					var success = await autoResetEvent.WaitAsync(TimeSpan.FromMilliseconds(1));
					if (success)
					{
						consumedCount++;
					}
					if (producerTask.IsCompleted)
					{
						break;
					}
				}
			}
			finally
			{
				Console.WriteLine("ConsumerTask end");
			}
		});


		await producerTask;
		autoResetEvent.Set();
		await consumerTask;


	}
}
