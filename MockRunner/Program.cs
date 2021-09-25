// See https://aka.ms/new-console-template for more information

//Console.WriteLine("Hello, World!");


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DumDum.Bcl;
using DumDum.Bcl.Diagnostics;
using DumDum.Engine.Allocation;
using DumDum.Engine.Ecs;
using DumDum.Engine.Sim;


//static async Task Main(string[] args)
//{
//	var task = Task.Run(() => { Console.WriteLine("HELLO"); });
//	await task;
//	Console.WriteLine("Done, press any key to exit");
//	Console.ReadKey();
//}


//var task = Task.Run(() => { Console.WriteLine("HELLO"); });

//await task;

//Task.WaitAll(Task.Delay(100000));
//Console.WriteLine("hi");

Allocator.__TEST_Unit_SingleAllocator();

Allocator.__TEST_Unit_SeriallAllocators();



Allocator.__TEST_Unit_ParallelAllocators();

var manager = new SimManager() { };


var testClass = new MyClass<int>();
testClass.GetStorage();




var start = Stopwatch.GetTimestamp();
long lastElapsed = 0;


//add some test nodes
manager.Register(new TimestepNodeTest { ParentName = "root", Name = "A", TargetFps=1 });
manager.Register(new DelayTest { ParentName = "root", Name = "A2" });

manager.Register(new DelayTest { ParentName = "A", Name = "B", _updateBefore = { "A2" }, _writeResources = {"taco" } });
manager.Register(new DelayTest { ParentName = "A", Name = "B2", _readResources = { "taco" } });
manager.Register(new DelayTest { ParentName = "A", Name = "B3", _readResources = { "taco" } }); 
manager.Register(new DelayTest { ParentName = "A", Name = "B4!", _updateAfter = { "A2" }, _readResources = { "taco" } });

manager.Register(new DelayTest { ParentName = "B", Name = "C" });
manager.Register(new DelayTest { ParentName = "B", Name = "C2" });
manager.Register(new DelayTest { ParentName = "B", Name = "C3", _updateAfter = { "C" } });




manager.Register(new DebugPrint { ParentName = "C3", Name = "DebugPrint" });














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

//[DebuggerVisualizer(typeof(List<T>),)]
public class MyClass<T>
{
	public List<T> GetStorage()
	{
		//return a list from somewhere...
		return new List<T>();
	}
	private List<T> Storage { get => GetStorage(); }
}

public class DebugPrint : SimNode
{
	private long avgMs = 0;
	protected override async Task Update(Frame frame, NodeFrameState nodeState)
	{
		//if (frame._stats._frameId % 200 == 0)
		{
			Console.WriteLine($"{Name} frame {frame} stats={_lastUpdate}");
		}		
		
	}
}
public class TimestepNodeTest : FixedTimestepNode
{
	
	protected override async Task Update(Frame frame, NodeFrameState nodeState)
	{
		await Task.Delay(0);
		//Console.WriteLine("WHUT");
		//if (frame._stats._frameId % 200 == 0)
		{
			var indent = GetHierarchy().Count * 3;
			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}");
		}
		//await Task.Delay(100000);
	}
}
public class DelayTest : SimNode
{
	private Random _rand = new();
	protected override async Task Update(Frame frame, NodeFrameState nodeState)
	{
		if (Name == "C")
		{
			Console.WriteLine($"{Name} frame {frame} stats={_lastUpdate}");
		}
		////Console.WriteLine("WHUT");
		if (frame._stats._frameId % 200 == 0)
		{
			var indent = GetHierarchy().Count * 3;
			
			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}       START");
			//await Task.Delay(_rand.Next(10,100));
			//__DEBUG.Assert(false);
			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}       END");
		}
	}
}