﻿// See https://aka.ms/new-console-template for more information

//Console.WriteLine("Hello, World!");


using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DumDum.Engine.Ecs;


using System;
using System.Threading.Tasks;
//static async Task Main(string[] args)
//{
//	var task = Task.Run(() => { Console.WriteLine("HELLO"); });
//	await task;
//	Console.WriteLine("Done, press any key to exit");
//	Console.ReadKey();
//}


//var task = Task.Run(() => { Console.WriteLine("HELLO"); });

//await task;



var manager = new SimManager() { };



var start = Stopwatch.GetTimestamp();
long lastElapsed = 0;


manager.Register(new DebugPrint { ParentName = "root", Name = "DebugPrint", _updateBefore = { "A" } });
manager.Register(new HierarchyTest { ParentName = "root", Name = "A" });
manager.Register(new HierarchyTest { ParentName = "root", Name = "A2" });

manager.Register(new HierarchyTest { ParentName = "A", Name = "B", _updateBefore = { "A2" } });
manager.Register(new HierarchyTest { ParentName = "A", Name = "B2" });
manager.Register(new HierarchyTest { ParentName = "A", Name = "B3" }); 
manager.Register(new DelayTest { ParentName = "A", Name = "B4!", _updateAfter = { "A2" } });

manager.Register(new HierarchyTest { ParentName = "B", Name = "C" });
manager.Register(new HierarchyTest { ParentName = "B", Name = "C2" });
manager.Register(new HierarchyTest { ParentName = "B", Name = "C3", _updateAfter = { "AA" } }); //bug

//add some test nodes
//execManager.Register(new A());
//execManager.Register(new B());
//execManager.Register(new C());
//execManager.Register(new B("b2"));
//execManager.Register(new B("b3"));
//execManager.Register(new B("b1"));

var loop = 0;
while (true)
{
	loop++;
	lastElapsed = Stopwatch.GetTimestamp() - start;
	start = Stopwatch.GetTimestamp();
	//Console.WriteLine($" ======================== {loop} ({Math.Round(TimeSpan.FromTicks(lastElapsed).TotalMilliseconds,1)}ms) ============================================== ");
	await manager.Update(TimeSpan.FromTicks(lastElapsed));
	//Console.WriteLine($"last Elapsed = {lastElapsed}");
}


public class DebugPrint : SimNode
{
	public override async Task Update(Frame frame)
	{
		//Console.WriteLine("WHUT");
		if (frame._stats._frameId % 200 == 0)
		{
			Console.WriteLine($"{Name} frame {frame}");
		}
		//await Task.Delay(100000);
	}
}
public class HierarchyTest : SimNode
{
	public override async Task Update(Frame frame)
	{
		await Task.Delay(0);
		//Console.WriteLine("WHUT");
		if (frame._stats._frameId % 200 == 0)
		{
			var indent = GetHierarchyChain().Count * 3;
			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}");
		}
		//await Task.Delay(100000);
	}
}
public class DelayTest : SimNode
{
	public override async Task Update(Frame frame)
	{
		await Task.Delay(0);
		////Console.WriteLine("WHUT");
		if (frame._stats._frameId % 200 == 0)
		{
			var indent = GetHierarchyChain().Count * 3;
			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}");
		}
	}
}