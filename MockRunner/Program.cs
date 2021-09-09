// See https://aka.ms/new-console-template for more information

//Console.WriteLine("Hello, World!");


using System;
using System.Diagnostics;
using DumDum.Engine;

var execManager = new ExecManager();



var start = Stopwatch.GetTimestamp();
long lastElapsed = 0;
while (true)
{
	lastElapsed = Stopwatch.GetTimestamp() - start;
	start = Stopwatch.GetTimestamp();
	execManager.Update(TimeSpan.FromTicks(lastElapsed));
	
}


