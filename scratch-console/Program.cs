using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DumDum.Bcl;
using DumDum.Bcl.Diagnostics;
using DumDum.Engine.Allocation;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.Toolkit.HighPerformance.Helpers;

namespace MyBenchmarks;


//[MemoryDiagnoser]
//public class Md5VsSha256
//{
//	private const int N = 10000;
//	private readonly byte[] data;

//	private readonly SHA256 sha256 = SHA256.Create();
//	private readonly MD5 md5 = MD5.Create();

//	public Md5VsSha256()
//	{
//		data = new byte[N];
//		new Random(42).NextBytes(data);
//	}

//	[Benchmark]
//	public byte[] Sha256() => sha256.ComputeHash(data);

//	[Benchmark]
//	public byte[] Md5() => md5.ComputeHash(data);
//	[Benchmark]
//	public byte[] MdFake() => new byte[data.Length];
//}

//public class Program
//{
//	public static void Main(string[] args)
//	{
//		var summary = BenchmarkRunner.Run<Md5VsSha256>();
//	}
//}

//[ShortRunJob]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class AllocatorBenchmark
{

	public MemoryOwner<long> externalIdsOwner;
	public HashSet<long> evenSet = new HashSet<long>();
	public HashSet<long> oddSet = new HashSet<long>();

	[Params(10000)]
	public int EntityCount { get; set; }

	[Params(true,false)]
	public bool AutoPack { get; set; }

	[Params(100,1000,10000)]
	public int ChunkSize { get; set; }

	[Params(1f,4f)]
	public float PBatchX { get; set; }


	[Params(10,100)]
	public int Allocators { get; set; }

	//[IterationSetup]
	//public void Temp()
	//{

	//}

	[GlobalSetup]
	public void Setup()
	{
		externalIdsOwner = MemoryOwner<long>.Allocate(EntityCount);
		var externalIds = externalIdsOwner.Span;
		var set = new HashSet<long>();
		while (set.Count < externalIds.Length)
		{
			set.Add(__.Rand.NextInt64());
		}
		var count = 0;
		foreach (var id in set)
		{
			externalIds[count] = id;
			count++;
		}
		__ERROR.Throw(count == EntityCount);



		//split into 2 groups
		foreach (var externalId in externalIds)
		{
			if (externalId % 2 == 0)
			{
				evenSet.Add(externalId);
			}
			else
			{
				oddSet.Add(externalId);
			}
		}



	}

	[Benchmark]
	public async Task Parallel_CreateEditDelete()
	{
		await Allocator.__TEST_Unit_ParallelAllocators(AutoPack, ChunkSize, externalIdsOwner, PBatchX, Allocators, evenSet, oddSet);
	}

}


public class Program
{
	public static void Main(string[] args)
	{
#if DEBUG

		//run in debug mode (can hit breakpoints in VS)
		var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new BenchmarkDotNet.Configs.DebugInProcessConfig());		
//run a specific benchmark
		//var summary = BenchmarkRunner.Run<Parallel_Lookup>(new BenchmarkDotNet.Configs.DebugInProcessConfig());
#else
		var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
	}
}
