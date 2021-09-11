// See https://aka.ms/new-console-template for more information

//Console.WriteLine("Hello, World!");


using System;
using System.Diagnostics;
using DumDum.Engine;

var execManager = new ExecManager();



var start = Stopwatch.GetTimestamp();
long lastElapsed = 0;


execManager.BeginRun();

//add some test nodes
execManager.Register(new A());
execManager.Register(new B());
execManager.Register(new C());
execManager.Register(new B("b2"));
execManager.Register(new B("b3"));
execManager.Register(new B("b1"));

while (true)
{
	
	lastElapsed = Stopwatch.GetTimestamp() - start;
	start = Stopwatch.GetTimestamp();
	execManager.Update(TimeSpan.FromTicks(lastElapsed));
	//Console.WriteLine($"last Elapsed = {lastElapsed}");
}


