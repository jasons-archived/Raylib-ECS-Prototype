// See https://aka.ms/new-console-template for more information


using RaylibScratch;
using System.Diagnostics;

Console.WriteLine("Main thread start");

var renderSystem = new RenderSystem();
renderSystem.Start();

var swMainLoop = new Stopwatch();
var loopCount = 0;

ThreadPool.SetMaxThreads(4, 4);
while (true)
{
	await renderSystem.renderGate.WaitAsync();
	loopCount++;
	swMainLoop.Restart();
	List<Task> tasks = new();
	List<Object> objects = new();
	for (var i = 0; i < 50; i++)
	{
		objects.Add((object)i);
		;
		var task = Task.Run(async () =>
		{
			var rand = new Random();
			var awaitTask = Task.Delay(rand.Next(10));
			Thread.SpinWait(100); //increase some time of main loop
			await awaitTask;
		});
		tasks.Add(task);
	}
	await Task.WhenAll(tasks);

	//Thread.SpinWait(10000000);

	if (renderSystem.renderTask.IsCompleted)
	{
		break;
	}

	

	var mainLoopElapsedMs = swMainLoop.ElapsedMilliseconds;
	if (loopCount % 1000 == 0 ||
		mainLoopElapsedMs > 70)
	{
		Console.WriteLine($"mainLoop={(int)mainLoopElapsedMs}   GC={GCInfo.Get()}");
	}

}

await renderSystem.renderTask;

Console.WriteLine("Main thread done");

//while (true)
//{
//	await renderSystem.renderGate.WaitAsync();
//	List<Object> objects = new();
//	for (var i = 0; i < 20;)
//	{
//		objects.Add((object)i);
//	}
//	if (renderSystem.renderTask.IsCompleted)
//	{
//		break;

//	}

//}

public static class GCInfo
{

	public static string Get()
	{

		float totalPauseMs = 0;
		foreach (var pauses in GC.GetGCMemoryInfo(GCKind.Any).PauseDurations)
		{
			totalPauseMs += (float)pauses.TotalMilliseconds;
		}

		var gcCountSum = 0;
		var gcCounts = "";
		for (var i = 0; i < GC.MaxGeneration; i++)
		{
			var count = GC.CollectionCount(i);
			gcCountSum += count;
			gcCounts += $"{count},";
		}

		return $"counts={gcCountSum}({gcCounts})  times={totalPauseMs}";
	}
}
