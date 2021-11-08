using BenchmarkDotNet.Attributes;
using NotNot.Ecs.Allocation;

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

[SimpleJob(runStrategy: BenchmarkDotNet.Engines.RunStrategy.Throughput, launchCount: 1)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class AllocatorBenchmark
{

	//public MemoryOwner<EntityHandle> externalIdsOwner;
	//public HashSet<EntityHandle> evenSet =new();
	//public HashSet<EntityHandle> oddSet = new();
	public EntityRegistry entityRegistry = new();

	[Params(//10,
		100)]
	public int Allocators { get; set; } = 100;

	[Params(100000)]
	public int EntityCount { get; set; } = 10000;


	[Params(1000
		,10000
		)]
	public int ChunkSize { get; set; } = 1000;

	[Params(1f
		,4f
		,16f
		)]
	public float PBatchX { get; set; } = 1f;




	[Params(true, false)]
	public bool AutoPack { get; set; } = true;


	//[IterationSetup]
	//public void Temp()
	//{

	//}

	[GlobalSetup]
	public void Setup()
	{
		//externalIdsOwner = MemoryOwner<EntityHandle>.Allocate(EntityCount);
		//var externalIds = externalIdsOwner.Span;
		//var set = new HashSet<EntityHandle>();
		//while (set.Count < externalIds.Length)
		//{
		//	set.Add(new(__.Rand.NextInt64()));
		//}
		//var count = 0;
		//foreach (var id in set)
		//{
		//	externalIds[count] = id;
		//	count++;
		//}
		//__ERROR.Throw(count == EntityCount);



		////split into 2 groups
		//foreach (var externalId in externalIds)
		//{
		//	if (externalId.id % 2 == 0)
		//	{
		//		evenSet.Add(externalId);
		//	}
		//	else
		//	{
		//		oddSet.Add(externalId);
		//	}
		//}



	}

	
	//	[Benchmark]
	public async Task Sequential_CreateEditDelete()
	{
		Page.__TEST_Unit_SinglePage_AndEdit(entityRegistry, AutoPack, ChunkSize, EntityCount);
	}
	[Benchmark]
	public async Task Parallel_CreateEditDelete()
	{
		await Page.__TEST_Unit_ParallelPages(entityRegistry, AutoPack, ChunkSize, EntityCount, Allocators, PBatchX);
	}

}
