








using System.Diagnostics;

Console.WriteLine(System.Environment.SystemPageSize);


var frame = 0;
float avgMs = 0;
while (true)
{
	frame++;
	var sw = Stopwatch.StartNew();
	await Task.Delay(TimeSpan.FromMilliseconds(1));
	var elapsed = sw.ElapsedMilliseconds;
	avgMs = (avgMs + elapsed)/2;

	if (frame % 200 == 0)
	{
		Console.WriteLine($"frame {frame}   avgMs={MathF.Round(avgMs,2)}  curMs={elapsed}");
	}

}


