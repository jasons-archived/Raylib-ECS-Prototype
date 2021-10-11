

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBenchmarks;

[SimpleJob(runStrategy: BenchmarkDotNet.Engines.RunStrategy.Throughput, launchCount: 1)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
internal class EcsEndToEndBenchmark
{

	//[Benchmark]
	//public 




}
