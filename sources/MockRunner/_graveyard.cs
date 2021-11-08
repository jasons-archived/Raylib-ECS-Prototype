// See https://aka.ms/new-console-template for more information

//Console.WriteLine("Hello, World!");


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine.Ecs.Allocation;
using NotNot.Engine.Ecs;
using NotNot.Engine.Internal.SimPipeline;
using Microsoft.Toolkit.HighPerformance.Buffers;



//////}
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



//////MemoryOwner<long> externalIdsOwner;
//////HashSet<long> evenSet = new HashSet<long>();
//////HashSet<long> oddSet = new HashSet<long>();

//////void TestSetup(int EntityCount)
//////{
//////	externalIdsOwner = MemoryOwner<long>.Allocate(EntityCount);
//////	var externalIds = externalIdsOwner.Span;
//////	var set = new HashSet<long>();
//////	while (set.Count < externalIds.Length)
//////	{
//////		set.Add(__.Rand.NextInt64());
//////	}
//////	var count = 0;
//////	foreach (var id in set)
//////	{
//////		externalIds[count] = id;
//////		count++;
//////	}
//////	__ERROR.Throw(count == EntityCount);



//////	//split into 2 groups
//////	foreach (var externalId in externalIds)
//////	{
//////		if (externalId % 2 == 0)
//////		{
//////			evenSet.Add(externalId);
//////		}
//////		else
//////		{
//////			oddSet.Add(externalId);
//////		}
//////	}



//////TestSetup(10000);
////////Allocator.__TEST_Unit_SingleAllocator();
////////Allocator.__TEST_Unit_SingleAllocator_AndEdit(true,100,externalIdsOwner,evenSet,oddSet);

//////////Allocator.__TEST_Unit_SeriallAllocators();
//////////Allocator.__TEST_Unit_SeriallAllocators();
//////////Allocator.__TEST_Unit_SeriallAllocators();
//////////Allocator.__TEST_Unit_SeriallAllocators();

//////var startedTest = Stopwatch.StartNew();

//////await Allocator.__TEST_Unit_ParallelAllocators(true,100,externalIdsOwner,1,10000,evenSet,oddSet);

//////Console.WriteLine($"test elapsed = {startedTest.Elapsed.TotalMilliseconds._Round(2)}ms");



//System.Environment.Exit(0);



//var engine = new NotNot.Engine.Engine();
//var updater = new NotNot.Engine.SimpleUpdater();
//engine.Updater = updater;
//engine.Initialize();
//updater.Run();

//engine.Dispose();

//var tests = new Tests.Internals.EngineBasic();
//await tests.Engine_WorldWithChild();



//var engine = new NotNot.Engine.Engine();
//engine.Updater = new NotNot.Engine.HeadlessUpdater();
//engine.Initialize();

//engine.Updater.Start();


//await Task.Delay(100);

//await engine.Updater.Stop();
//__ERROR.Assert(engine.DefaultWorld._lastUpdate._timeStats._frameId > 30);
//engine.Dispose();
//__ERROR.Throw(engine.IsDisposed);

