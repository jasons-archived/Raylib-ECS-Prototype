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




public class Program
{
	public static void Main(string[] args)
	{

		//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new BenchmarkDotNet.Configs.DebugInProcessConfig());
#if DEBUG
		//var toBenchmark = new DelegatesBenchmark();

		//toBenchmark.SpanParallel_MemoryOwner_Array();

		//toBenchmark.SpanParallel_Default_MemoryOwner_Array();
#endif


		//var summary = BenchmarkRunner.Run<DelegatesBenchmark>();
	}
}
