// See https://aka.ms/new-console-template for more information

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


manager.Register(new A { ParentName = "root", Name = "A" });

//add some test nodes
//execManager.Register(new A());
//execManager.Register(new B());
//execManager.Register(new C());
//execManager.Register(new B("b2"));
//execManager.Register(new B("b3"));
//execManager.Register(new B("b1"));

while (true)
{

lastElapsed = Stopwatch.GetTimestamp() - start;
start = Stopwatch.GetTimestamp();
await manager.Update(TimeSpan.FromTicks(lastElapsed));
//Console.WriteLine($"last Elapsed = {lastElapsed}");
}


public class A : SimNode
{
	public override async Task Update(Frame frame)
	{
		//Console.WriteLine("WHUT");
		Console.WriteLine($"{Name} frame {frame}");
	}
}