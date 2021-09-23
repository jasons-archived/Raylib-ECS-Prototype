using System;
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
public class DelegatesBenchmark
{
	private const int SPAN_SIZE = 10000;
	private const int CHUNK_SIZE = 1000;
	private Dictionary<long, AllocToken> _lookup = new(SPAN_SIZE);



	//[IterationCleanup]
	public void IterationCleanup()
	{
		// Disposing logic
		_lookup.Clear();
	}


	private bool Verify(Span<long> externalIds, Span<AllocToken> allocTokens)
	{
		var toReturn = true;
		//__ERROR.Throw(false, "whut");
		for (int i = 0; i < externalIds.Length; i++)
		{
			if (externalIds[i] != i)
			{
				//throw new ArgumentException("invalid span");

				toReturn = false;
			}
		}
		AllocToken allocToken = default;
		for (int i = 0; i < allocTokens.Length; i++)
		{
			GenAllocTokenRef(i, ref allocToken);

			if (allocTokens[i] != allocToken)
			{
				//throw new ArgumentException("invalid span");

				toReturn = false;
			}
		}
		return toReturn;
	}

	private void GenAllocTokenRef(int i, ref AllocToken allocToken)
	{

		allocToken = new AllocToken()
		{
			externalId = i,
			allocatorId = 0,
			allocSlot = new AllocSlot(i / CHUNK_SIZE, i % CHUNK_SIZE),
			isAlive = true,
			packVersion = 0,

		};

		//throw new InvalidOperationException("BOOM");
	}
	private AllocToken GenAllocToken(int i)
	{
		return new AllocToken()
		{
			externalId = i,
			allocatorId = 0,
			allocSlot = new AllocSlot(i / CHUNK_SIZE, i % CHUNK_SIZE),
			isAlive = true,
			packVersion = 0,

		};
	}

	private void PopulateExternalIdsSpan(Span<long> span)
	{
		for (int i = 0; i < span.Length; i++)
		{
			span[i] = i;
		}
	}

	private void PopulateExternalIdsSpan_Parallel(Span<long> span)
	{
		span._ParallelForEach((ref long item, ref int index) =>
		{
			item = index;


		});
		//for (int i = 0; i < span.Length; i++)
		//{
		//	span[i] = i;
		//}
	}


	[Benchmark(Baseline = true)]
	public bool NoDelegate_Default_StackAlloc()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		Span<long> externalIds = stackalloc long[SPAN_SIZE];
		Span<AllocToken> allocTokens = stackalloc AllocToken[SPAN_SIZE];

		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}
		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}
		allocTokens.Sort();




		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	#region NoDelegate
	[Benchmark]
	public bool NoDelegate_OneBox()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		Span<long> externalIds = stackalloc long[SPAN_SIZE];
		Span<AllocToken> allocTokens = stackalloc AllocToken[SPAN_SIZE];



		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}
		__ERROR.Throw(lookup.Count == SPAN_SIZE);
		object box = null;
		//create span of allocTokens and sort
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
			if (box == null || box.GetHashCode() < i)
			{
				box = i + lookup.Count;
			}


			if (box.GetHashCode() == 0)
			{
				throw new Exception("BOOM");
			}
		}
		allocTokens.Sort();




		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	[Benchmark]
	public bool NoDelegate_OneObjectAlloc()
	{
		object obj = new object();

		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		Span<long> externalIds = stackalloc long[SPAN_SIZE];
		Span<AllocToken> allocTokens = stackalloc AllocToken[SPAN_SIZE];




		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}
		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}

		}
		allocTokens.Sort();

		if (obj.GetHashCode() == lookup.Count)
		{
			allocTokens.Sort();
		}



		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	[Benchmark]
	public bool NoDelegate_OneObjectAllocPerLoop()
	{


		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		Span<long> externalIds = stackalloc long[SPAN_SIZE];
		Span<AllocToken> allocTokens = stackalloc AllocToken[SPAN_SIZE];




		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}
		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				object obj = new object();
				allocTokens[i] = allocToken;
				i++;

				if (obj.GetHashCode() == i + lookup.Count)
				{
					allocTokens.Sort();
				}
			}

		}
		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	[Benchmark]
	public bool NoDelegate_OneObjectAllocSimple()
	{
		object obj = new object();

		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		Span<long> externalIds = stackalloc long[SPAN_SIZE];
		Span<AllocToken> allocTokens = stackalloc AllocToken[SPAN_SIZE];




		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}
		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}

		}
		allocTokens.Sort();



		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}

	[Benchmark]
	public bool NoDelegate_NaiveAlloc()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		Span<long> externalIds = new long[SPAN_SIZE];
		Span<AllocToken> allocTokens = new AllocToken[SPAN_SIZE];

		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}
		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}
		allocTokens.Sort();




		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	[Benchmark]
	public bool NoDelegate_NoForeach()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		Span<long> externalIds = stackalloc long[SPAN_SIZE];
		Span<AllocToken> allocTokens = stackalloc AllocToken[SPAN_SIZE];

		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		//{
		//	var i = 0;
		//	foreach (var (externalId, allocToken) in lookup)
		//	{
		//		allocTokens[i] = allocToken;
		//		i++;
		//	}
		//}
		for (var i = 0; i < lookup.Count; i++)
		{
			var externalId = (long)i;
			var allocToken = lookup[externalId];
			allocTokens[i] = allocToken;
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}

	[Benchmark]
	public bool NoDelegate_SpanOwner()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = SpanOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = soExternalIds.Span; //stackalloc long[SPAN_SIZE];
		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.Span;// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	[Benchmark]
	public bool NoDelegate_SpanOwner_Array()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = SpanOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];
		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}

	[Benchmark]
	public bool NoDelegate_SpanOwner_NoUsing()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		//using var soExternalIds = SpanOwner<long>.Allocate(SPAN_SIZE);
		//using var soAllocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = SpanOwner<long>.Allocate(SPAN_SIZE).Span; //stackalloc long[SPAN_SIZE];
		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE).Span;// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}


	[Benchmark]
	public bool NoDelegate_MemoryOwner()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = MemoryOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = soExternalIds.Span; //stackalloc long[SPAN_SIZE];
		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.Span;// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	[Benchmark]
	public bool NoDelegate_MemoryOwner_Array()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = MemoryOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];
		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}

	[Benchmark]
	public bool NoDelegate_MemoryOwner_NoUsing()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		//using var soExternalIds = SpanOwner<long>.Allocate(SPAN_SIZE);
		//using var soAllocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = MemoryOwner<long>.Allocate(SPAN_SIZE).Span; //stackalloc long[SPAN_SIZE];
		PopulateExternalIdsSpan(externalIds);

		//create allocToken lookups
		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE).Span;// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	#endregion NoDelegate


	#region Parallel


	[Benchmark]
	public bool SpanParallel_Default_MemoryOwner_Array()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = MemoryOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];
																	//PopulateExternalIdsSpan_Parallel(externalIds);
		externalIds._ParallelForEach((ref long item, ref int index) =>
		{
			item = index;


		});






		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();





		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}
	private static void _SpanWorker(ref long item, ref int index)
	{
		item = index;
	}


	[Benchmark]
	public bool SpanParallel_StaticMethod()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = MemoryOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE);



		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];
																	//PopulateExternalIdsSpan_Parallel(externalIds);
		externalIds._ParallelForEach(_SpanWorker);


		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();

		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}



	[Benchmark]
	unsafe public bool SpanParallel_StaticMethodDelegatePtr()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = MemoryOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE);

		delegate*<ref long, ref int, void> p_SpanWorker = &_SpanWorker;

		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];
																	//PopulateExternalIdsSpan_Parallel(externalIds);
		externalIds._ParallelForEach(p_SpanWorker);


		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();

		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}

	unsafe delegate*<ref long, ref int, void> p_SpanWorker = &_SpanWorker;
	[Benchmark]
	unsafe public bool SpanParallel_StaticMethodDelegatePtr_classMember()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = MemoryOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE);

		delegate*<ref long, ref int, void> tmp = &_SpanWorker;

		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];

		//PopulateExternalIdsSpan_Parallel(externalIds);
		externalIds._ParallelForEach(p_SpanWorker);


		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();

		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}



	/// <summary>
	/// important implementation notes, be sure to read https://docs.microsoft.com/en-us/windows/communitytoolkit/high-performance/parallelhelper
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	private unsafe readonly struct _ParallelHelper_ActionHelper : IAction
	{
		public readonly long* pSpan;

		public _ParallelHelper_ActionHelper(long* pSpan)
		{
			this.pSpan = pSpan;
		}

		public void Invoke(int index)
		{
			pSpan[index] = index;
		}
	}

	[Benchmark]
	unsafe public bool SpanParallel_ParallelHelperStandard()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = SpanOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE);

		

		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];

		//PopulateExternalIdsSpan_Parallel(externalIds);
		fixed (long* pSpan = externalIds)
		{
			var action = new _ParallelHelper_ActionHelper(pSpan);
			ParallelHelper.For(0, externalIds.Length, in action);
		}
		
		


		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();

		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}



	[Benchmark]
	public bool SpanParallel_NoWorkVerify()
	{
		//Dictionary<long, AllocToken> lookup = new(SPAN_SIZE);
		var lookup = _lookup;
		using var soExternalIds = SpanOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE);
			

		Span<long> externalIds = soExternalIds.DangerousGetArray(); //stackalloc long[SPAN_SIZE];

		////PopulateExternalIdsSpan_Parallel(externalIds);
		//fixed (long* pSpan = externalIds)
		//{
		//	var action = new _ParallelHelper_ActionHelper(pSpan);
		//	ParallelHelper.For(0, externalIds.Length, in action);
		//}
		for(var i = 0; i < externalIds.Length; i++)
		{
			externalIds[i] = i;
		}




		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.DangerousGetArray();// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();

		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}


	[Benchmark]
	public bool SpanParallel_Parallel_ForEach()
	{
		var lookup = _lookup;
		using var soExternalIds = SpanOwner<long>.Allocate(SPAN_SIZE);
		using var soAllocTokens = SpanOwner<AllocToken>.Allocate(SPAN_SIZE);

		Span<long> externalIds = soExternalIds.Span; //stackalloc long[SPAN_SIZE];

		//PopulateExternalIdsSpan_Parallel(externalIds);
		//fixed (long* pSpan = externalIds)
		//{
		//	var action = new _ParallelHelper_ActionHelper(pSpan);
		//	ParallelHelper.For(0, externalIds.Length, in action);
		//}

		var array = soExternalIds.DangerousGetArray();
		Parallel.For(0, externalIds.Length, (index) => {
			array[index] = index;
		});




		for (var i = 0; i < externalIds.Length; i++)
		{
			var allocToken = GenAllocToken(i);

			lookup.Add(i, allocToken);
		}

		__ERROR.Throw(lookup.Count == SPAN_SIZE);

		//create span of allocTokens and sort
		Span<AllocToken> allocTokens = soAllocTokens.Span;// stackalloc AllocToken[SPAN_SIZE];
		{
			var i = 0;
			foreach (var (externalId, allocToken) in lookup)
			{
				allocTokens[i] = allocToken;
				i++;
			}
		}

		allocTokens.Sort();

		var toReturn = Verify(externalIds, allocTokens);
		IterationCleanup();
		return toReturn;
	}


	private ValueTask _ValueTaskHelper(ArraySegment<long> externalIds, int index)
	{
		externalIds[index] = index;
		return ValueTask.CompletedTask;
	}


	private unsafe async Task _TestHelper(long pExternalIds)
	{

	}


	//[Benchmark]
	//public async Task<bool> SpanParallel_ValueTask()
	//{
	//	var lookup = _lookup;
	//	using var soExternalIds = MemoryOwner<long>.Allocate(SPAN_SIZE);
	//	using var soAllocTokens = MemoryOwner<AllocToken>.Allocate(SPAN_SIZE);

	//	var externalIds = soExternalIds.DangerousGetArray();


	//	using var soValueTasks = MemoryOwner<ValueTask>.Allocate(16);


		
	//	var vtSpan = soValueTasks.DangerousGetArray();


	//	//PopulateExternalIdsSpan_Parallel(externalIds);


	//	for (var i = 0; i < externalIds.Count; i++)
	//	{
	//		var taskIndex = i % soValueTasks.Length;
	//		if (vtSpan[taskIndex].IsCompleted == false)
	//		{
	//			await vtSpan[taskIndex];
	//		}
	//		//var result = _ValueTaskHelper(externalIds, i);
	//		var result = Task.Run(()=>_ValueTaskHelper(externalIds, i));

	//		var result  = Task.Run(async () => { 
	//			var result = _ValueTaskHelper(externalIds, i);
	//		});

	//		Task.va
	//	}



	//	var array = soExternalIds.DangerousGetArray();
	//	Parallel.For(0, externalIds.Length, (index) =>
	//	{
	//		array[index] = index;
	//	});




	//	for (var i = 0; i < externalIds.Length; i++)
	//	{
	//		var allocToken = GenAllocToken(i);

	//		lookup.Add(i, allocToken);
	//	}

	//	__ERROR.Throw(lookup.Count == SPAN_SIZE);

	//	//create span of allocTokens and sort
	//	Span<AllocToken> allocTokens = soAllocTokens.Span;// stackalloc AllocToken[SPAN_SIZE];
	//	{
	//		var i = 0;
	//		foreach (var (externalId, allocToken) in lookup)
	//		{
	//			allocTokens[i] = allocToken;
	//			i++;
	//		}
	//	}

	//	allocTokens.Sort();

	//	var toReturn = Verify(externalIds, allocTokens);
	//	IterationCleanup();
	//	return toReturn;
	//}


	#endregion
}

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


		var summary = BenchmarkRunner.Run<DelegatesBenchmark>();
	}
}
