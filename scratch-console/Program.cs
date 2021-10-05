using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using BenchmarkDotNet.Running;
using DumDum.Bcl;
using DumDum.Bcl.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.Toolkit.HighPerformance.Helpers;

namespace MyBenchmarks;






public class Program
{
	public static async Task Main(string[] args)
	{
#if DEBUG
		var bm = new AllocatorBenchmark();
		bm.Setup();
		await bm.Sequential_CreateEditDelete();
		await bm.Parallel_CreateEditDelete();
		//run in debug mode (can hit breakpoints in VS)
		//var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new BenchmarkDotNet.Configs.DebugInProcessConfig());		
		//run a specific benchmark
		var summary = BenchmarkRunner.Run<AllocatorBenchmark>(new BenchmarkDotNet.Configs.DebugInProcessConfig());
#else
		var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
	}
}
