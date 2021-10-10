using NotNot.Engine._internal.ExecPipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scratch_console.Helpers.Pipeline;


public class DebugPrint : SimNode
{
	//public DebugPrint(string name, SimManager manager, string parentName) : base(name, manager, parentName)
	//{
	//}
	//public DebugPrint(string name, SimManager manager, SimNode parent) : base(name, manager, parent)
	//{
	//}
	private long avgMs = 0;
	protected override async Task OnUpdate(Frame frame, NodeFrameState nodeState)
	{
		//if (frame._stats._frameId % 200 == 0)
		{
			Console.WriteLine($"{Name} frame {frame} stats={_lastUpdate}");
		}

	}
}
public class TimestepNodeTest : FixedTimestepNode
{
	//public TimestepNodeTest(string name, SimManager manager, string parentName) : base(name, manager, parentName)
	//{
	//}
	//public TimestepNodeTest(string name, SimManager manager, SimNode parent) : base(name, manager, parent)
	//{
	//}

	protected override async Task OnUpdate(Frame frame, NodeFrameState nodeState)
	{
		await Task.Delay(0);
		//Console.WriteLine("WHUT");
		//if (frame._stats._frameId % 200 == 0)
		{
			var indent = HierarchyDepth * 3;
			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}");
		}
		//await Task.Delay(100000);
	}
}
public class DelayTest : SimNode
{
	//public DelayTest(string name, SimManager manager, string parentName) : base(name, manager, parentName)
	//{
	//}
	//public DelayTest(string name, SimManager manager, SimNode parent) : base(name, manager, parent)
	//{
	//}
	private Random _rand = new();
	protected override async Task OnUpdate(Frame frame, NodeFrameState nodeState)
	{
		if (Name == "C")
		{
			Console.WriteLine($"{Name} frame {frame} stats={_lastUpdate}");
		}
		////Console.WriteLine("WHUT");
		if (frame._stats._frameId % 200 == 0)
		{
			var indent = HierarchyDepth * 3;

			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}       START");
			//await Task.Delay(_rand.Next(10,100));
			//__DEBUG.Assert(false);
			Console.WriteLine($"{Name.PadLeft(indent + Name.Length)}       END");
		}
	}
}
