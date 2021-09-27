using DumDum.Bcl;
using DumDum.Bcl.Collections._unused;
using DumDum.Bcl.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.Toolkit.HighPerformance.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DumDum.Engine.Allocation;

/// <summary>
/// just a hint that the derived object is a component.  not actually needed.
/// </summary>
public interface IComponent
{

}











/// <summary>
/// only good for the current frame, unless chunk packing is disabled.  in that case it's good for lifetime of entity in the archetype.
/// </summary>
public readonly record struct AllocToken : IComparable<AllocToken>
{
	public readonly bool isAlive { get; init; }
	public readonly long externalId { get; init; }


	/// <summary>
	/// the id of the allocator that created/tracks this allocSlot.  //TODO: make the archetype also use this ID as it's own (on create allocator, use it's ID)
	/// If needed, the allocator can be accessed via `Allocator._GLOBAL_LOOKUP(allocatorId)`
	/// </summary>
	public readonly int allocatorId { get; init; }
	/// <summary>
	/// used to verify allocator was not replaced with another
	/// </summary>
	public readonly int allocatorVersion { get; init; }
	public readonly AllocSlot allocSlot { get; init; }
	/// <summary>
	/// needs to match Allocator._packVersion, otherwise a pack took place and the token needs to be refreshed.
	/// </summary>
	public readonly int packVersion { get; init; }
	//	/// <summary>
	//	/// can be used to directly find a chunk from `Chunk[TComponent]._GLOBAL_LOOKUP(chunkId)`
	//	/// </summary>
	//	public long GetChunkLookupId()
	//	{
	//		//create a long from two ints
	//		//long correct = (long)left << 32 | (long)(uint)right;  //from: https://stackoverflow.com/a/33325313/1115220
	//#if CHECKED

	//		var chunkLookup = new ChunkLookupId { allocatorId = allocatorId, columnChunkIndex = allocSlot.columnChunkIndex };
	//		var chunkLookupId = chunkLookup._packedValue;
	//		var toReturn = (long)allocatorId << 32 | (uint)allocSlot.columnChunkIndex;
	//		//var toReturn = (long)allocSlot.columnChunkIndex<< 32 | (uint)allocatorId;
	//		__CHECKED.Throw(chunkLookupId == toReturn, "ints to long is wrong");
	//#endif




	//		//return (long)allocSlot.columnChunkIndex << 32 | (uint)allocatorId;
	//		return (long)allocatorId << 32 | (uint)allocSlot.columnChunkIndex;
	//	}

	public ref T GetComponentWriteRef<T>()
	{
		var (result, reason) = GetAllocator().CheckIsValid(this);
		__ERROR.Throw(result, reason);

		//_CHECKED_VerifyInstance<T>();
		var chunk = GetContainingChunk<T>();
		return ref chunk.GetWriteRef(this);

	}
	public ref readonly T GetComponentReadRef<T>()
	{
		var (result, reason) = GetAllocator().CheckIsValid(this);
		__ERROR.Throw(result, reason);

		//_CHECKED_VerifyInstance<T>();
		var chunk = GetContainingChunk<T>();
		return ref chunk.GetReadRef(this);
	}
	public Chunk<T> GetContainingChunk<T>()
	{
		//_CHECKED_VerifyInstance<T>();
		//var chunkLookupId = GetChunkLookupId();

		//lock (Chunk<T>._GLOBAL_LOOKUP)
		//{
		//	if (!Chunk<T>._GLOBAL_LOOKUP.TryGetValue(chunkLookupId, out var chunk))
		//	{
		//		//if (!Chunk<T>._GLOBAL_LOOKUP.TryGetValue(chunkLookupId, out chunk))
		//		{
		//			__ERROR.Throw(GetAllocator().HasComponentType<T>(), $"the archetype this element is attached to does not have a component of type {typeof(T).FullName}. Be aware that base classes do not match.");
		//			//need to refresh token
		//			__ERROR.Throw(false, "the chunk this allocToken points to does not exist.  either entity was deleted or it was packed.  Do not use AllocTokens beyond the frame aquired unless archetype.allocator.AutoPack==false");
		//		}
		//	}
		//	return chunk;
		//}

		var column = Chunk<T>._LOOKUP._storage[allocatorId];
		__DEBUG.Throw(column.Count > allocSlot.chunkIndex, "chunk doesn't exist");
		var chunk = column._AsSpan_Unsafe()[allocSlot.chunkIndex];
		__DEBUG.Throw(chunk != null);
		return chunk;


	}
	/// <summary>
	/// Get allocator this token is associated with.  If a null is returned or an exception is thrown, it is likely our AllocToken is out of date.
	/// </summary>
	public Allocator GetAllocator()
	{
		return Allocator._GLOBAL_LOOKUP.Span[allocatorId];

		//var toReturn = Allocator._GLOBAL_LOOKUP.Span[allocatorId];
		//__ERROR.Throw(toReturn != null && toReturn._version == allocatorVersion, "alloc token seems to be expired");
		//__CHECKED.Throw(toReturn._allocatorId == allocatorId);

		//return toReturn;
	}
	//[Conditional("CHECKED")]
	//private void _CHECKED_VerifyInstance<T>()
	//{
	//	if (typeof(T) == typeof(AllocMetadata))
	//	{
	//		//otherwise can cause infinite recursion
	//		return;
	//	}
	//	ref readonly var allocMetadata = ref GetComponentReadRef<AllocMetadata>();
	//	__CHECKED.Throw(allocMetadata.allocToken == this, "mismatch");
	//	//get chunk via the allocator, where the default way is direct through the Chunk<T>._GLOBAL_LOOKUP
	//	var atomId = Allocator.Atom.GetId<T>();

	//	//var chunk = GetAllocator()._componentColumns[typeof(AllocMetadata)][allocSlot.columnChunkIndex] as Chunk<T>;
	//	var chunk = GetAllocator()._GetColumnsSpan()[atomId][allocSlot.chunkIndex] as Chunk<T>;
	//	__CHECKED.Throw(GetContainingChunk<T>() == chunk, "chunk lookup between both techniques does not match");
	//	var allocMetaChunk = GetContainingChunk<AllocMetadata>();
	//	__CHECKED.Throw(this == allocMetaChunk.Span[allocSlot.rowSlotIndex].allocToken);
	//}

	public int CompareTo(AllocToken other)
	{
		return allocSlot.CompareTo(other.allocSlot);
	}




	//public override string ToString()
	//{
	//	if (isInit)
	//	{
	//		return base.ToString();
	//	}
	//	else
	//	{
	//		return "AllocToken [NOT INITIALIZED]";
	//	}
	//}
}


/// <summary>
/// chunk -> row => slot
/// on it's own, this is not enough to access an entities components, you need the alocator.   See <see cref="AllocToken"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public record struct AllocSlot : IComparable<AllocSlot>
{

	[FieldOffset(0)]
	private long _packedValue;

	/// <summary>
	/// can be used to directly find a chunk from `Chunk[TComponent]._GLOBAL_LOOKUP(chunkId)`
	/// </summary>
	//[FieldOffset(0)]
	//public int chunkId;

	//[FieldOffset(0)]
	//public short allocatorId;
	/// <summary>
	/// the index to the slot from inside the chunk.
	/// </summary>
	[FieldOffset(0)]
	public int rowSlotIndex;

	/// <summary>
	/// the location of the chunk in the column
	/// </summary>
	[FieldOffset(4)]
	public int chunkIndex;

	public AllocSlot(//short allocatorId, 
		int columnIndex, int chunkRowIndex)
	{
		this = default;
		//this.allocatorId = allocatorId;
		this.chunkIndex = columnIndex;
		this.rowSlotIndex = chunkRowIndex;
	}

	public long GetChunkLookupId(int allocatorId)
	{
		//return (long)allocatorId << 32 | (uint)columnChunkIndex;
		return (long)allocatorId << 32 | (uint)chunkIndex;
	}

	public int CompareTo(AllocSlot other)
	{
		return _packedValue.CompareTo(other._packedValue);
	}

	public static bool operator <(AllocSlot left, AllocSlot right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator <=(AllocSlot left, AllocSlot right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >(AllocSlot left, AllocSlot right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator >=(AllocSlot left, AllocSlot right)
	{
		return left.CompareTo(right) >= 0;
	}
}


public partial class Allocator //unit test
{


	public static async Task __TEST_Unit_ParallelAllocators(bool autoPack, int chunkSize, MemoryOwner<long> externalIds, float batchSizeMultipler,int allocatorCount, HashSet<long> evenSet, HashSet<long> oddSet)
	{

		var PARALLEL_LOOPS = allocatorCount;
		var execCount = 0;
		await ParallelFor.Range(0, PARALLEL_LOOPS, batchSizeMultipler, (start, endExclusive) =>
		{
			var tempCount = 0;
			for (var i = start; i < endExclusive; i++)
			{
				__TEST_Unit_SingleAllocator_AndEdit(autoPack,chunkSize,externalIds,evenSet,oddSet);
				tempCount++;
			}
			Interlocked.Add(ref execCount, tempCount);

			return ValueTask.CompletedTask;

		});
		__ERROR.Throw(execCount == PARALLEL_LOOPS);



	}
	[Conditional("TEST")]
	public static unsafe void __TEST_Unit_SeriallAllocators(bool autoPack, int chunkSize, MemoryOwner<long> externalIds, int allocatorCount, HashSet<long> evenSet, HashSet<long> oddSet)
	{
		var count = allocatorCount;
		using var allocOwner = SpanPool<Allocator>.Allocate(count);
		var allocs = allocOwner.Span;
		for (var i = 0; i < count; i++)
		{
			allocs[i] = _TEST_HELPER_CreateAndEditAllocator(autoPack,chunkSize,externalIds, evenSet, oddSet);
		}
		for (var i = 0; i < count; i++)
		{
			allocs[i].Dispose();
		}
		allocs.Clear();
		//var result = Parallel.For(0, 10000, (index) => __TEST_Unit_SingleAllocator());
		//__ERROR.Throw(result.IsCompleted);

	}

	[Conditional("TEST")]
	public static unsafe void __TEST_Unit_SingleAllocator()
	{
		var allocator = _TEST_HELPER_CreateAllocator();

		allocator.Dispose();
	}


	[Conditional("TEST")]
	public static unsafe void __TEST_Unit_SingleAllocator_AndEdit(bool autoPack, int chunkSize, MemoryOwner<long> externalIds, HashSet<long> evenSet, HashSet<long> oddSet)
	{
		var allocator = _TEST_HELPER_CreateAndEditAllocator(autoPack,chunkSize,externalIds, evenSet, oddSet);

		allocator.Dispose();
	}

	private static unsafe Allocator _TEST_HELPER_CreateAllocator()
	{
		var allocator = new Allocator()
		{
			AutoPack = __.Rand._NextBoolean(),
			ChunkSize = __.Rand.Next(1, 100),
			ComponentTypes = new() { typeof(int), typeof(string) },


		};
		allocator.Initialize();

		using var externalIdsOwner = SpanPool<long>.Allocate(__.Rand.Next(0, 1000));
		var set = new HashSet<long>();
		var externalIds = externalIdsOwner.Span;
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
		//Span<long> externalIds = stackalloc long[] { 2, 4, 8, 7, -2 };
		using var tokensOwner = SpanPool<AllocToken>.Allocate(externalIds.Length);
		var tokens = tokensOwner.Span;
		allocator.Alloc(externalIds, tokens);
		return allocator;
	}

	private static unsafe Allocator _TEST_HELPER_CreateAndEditAllocator(bool autoPack, int chunkSize, MemoryOwner<long> externalIdsOwner, HashSet<long> evenSet, HashSet<long> oddSet)
	{
		var allocator = new Allocator()
		{
			AutoPack = autoPack,
			ChunkSize = chunkSize,
			ComponentTypes = new() { typeof(int), typeof(string) },


		};
		allocator.Initialize();

		//using var externalIdsOwner = SpanPool<long>.Allocate(__.Rand.Next(0, 1000));
		//var set = new HashSet<long>();
		//var externalIds = externalIdsOwner.Span;
		//while (set.Count < externalIds.Length)
		//{
		//	set.Add(__.Rand.NextInt64());
		//}

		//var count = 0;
		//foreach (var id in set)
		//{
		//	externalIds[count] = id;
		//	count++;
		//}

		var externalIds = externalIdsOwner.Span;

		//Span<long> externalIds = stackalloc long[] { 2, 4, 8, 7, -2 };
		using var tokensOwner = SpanPool<AllocToken>.Allocate(externalIds.Length);
		var tokens = tokensOwner.Span;
		allocator.Alloc(externalIds, tokens);


		//test edits
		for (var i = 0; i < tokens.Length; i++)
		{
			var token = tokens[i];


			ref var num = ref token.GetComponentWriteRef<int>();
			__ERROR.Throw(num == 0);
			num = i;
			var numRead = token.GetComponentReadRef<int>();
			__ERROR.Throw(numRead == i);

			num = i + 1;
			numRead = token.GetComponentReadRef<int>();
			__ERROR.Throw(numRead == i + 1);


			ref var myStr = ref token.GetComponentWriteRef<string>();
			__ERROR.Throw(myStr == null);
			myStr = $"hello {i}";
			__ERROR.Throw(token.GetComponentReadRef<string>() == myStr);


			ref var numExId = ref token.GetComponentWriteRef<int>();
			__ERROR.Throw(num == numExId);
			numExId = (int)token.externalId;
			__ERROR.Throw(num == numExId);

		}


		//delete odds
		allocator.Free(oddSet.ToArray());

		//verify that evens still here
		{
			var i = 0;
			foreach (var (externalId, allocToken) in allocator._lookup)
			{
				if (allocToken.externalId % 2 != 0)
				{
					__ERROR.Throw(false, "only even should exist");
				}

				var num = allocToken.GetComponentReadRef<int>();
				__ERROR.Throw(num == (int)allocToken.externalId);
				__ERROR.Throw(allocToken.GetComponentReadRef<string>().StartsWith("hello"));
				i++;

			}
		}
		//same verify only evens,
		foreach (var (externalId, allocToken) in allocator._lookup)
		{
			ref var numExId = ref allocToken.GetComponentWriteRef<int>();
			__ERROR.Throw(numExId % 2 == 0);
			__ERROR.Throw(allocToken.externalId % 2 == 0);
			__ERROR.Throw(allocToken.GetComponentReadRef<string>().StartsWith("hello"));
		}
		//add odds
		using var oddTokens = SpanPool<AllocToken>.Allocate(oddSet.Count);
		var oddSpan = oddTokens.Span;
		allocator.Alloc(oddSet.ToArray(), oddSpan);

		//delete evens
		allocator.Free(evenSet.ToArray());

		//verify only odds
		foreach (var (externalId, allocToken) in allocator._lookup)
		{
			ref var numExId = ref allocToken.GetComponentWriteRef<int>();
			__ERROR.Throw(numExId == 0, "we just wrote a new entity, old data should be blown away");
			__ERROR.Throw(allocToken.externalId % 2 == 1);
			__ERROR.Throw(allocToken.GetComponentReadRef<string>() == null);
		}

		//delete odds again
		allocator.Free(oddSet.ToArray());


		//verify empty
		__ERROR.Throw(allocator._lookup.Count == 0);


		return allocator;
	}
}

public partial class Allocator  //ATOM logic
{

	protected internal static class Atom
	{
		/**
		 * 
		 * Tanner Gooding — Today at 3:44 PM
			I'd recommend looking at an ATOM based system

			that is, your issue is you have a bunch of keys (the type) and using the hashcode in a dictionary is expensive for your scenario
			however, the number of types you need to support isn't likely most of them, its probably a small subset (like 1000-10k)

			so you can have a system that offsets most of the dictionary cost to be "1 time" by mapping it to an incremental key (sometimes called an atom)
			that key can then be used for constant time indexing into an array

			this is how a lot of Windows/Unix work internally for the xprocess windowing support
			almost every string is registered as a ushort ATOM, which is really just used as the index into a linear array
			and then everything else carries around the ATOM, not the string (or in your case the TYPE)
			its similar in concept to primary keys in a database, or similar

			or maybe its not primary keys, I'm bad with databases; but there is a "key" like concept in databases that corresponds to integers rather than actual values

			Zombie — Today at 3:48 PM
			In other words, you're basically turning a Type into an array index as early as you can, and you just pass that around
			So then your code doesn't need to query the dictionary nearly as much
			Tanner Gooding — Today at 3:49 PM
			right, which is similar to what GetHashCode does
			the difference being that GetHashCode is "random"
			while ATOM is explicitly incremental and "registered"

			Zombie — Today at 3:49 PM
			You'd have an API like Atom RegisterComponent<T>()
			And you'd pass around that Atom instead of T
			or in addition to T
			Tanner Gooding — Today at 3:50 PM
			so rather than needing to do buckets and stuff based on arbitrary integers (what dictionaries do)
			you can just do array[atom]

			yeah, no need for Atom to be an allocation
			its just a simple integer that is itself a dictionary lookup over the Type
			and a small registration lock if an entry doesn't exist to handle multi-threading

			so its a 1 time cost up front
			and a basically zero cost for anything that already has the atom, which should be most things
		 * 
		*/





		private abstract class AtomHelper
		{
			/// <summary>
			/// don't start at zero so that signifies an error
			/// </summary>
			protected static int _counter = 1;
			public abstract int GetId();
		}
		private class AtomHelper<T> : AtomHelper
		{
			public static int _atomId;

			static AtomHelper()
			{
				_atomId = Interlocked.Increment(ref _counter);
				lock (_typeLookup)
				{
					_typeLookup.Add(typeof(T), _atomId);
					_atomIdLookup.Add(_atomId, typeof(T));
				}
			}

			public override int GetId()
			{
				return _atomId;
			}
		}

		public static int GetId<T>()
		{
			return AtomHelper<T>._atomId;
		}
		public static Type GetType(int atomId)
		{
			lock (_typeLookup)
			{
				return _atomIdLookup[atomId];
			}
		}

		private static Dictionary<Type, int> _typeLookup = new();

		private static Dictionary<int, Type> _atomIdLookup = new();
		public static int GetId(Type type)
		{
			lock (_typeLookup)
			{
				if (_typeLookup.TryGetValue(type, out var atomId))
				{
					return atomId;
				}
			}
			{
				//make atomId
				var helperType = typeof(AtomHelper<>).MakeGenericType(type);
				var helper = Activator.CreateInstance(helperType) as AtomHelper;
				var newAtomId = helper.GetId();
				lock (_typeLookup)
				{
					if (_typeLookup.TryGetValue(type, out var atomId))
					{
						__ERROR.Throw(newAtomId == atomId);
						return atomId;
					}
					_typeLookup.Add(type, newAtomId);
					_atomIdLookup.Add(atomId, type);
					return newAtomId;

				}
			}
		}
	}

}

/// <summary>
/// allocator for archetypes 
/// </summary>
public partial class Allocator : IDisposable //init logic
{

	//public static int _allocatorId_GlobalCounter;
	//public static Dictionary<int, Allocator> _GLOBAL_LOOKUP = new();
	//public int _allocatorId = _allocatorId_GlobalCounter._InterlockedIncrement();
	public static AllocSlotList<Allocator> _GLOBAL_LOOKUP = new();
	public int _allocatorId = -1;
	private static int _versionCounter;
	public int _version = _versionCounter++;

	/// <summary>
	/// if you want to add additional custom components to each entity, list them here.  These are not used to compute the <see cref="_componentsHashId"/>
	/// <para>be sure not to remove the items already in the list.</para>
	/// </summary>
	public List<Type> CustomMetaComponentTypes = new List<Type>() { typeof(AllocMetadata) };


	public List<Type> ComponentTypes { get; init; }


	/// <summary>
	/// used to quickly identify what collection of ComponentTypes this allocator is in charge of
	/// </summary>
	public int _componentsHashId;


	//public Dictionary<Type, List<Chunk>> _componentColumns = new();
	/// <summary>
	/// ATOM_ID --> chunkId --> rowId --> The_Component
	/// </summary>
	public List<List<Chunk>> _columnStorage = new();

	public Span<List<Chunk>> _GetColumnsSpan() { return _columnStorage._AsSpan_Unsafe(); }

	/// <summary>
	/// all the atomId's used in columns.  can use the atomId to get the offset to the proper column
	/// </summary>
	public List<int> _atomIdsUsed = new();

	public List<Chunk> GetColumn<T>()
	{
		var atomId = Atom.GetId<T>();
		return _GetColumnsSpan()[atomId];
	}
	public Chunk<T> GetChunk<T>(ref AllocToken allocToken)
	{
		var (result, reason) = CheckIsValid(ref allocToken);
		__ERROR.Throw(result, reason);

		__CHECKED_INTERNAL_VerifyAllocToken(ref allocToken);
		var column = GetColumn<T>();
		return column[allocToken.allocSlot.chunkIndex] as Chunk<T>;
	}
	public ref T GetComponentRef<T>(ref AllocToken allocToken)
	{
		var chunk = GetChunk<T>(ref allocToken);
		return ref chunk.Span[allocToken.allocSlot.rowSlotIndex];
	}

	public static ref T GetComponent<T>(ref AllocToken allocToken)
	{
		return ref _GLOBAL_LOOKUP.Span[allocToken.allocatorId].GetComponentRef<T>(ref allocToken);
		//var atomId = Atom.GetId<T>();
		//var chunk = _GLOBAL_LOOKUP.Span[allocToken.allocatorId]._GetColumnsSpan()[atomId]._AsSpan_Unsafe()[allocToken.allocSlot.columnChunkIndex] as Chunk<T>;
		//return ref chunk.Span[allocToken.allocSlot.chunkRowIndex];
	}


	/// <summary>
	/// will only return false if the slot is not present.  so check .IsAlive
	/// </summary>
	private bool __TryQueryMetadata(AllocSlot slot, out AllocMetadata metadata)
	{
		//var columns = _GetColumnsSpan();
		//var atomId = Atom.GetId<AllocMetadata>();
		//var columnList = columns[atomId];//  _componentColumns[typeof(AllocMetadata)];
		var column = GetColumn<AllocMetadata>();
		if (column == null || column.Count < slot.chunkIndex)
		{
			metadata = default;
			return false;
		}
		var chunk = column[slot.chunkIndex] as Chunk<AllocMetadata>;
		metadata = chunk.Span[slot.rowSlotIndex];
		return true;
	}


	protected internal static ref T _UNCHECKED_GetComponent<T>(ref AllocToken allocToken)
	{
		var atomId = Atom.GetId<T>();
		var chunk = _GLOBAL_LOOKUP.Span[allocToken.allocatorId]._GetColumnsSpan()[atomId]._AsSpan_Unsafe()[allocToken.allocSlot.chunkIndex] as Chunk<T>;
		return ref chunk.Span[allocToken.allocSlot.rowSlotIndex];
	}
	/// <summary>
	/// INTERNAL USE ONLY.  doesn't do checks to ensure token is valid.
	/// returns false if the slot is not allocated.  if it's allocated but free, it still returns whatever data is contained.
	/// </summary>
	protected internal ref T _UNCHECKED_GetComponent<T>(ref AllocSlot slot, out bool exists)
	{
		//var atomId = Atom.GetId<T>();
		var column = GetColumn<T>();
		if (column == null)
		{
			exists = false;
			return ref Unsafe.NullRef<T>();
		}
		var columnSpan = column._AsSpan_Unsafe();
		if (columnSpan.Length <= slot.chunkIndex || columnSpan[slot.chunkIndex] == null)
		{

			exists = false;
			return ref Unsafe.NullRef<T>();
		}
		var chunk = columnSpan[slot.chunkIndex] as Chunk<T>;
		if (chunk == null)
		{
			exists = false;
			return ref Unsafe.NullRef<T>();
		}
		exists = true;
		return ref chunk.Span[slot.rowSlotIndex];
	}
	/// <summary>
	/// INTERNAL USE ONLY.  doesn't do checks to ensure token is valid.
	/// </summary>
	protected internal ref T _UNCHECKED_GetComponent<T>(ref AllocSlot slot)
	{
		//var atomId = Atom.GetId<T>();
		return ref (GetColumn<T>()._AsSpan_Unsafe()[slot.chunkIndex] as Chunk<T>).Span[slot.rowSlotIndex];
	}


	public bool HasComponentType<T>()
	{
		//return _componentColumns.ContainsKey(typeof(T));
		var atomId = Atom.GetId<T>();
		var columns = _GetColumnsSpan();
		return columns.Length > atomId && columns[atomId] != null;
	}

	public void Initialize()
	{
		//add self to global lookup
		__ERROR.Throw(_allocatorId == -1, "why already set?");
		_allocatorId = _GLOBAL_LOOKUP.AllocSlot();

		//__DEBUG.AssertOnce(_allocatorId < 10, "//TODO: change allocatorId to use a pool, not increment, otherwise risk of collisions with long-running programs");
		__DEBUG.Throw(ComponentTypes != null, "need to set properties before init");
		//generate hash for fast matching of archetypes
		foreach (var type in ComponentTypes)
		{
			_componentsHashId += type.GetHashCode();
		}


		void _AllocColumnsHelper(Type type)
		{
			var atomId = Atom.GetId(type);
			__DEBUG.Throw(_atomIdsUsed.Contains(atomId) == false);
			_atomIdsUsed.Add(atomId);
			while (_columnStorage.Count() <= atomId)
			{
				_columnStorage.Add(null);
			}
			__DEBUG.Throw(_columnStorage[atomId] == null);
			_columnStorage[atomId] = new();
		}


		//create columns
		foreach (var type in ComponentTypes)
		{
			//_componentColumns.Add(type, new());
			_AllocColumnsHelper(type);
		}
		//add our special metadata component column
		__DEBUG.Throw(CustomMetaComponentTypes.Contains(typeof(AllocMetadata)), "we must have allocMetadata to store info on each entity added");
		foreach (var type in CustomMetaComponentTypes)
		{
			//_componentColumns.Add(type, new());	
			_AllocColumnsHelper(type);
		}



		//create our next slot alloc tracker
		_nextSlotTracker = new()
		{
			chunkSize = ChunkSize,
			nextAvailable = new(0, 0),
			allocator = this,
		};

		//create the first (blank) chunk for each column
		_AllocNextChunk();





		//lock (_GLOBAL_LOOKUP)
		//{
		//	_GLOBAL_LOOKUP.Add(_allocatorId, this);
		//}
		_GLOBAL_LOOKUP.Span[_allocatorId] = this;
	}
	public bool IsDisposed { get; private set; } = false;
#if CHECKED
	private DisposeSentinel _disposeCheck = new();
#endif
	public void Dispose()
	{
		if (IsDisposed)
		{
			__DEBUG.Assert(false, "why dispose twice?");
			return;
		}
		IsDisposed = true;
#if CHECKED
		_disposeCheck.Dispose();
#endif

		//lock (_GLOBAL_LOOKUP)
		//{
		//	_GLOBAL_LOOKUP.Remove(_allocatorId);
		//}

		var columns = _GetColumnsSpan();
		//foreach (var (type, columnList) in _componentColumns)
		//foreach (var columnList in _columns)
		foreach (var atomId in _atomIdsUsed)
		{
			var columnList = columns[atomId];
			foreach (var chunk in columnList)
			{
				chunk.Dispose();
			}
			columnList.Clear();
			columns[atomId] = null;
		}
#if CHECKED
		foreach (var columnList in columns)
		{
			__CHECKED.Throw(columnList == null);
		}
#endif

		columns.Clear();
		_columnStorage = null;
		_free.Clear();
		_free = null;
		_lookup.Clear();
		_lookup = null;
		_GLOBAL_LOOKUP.FreeSlot(_allocatorId);
		__ERROR.Throw(_GLOBAL_LOOKUP.Span.Length <= _allocatorId || _GLOBAL_LOOKUP.Span[_allocatorId] == null);
		_allocatorId = -1;
	}
#if DEBUG
	~Allocator()
	{
		if (IsDisposed == false)
		{
			__DEBUG.AssertOnce(false, "need to have parent archetype dispose allocator for proper cleanup");
			Dispose();
		}
	}
#endif
}

public partial class Allocator //chunk management logic
{

	private void _AllocNextChunk() //TODO: preallocate extra chunks ahead of their need (always keep 1x extra chunk around)
	{
		void _AllocChunkHelper(Type type)
		{
			var columns = _GetColumnsSpan();
			var atomId = Atom.GetId(type);
			var chunkType = typeof(Chunk<>).MakeGenericType(type);
			var chunk = Activator.CreateInstance(chunkType) as Chunk;
			//chunk.Initialize(_nextSlotTracker.chunkSize, _nextSlotTracker.nextAvailable.GetChunkLookupId(_allocatorId));
			chunk.Initialize(_nextSlotTracker.chunkSize, this, _nextSlotTracker.nextAvailable.chunkIndex);

			__DEBUG.Assert(columns[atomId].Count == _nextSlotTracker.nextAvailable.chunkIndex, "somehow our column allocations is out of step with our next free tracking.");
			columns[atomId].Add(chunk);
		}

		foreach (var type in ComponentTypes)
		{
			_AllocChunkHelper(type);
		}


		foreach (var type in CustomMetaComponentTypes)
		{
			_AllocChunkHelper(type);
		}
	}

	private void _FreeLastChunk()
	{
		void _FreeChunkHelper(Type type)
		{
			var columns = _GetColumnsSpan();
			var atomId = Atom.GetId(type);
			var result = columns[atomId]._TryTakeLast(out var chunk);
			__DEBUG.Throw(result && chunk._count == 0);
			chunk.Dispose();
			__DEBUG.Assert((columns[atomId].Count - 1) == _nextSlotTracker.nextAvailable.chunkIndex, "somehow our column allocations is out of step with our next free tracking.");
		}
		foreach (var type in ComponentTypes)
		{
			_FreeChunkHelper(type);

		}
		foreach (var type in CustomMetaComponentTypes)
		{
			_FreeChunkHelper(type);
		}
	}
}

public partial class Allocator  //alloc/free/pack logic
{
	/// <summary>
	/// when we pack, we move entities around.  This is used to determine if a AllocToken is out of date.
	/// </summary>
	public int _packVersion = 0;
	/// <summary>
	/// given an externalId, find current token.  this allows decoupling our internal storage location from external callers, allowing packing.
	/// </summary>
	public Dictionary<long, AllocToken> _lookup = new();


	/// <summary>
	/// temp listing of free entities, we will pack and/or deallocate at a specific time each frame
	/// </summary>
	public List<AllocSlot> _free = new();
	public bool _isFreeSorted = true;


	public int ChunkSize { get; init; } = 1000;
	public int Count { get => _lookup.Count; }
	/// <summary>
	/// the next slot we will allocate from, and logic informing us when to add/remove chunks
	/// </summary>
	public AllocPositionTracker _nextSlotTracker;



	/// <summary>
	/// default true, automatically pack when Free() is called. 
	/// </summary>
	public bool AutoPack { get; init; } = true;


	/// <summary>
	/// get a slot (recycling free if available)
	/// make allocToken
	/// add slot to columnList
	/// set builtin allocMetadata component
	/// verify
	/// </summary>
	public void Alloc(Span<long> externalIds, Span<AllocToken> output)
	{
		if (_isFreeSorted != true)
		{
			_free.Sort();
			_isFreeSorted = true;
		}

		__DEBUG.Assert(output.Length == externalIds.Length);


		var columns = _GetColumnsSpan();

		for (var i = 0; i < externalIds.Length; i++)
		{
			var externalId = externalIds[i];
			//get next free.
			if (!_free._TryTakeLast(out var slot))
			{
				//  if we need to allocate a chunk do so here also.  //TODO: multithread
				slot = _nextSlotTracker.AllocNext(out var newChunk);
				if (newChunk)
				{
					_AllocNextChunk();
				}
			}
			ref var allocToken = ref output[i];
			allocToken = _GenerateLiveAllocToken(externalId, slot);
			//new()  
			//{
			//	isInit = true,
			//	allocatorId = _allocatorId,
			//	allocSlot = slot,
			//	externalId = externalId,
			//	packVersion = _packVersion,
			//};




			//loop all components zeroing out data and informing chunk of added item
			foreach (var atomId in _atomIdsUsed)
			{
				columns[atomId][slot.chunkIndex].OnAllocSlot(ref allocToken);
			}




			//set the allocMetadata builtin componenent
			ref var allocMetadata = ref allocToken.GetContainingChunk<AllocMetadata>().Span[allocToken.allocSlot.rowSlotIndex];
			__CHECKED.Throw(allocMetadata == default(AllocMetadata), "expect this to be cleared out, why not?");
			allocMetadata = new AllocMetadata()
			{
				allocToken = allocToken,
				componentCount = _atomIdsUsed.Count,
			};


			//add to lookup
			_lookup.Add(externalId, allocToken);


			__CHECKED.Throw(allocMetadata == allocToken.GetComponentReadRef<AllocMetadata>(), "component reference verification failed.  why?");

#if CHECKED
			var (result, reason) = CheckIsValid(ref allocToken);
			__ERROR.Throw(result, reason);
#endif
			__CHECKED_INTERNAL_VerifyAllocToken(ref allocToken);
		}


	}
	private AllocToken _GenerateLiveAllocToken(long externalId, AllocSlot slot)
	{
		return new AllocToken
		{
			isAlive = true,
			allocatorId = _allocatorId,
			allocSlot = slot,
			externalId = externalId,
			packVersion = _packVersion,
			allocatorVersion = _version,
		};
	}

	public (bool result, string reason) CheckIsValid(AllocToken allocToken)
	{
		unsafe
		{
			return CheckIsValid(ref *&allocToken);
		}
	}
	public (bool result, string reason) CheckIsValid(ref AllocToken allocToken)
	{
		var result = true;
		string reason = null;
		if (allocToken.isAlive == false)
		{
			reason = "token is not alive";
			result = false;
		}else if(!_lookup.TryGetValue(allocToken.externalId, out var lookupToken))
		{
			reason = "token does not have a matching entityId.  was it removed?";
			result = false;
		}
		else if (lookupToken.packVersion != allocToken.packVersion)
		{
			reason = "wrong packVersion.  aquire a new AllocToken every update from Archetype.GetAllocToken() with AutoPack=true. But for best performance over many entities, use Archetype.Query()";
			result = false;
		}

		if (result == true)
		{
			__CHECKED_INTERNAL_VerifyAllocToken(ref allocToken);
		}

		return (result, reason);
	}


	/// <summary>
	/// verify engine state internally.  Use `CheckIsValid()` to verify user input
	/// </summary>
	/// <param name="allocToken"></param>
	[Conditional("CHECKED")]
	public void __CHECKED_INTERNAL_VerifyAllocToken(ref AllocToken allocToken)
	{

		//__ERROR.Throw(_packVersion == allocToken.packVersion, "allocToken out of date.  a pack occured.  you need to reaquire the token every frame if AutoPack==true");
		var storedToken = _lookup[allocToken.externalId];
		__ERROR.Throw(storedToken == allocToken);

		var columns = _GetColumnsSpan();

		//make sure proper chunk is referenced, and field
		//foreach (var (type, columnList) in _componentColumns)
		foreach (var atomId in _atomIdsUsed)
		{
			var columnChunk = columns[atomId][allocToken.allocSlot.chunkIndex];

			//__CHECKED.Throw(columnChunk._chunkLookupId == allocToken.GetChunkLookupId(), "lookup id mismatch");
			__CHECKED.Throw(columnChunk.allocatorId == allocToken.allocatorId && columnChunk.allocatorVersion == allocToken.allocatorVersion, "lookup id mismatch");

		}


		//verify chunk accessor workflows are correct
		//var manualGetChunk = _componentColumns[typeof(AllocMetadata)][allocToken.allocSlot.columnChunkIndex] as Chunk<AllocMetadata>;
		//var manualGetChunk = GetChunk<AllocMetadata>(ref allocToken);
		var manualGetChunk = GetColumn<AllocMetadata>()._AsSpan_Unsafe()[allocToken.allocSlot.chunkIndex] as Chunk<AllocMetadata>;

		var autoGetChunk = allocToken.GetContainingChunk<AllocMetadata>();
		__CHECKED.Throw(manualGetChunk == autoGetChunk, "should match");

		//verify allocMetadatas match
		__ERROR.Throw(manualGetChunk.Span[allocToken.allocSlot.rowSlotIndex].allocToken == allocToken);



		//verify access thru Chunk<T> works also
		var chunkLookupChunk = Chunk<AllocMetadata>._LOOKUP._storage[allocToken.allocatorId]._AsSpan_Unsafe()[allocToken.allocSlot.chunkIndex];
		__ERROR.Throw(chunkLookupChunk == manualGetChunk);

	}




	/// <summary>
	/// get the allocTokens to delete
	/// verify
	/// delete from allocations lookup
	/// free slot from columnList
	/// add to free list
	/// if AutoPack, do it now.
	/// </summary>
	public unsafe void Free(Span<long> externalIds)
	{
		if(externalIds.Length == 0)
		{
			return;
		}
		using var so_AllocTokens = SpanPool<AllocToken>.Allocate(externalIds.Length);
		var allocTokens = so_AllocTokens.Span;
		//get tokens for freeing
		for (var i = 0; i < externalIds.Length; i++)
		{
			allocTokens[i] = _lookup[externalIds[i]];
			__CHECKED.Throw(allocTokens[i].externalId == externalIds[i]);
			var (result, reason) = CheckIsValid(ref allocTokens[i]);
			__ERROR.Throw(result, reason);

			__CHECKED_INTERNAL_VerifyAllocToken(ref allocTokens[i]);
			//remove them now??  maybe will cause further issues with verification
			_lookup.Remove(externalIds[i]);
		}
		//sort so that when we itterate through, they will have a higher chance of being in the same chunk
		allocTokens.Sort();


		//parallel through all columns, deleting
		var allocTokensArraySegment = so_AllocTokens.DangerousGetArray();
		var allocArray = allocTokensArraySegment.Array;
		Parallel.ForEach(_atomIdsUsed, (atomId, loopState) =>
		{

			var columns = _GetColumnsSpan();
			var columnList = columns[atomId];
			//var (type, columnList) = pair;
			for (var i = 0; i < allocTokensArraySegment.Count; i++)
			{
				ref var allocToken = ref allocArray[i];
				columnList[allocToken.allocSlot.chunkIndex].OnFreeSlot(ref allocToken);
			}
		});




		//add to free list
		for (var i = 0; i < allocTokens.Length; i++)
		{
			__CHECKED.Throw(_free.Contains(allocTokens[i].allocSlot) == false);
			_free.Add(allocTokens[i].allocSlot);

		}
		_isFreeSorted = false;


		if (AutoPack == true)
		{
			var priorPackVersion = _packVersion;
			__DEBUG.Assert(externalIds.Length == _free.Count);
			Pack(externalIds.Length);
			__DEBUG.Assert(priorPackVersion != _packVersion && _free.Count == 0, "autopack not working?");
		}

	}


	private void _PackHelper_MoveSlotToFree(AllocToken highestAlive, AllocSlot lowestFree)
	{
		//verify freeSlot is free, and allocToken is valid
#if CHECKED
		__CHECKED_INTERNAL_VerifyAllocToken(ref highestAlive);
		__CHECKED.Assert(highestAlive.allocSlot > lowestFree);
		if (!__TryQueryMetadata(lowestFree, out var freeSlotMeta))
		{
			__CHECKED.Throw(false, "this should not happen.  returning false means no chunk exists.");
		}
		__CHECKED.Assert(freeSlotMeta.IsAlive == false && freeSlotMeta.allocToken.isAlive == false, "should be default value");
#endif
		//generate our newPos allocToken
		var newSlotAllocToken = _GenerateLiveAllocToken(highestAlive.externalId, lowestFree);

		//do a single alloc for that freeSlot componentColumns
		//foreach (var (type, columnList) in _componentColumns)

		var columns = _GetColumnsSpan();
		foreach (var columnList in columns)
		{
			if (columnList == null)
			{
				//not our atom
				continue;
			}
			//copy data from old while allocting the slot
			columnList[lowestFree.chunkIndex].OnPackSlot(ref newSlotAllocToken, ref highestAlive);
			//deallocate old slot componentColumns
			columnList[highestAlive.allocSlot.chunkIndex].OnFreeSlot(ref highestAlive);
		}
		//update the metadata component with the new token (the other fields of metadata were coppied along with all other components in the above loop)
		//var metadataChunk = _componentColumns[typeof(AllocMetadata)][newSlotAllocToken.allocSlot.columnChunkIndex] as Chunk<AllocMetadata>;
		//var metadataChunk = GetChunk<AllocMetadata>(ref newSlotAllocToken);
		//ref var metadataComponent = ref metadataChunk.Span[newSlotAllocToken.allocSlot.chunkRowIndex];
		ref var metadataComponent = ref _UNCHECKED_GetComponent<AllocMetadata>(ref newSlotAllocToken);
		metadataComponent.allocToken = newSlotAllocToken;


		//update our _lookup
		_lookup[newSlotAllocToken.externalId] = newSlotAllocToken;

		//make sure our newly moved is all setup properly
		__CHECKED_INTERNAL_VerifyAllocToken(ref newSlotAllocToken);
	}

	public bool Pack(int maxCount)
	{
		//sor frees
		//loop through all to free, lowest to highest
		//if free is higher than highest active slot, done.
		//take highest active allocSlot and swap it with the free


		///<summary>helper function to walk from our highest slot downward (deallocating any free) until it hits a live slot.</summary>
		void _TryFreeAndGetNextAlive(out AllocSlot highestAllocatedSlot, out AllocMetadata highestAliveToken)
		{
			while (true)
			{
				var result = _nextSlotTracker.TryGetHighestAllocatedSlot(out highestAllocatedSlot);
				if (result == false)
				{					
					//  we are done
					__ERROR.Assert(_lookup.Count == 0, "no more slots available.  we expect this to happen if the Allocator is totally empty.");
					break;
				}
				result = __TryQueryMetadata(highestAllocatedSlot, out highestAliveToken);
				if (result == false)
				{
					__ERROR.Assert(false, "why are we not getting a slot?   our nextSlotTracker thinks there should be this allocated.  investigate");
				}
				if (highestAliveToken.IsAlive == true)
				{
					//we have a live slot!
					break;
				}
				else
				{
#if CHECKED
					//verify The highest slot is free.  
					var foundMeta = _UNCHECKED_GetComponent<AllocMetadata>(ref highestAllocatedSlot);
					__ERROR.Throw(foundMeta.IsAlive == false);

#endif

					//decrement our allocations because the top slot is not alive
					_nextSlotTracker.FreeLast(out var shouldFreeChunk);
#if CHECKED
					//verify next free is actually free
					result = __TryQueryMetadata(_nextSlotTracker.nextAvailable, out var shouldBeFreeMetadata);
					__ERROR.Throw(result && shouldBeFreeMetadata.IsAlive == false && shouldBeFreeMetadata.allocToken.isAlive == false);
#endif
					if (shouldFreeChunk)
					{
						_FreeLastChunk();
					}
				}
			}
		}


		if (_free.Count == 0)
		{
			return false;
		}

		_packVersion++;



		var count = Math.Min(maxCount, _free.Count);

		//sort our free list so that the ones closest to rowIndex zero will be swapped out with higher live ones
		if (_isFreeSorted != true)
		{
			_free.Sort();
			_isFreeSorted = true;
		}

		//loop through our free items
		for (var i = 0; i < count; i++)
		{
			var firstFreeSlotToFill = _free[i];

			//find the highestAliveToken, reducing our allocated until that occurs
			AllocSlot highestAllocatedSlot;
			AllocMetadata highestAliveToken;
			_TryFreeAndGetNextAlive(out highestAllocatedSlot, out highestAliveToken);
			if (firstFreeSlotToFill >= highestAllocatedSlot)
			{
				//our first free slot is higher up than our highest allocated.   Because the _free list is sorted, everything else to free is already in unallocated territory.
				//this happens because during the pack, when our `_TryFreeAndGetNextAlive` finds a dead slot on top it will deallocate it.
				//the above while loop already deallocated to before our current free slot.  so we are done				
				break;
			}

			//if we get here, we have a live `highestAliveToken` and a `firstFreeSlotToFill` that is below it.   lets swap
			{
				//swap out free and highest
				__CHECKED.Throw(highestAliveToken.IsAlive == true);
				__CHECKED_INTERNAL_VerifyAllocToken(ref highestAliveToken.allocToken);
				_PackHelper_MoveSlotToFree(highestAliveToken.allocToken, firstFreeSlotToFill);


			}




			//////			if (!_nextSlotTracker.TryGetHighestAllocatedSlot(out var highestAllocatedSlot))
			//////			{
			//////				__ERROR.Assert(false, "investigate?  no slots are filled?  probably okay, just clear our slots?");
			//////				break;
			//////			}
			//////			if (firstFreeSlotToFill > highestAllocatedSlot)
			//////			{
			//////				__ERROR.Assert(false, "investigate?  our free are higher than our filled?  probably okay, just clear our slots?");
			//////				break;
			//////			}


			//////			var highSlotResult = __TryQueryMetadata(highestAllocatedSlot, out var highestAliveToken);
			//////			if (highSlotResult == false || highestAliveToken.IsAlive == false)
			//////			{
			//////				//__ERROR.Throw(false, "if our highestAllocatedSlot is not alive, it should have been sorted ");
			//////				var foundMeta = _UNCHECKED_GetComponent<AllocMetadata>(ref highestAllocatedSlot);
			//////				__ERROR.Throw(foundMeta.IsAlive == false);
			//////				//The highest slot is free.  
			//////			}
			//////			else
			//////			{
			//////				//swap out free and highest
			//////				__CHECKED.Throw(highestAliveToken.IsAlive == true);
			//////				__CHECKED_VerifyAllocToken(ref highestAliveToken.allocToken);
			//////				_PackHelper_MoveSlotToFree(highestAliveToken.allocToken, firstFreeSlotToFill);
			//////			}


			//////			//decrement our slotTracker position now that we have moved our top item (or if top was already free)
			//////			_nextSlotTracker.FreeLast(out var shouldFreeChunk);
			//////#if CHECKED
			//////			//verify next free is actually free
			//////			var result = __TryQueryMetadata(_nextSlotTracker.nextAvailable, out var shouldBeFreeMetadata);
			//////			__ERROR.Throw(result && shouldBeFreeMetadata.IsAlive ==false && shouldBeFreeMetadata.allocToken.isAlive == false);
			//////#endif
			//////			if (shouldFreeChunk)
			//////			{
			//////				_FreeLastChunk();
			//////			}

		}

		{
			//finally, remove any more dead slots on top.  This can occur if the last `_free` items iterated (above) were swaps.
			_TryFreeAndGetNextAlive(out var highestAllocatedSlot, out var highestAliveToken);
		}
#if CHECKED
		//verify that our highest allocated is either free or init
		if (_lookup.Count > 0)
		{
			var result = _nextSlotTracker.TryGetHighestAllocatedSlot(out var highestAllocated);
			__ERROR.Throw(result);
			result = __TryQueryMetadata(highestAllocated, out var highestMetadata);
			__CHECKED.Throw(highestMetadata.IsAlive || _free.Contains(highestAllocated));
		}
#endif
		//remove these free slots now that we are done filling them
		_free.RemoveRange(0, count);

		return true;

	}



}
/// <summary>
/// data for a default chunk always applied to all allocators.
/// </summary>
public record struct AllocMetadata : IComponent
{
	public AllocToken allocToken;
	public int componentCount;
	/// <summary>
	/// hint informing that a writeRef was aquired for one of the components.
	/// <para>Important Note: writing to this fieldWrites is done internally, and does not increment the Chunk[AllocMetadata]._writeVersion.  This is so _writeVersion can be used to detect entity alloc/free </para>
	/// </summary>
	public int fieldWrites;

	/// <summary>
	/// If this slot is in use by an entity
	/// </summary>
	public bool IsAlive { get => allocToken.isAlive; }
}

/// <summary>
/// a helper struct that tracks and computes the next slot available for ALLOCATION.  Note that freed slots are still considered allocated.  
/// when getting a new slot just check the value of `newChunk` to determine if a new chunk needs to be allocated.
/// </summary>
public struct AllocPositionTracker
{
	public int chunkSize;
	public AllocSlot nextAvailable;
	public Allocator allocator;
	/// <summary>
	/// allocate a new slot for the Allocator object.   this slot is identical for all components in this allocator.  (together, it makes up a "row")
	/// </summary>
	/// <param name="newChunk"></param>
	/// <returns></returns>
	public AllocSlot AllocNext(out bool newChunk)
	{
		var toReturn = nextAvailable;
		nextAvailable.rowSlotIndex++;
		if (nextAvailable.rowSlotIndex >= chunkSize)
		{
			nextAvailable.rowSlotIndex = 0;
			nextAvailable.chunkIndex++;
			newChunk = true;
		}
		else
		{
			newChunk = false;
		}
		__CHECKED.Throw(nextAvailable != toReturn, "make sure these are still structs?");
		return toReturn;
	}

	/// <summary>
	/// Free the last slot  (highest chunk+row index)
	/// </summary>
	/// <param name="freeChunk"></param>
	public void FreeLast(out bool freeChunk)
	{
		//if(TryGetPriorSlot(nextAvailable,out var prior))
		//{
		//	if (nextAvailable.columnChunkIndex != prior.columnChunkIndex)
		//	{
		//		freeChunk = true;
		//	}
		//	else
		//	{
		//		freeChunk = false;
		//	}
		//	nextAvailable = prior;
		//}

#if CHECKED
		//ensure that slot we are going to free is actually free
		var result = TryGetHighestAllocatedSlot(out var slotToCheck);
		__ERROR.Throw(result, "shouldnt free if nothing left allocated");
		ref var checkMeta = ref allocator._UNCHECKED_GetComponent<AllocMetadata>(ref slotToCheck);
		__ERROR.Throw(checkMeta.IsAlive == false, "should be dead if we are about to free it");
#endif


		nextAvailable.rowSlotIndex--;
		if (nextAvailable.rowSlotIndex < 0)
		{
			nextAvailable.rowSlotIndex = chunkSize - 1;
			nextAvailable.chunkIndex--;
			freeChunk = true;
			__DEBUG.Throw(nextAvailable.chunkIndex >= 0, "less than zero allocations");
		}
		else
		{
			freeChunk = false;
		}
	}
	private bool _TryGetPriorSlot(AllocSlot current, out AllocSlot prior)
	{
		if (nextAvailable.chunkIndex == 0 && nextAvailable.rowSlotIndex == 0)
		{
			prior = default;
			return false;
		}
		prior = current;
		prior.rowSlotIndex--;
		if (prior.rowSlotIndex < 0)
		{
			prior.rowSlotIndex = chunkSize - 1;
			prior.chunkIndex--;
		}
		return true;
	}
	/// <summary>
	/// For our Allocator, get the highest slot currently allocated.
	/// Note: while the slot may be allocated, it may also be freed.  
	/// <para>returns false if no slots are allocated</para>
	/// </summary>
	/// <param name="slot"></param>
	/// <returns></returns>
	internal bool TryGetHighestAllocatedSlot(out AllocSlot slot)
	{
		return _TryGetPriorSlot(nextAvailable, out slot);
	}
}



public abstract class Chunk : IDisposable
{
	//public long _chunkLookupId = -1;
	public int allocatorId = -1;
	public int allocatorVersion = -1;
	public int columnIndex = -1;
	public int _count;
	public int _length = -1;
	/// <summary>
	/// incremented every time a system writes to any of its slots.  
	/// </summary>
	public int _writeVersion;


	/// <summary>
	/// delete from global chunk store
	/// </summary>
	public abstract void Dispose();

	/// <summary>
	/// using the given _chunkId, allocate self a slot on the global chunk store for Chunk[T]
	/// </summary>
	public abstract void Initialize(int length, Allocator allocator, int columnIndex);
	internal abstract void OnAllocSlot(ref AllocToken allocToken);
	/// <summary>
	/// overload used internally for packing
	/// </summary>
	internal abstract void OnPackSlot(ref AllocToken allocToken, ref AllocToken moveComponentDataFrom);
	internal abstract void OnFreeSlot(ref AllocToken allocToken);
}
[StructLayout(LayoutKind.Explicit)]
public record struct ChunkLookupId
{

	[FieldOffset(0)]
	public long _packedValue;
	[FieldOffset(0)]
	public int columnChunkIndex;
	[FieldOffset(4)]
	public int allocatorId;
}


public class Chunk<TComponent> : Chunk
{
	//public static Dictionary<long, Chunk<TComponent>> _GLOBAL_LOOKUP = new(0);


	public static ResizableArray<List<Chunk<TComponent>>> _LOOKUP = new();


	private MemoryOwner<TComponent> _storage;
	public Memory<TComponent> Memory { get => _storage.Memory; }
	public Span<TComponent> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _storage.Span;
	}

	/// <summary>
	/// this is an array obtained by a object pool (cache).  It is longer than actually needed.  Do not use the extra slots.  always get length from _storage or Span
	/// </summary>
	private TComponent[] _DANGEROUS_refStorage;

#if CHECKED
	private DisposeSentinel _disposeCheck = new();
#endif
	public bool IsDisposed { get; private set; } = false;
	public override void Dispose()
	{
		if (IsDisposed)
		{
			__DEBUG.Throw(false, "why already disposed");
			return;
		}
		IsDisposed = true;

#if CHECKED
		_disposeCheck.Dispose();
#endif
		//lock (_GLOBAL_LOOKUP)
		//{
		//	var result = _GLOBAL_LOOKUP._TryRemove(_chunkLookupId, out _);
		//	__ERROR.Throw(result);
		//}
		__CHECKED.Throw(_LOOKUP._storage[allocatorId][columnIndex] == this, "ref mismatch");
		_LOOKUP._storage[allocatorId][columnIndex] = null;

		allocatorId = -1;
		allocatorVersion = -1;
		columnIndex = -1;




		//our allocator.FreeLastChunk code checks count.   we don't want to check count here because of cases like game shutdown
		//__ERROR.Throw(_count == 0); 
		_DANGEROUS_refStorage = null;
		_storage.Dispose();
		_storage = null;
	}

	public override void Initialize(int length, Allocator allocator, int columnIndex)
	{
		//	_chunkLookupId = chunkLookupId;
		__DEBUG.Throw(this.allocatorId == -1, "already init");
		allocatorId = allocator._allocatorId;
		allocatorVersion = allocator._version;
		this.columnIndex = columnIndex;



		_length = length;

		////__DEBUG.Throw(_chunkLookupId != -1 && _length != -1, "need to set before init");
		//lock (_GLOBAL_LOOKUP)
		//{
		//	var result = _GLOBAL_LOOKUP.TryAdd(_chunkLookupId, this);
		//	__ERROR.Throw(result);
		//}
		var column = _LOOKUP.GetOrSet(allocatorId, () => new());
		__CHECKED.Throw(column.Count <= columnIndex || column[columnIndex] == null);
		column._ExpandAndSet(columnIndex, this);

		_storage = MemoryOwner<TComponent>.Allocate(_length, AllocationMode.Clear); //TODO: maybe no need to clear?
		_DANGEROUS_refStorage = _storage.DangerousGetArray().Array;
	}
	internal override void OnAllocSlot(ref AllocToken allocToken)
	{
		_count++;
#if DEBUG
		//clear the slot
		Span[allocToken.allocSlot.rowSlotIndex] = default(TComponent);
#endif
	}
	/// <summary>
	/// overload used internally for packing
	/// </summary>
	internal override void OnPackSlot(ref AllocToken allocToken, ref AllocToken moveComponentDataFrom)
	{
		_count++;
		Span[allocToken.allocSlot.rowSlotIndex] = moveComponentDataFrom.GetComponentReadRef<TComponent>();
#if DEBUG
		//lock (_GLOBAL_LOOKUP)
		{
			//clear the old slot.  this isn't needed, but in case someone is still using the old ref, lets make them aware of it in DEBUG
			//var chunkLookupId = moveComponentDataFrom.GetChunkLookupId();
			//var result = _GLOBAL_LOOKUP.TryGetValue(chunkLookupId, out var chunk);
			//__ERROR.Throw(result);

			var chunk = _LOOKUP._storage[moveComponentDataFrom.allocatorId]._AsSpan_Unsafe()[moveComponentDataFrom.allocSlot.chunkIndex];

			chunk.Span[moveComponentDataFrom.allocSlot.rowSlotIndex] = default(TComponent);
		}
#endif

	}
	internal override void OnFreeSlot(ref AllocToken allocToken)
	{
		_count--;

		//if (allocToken.GetAllocator().AutoPack)
		//{
		//	//no need to clear, as we will pack over this!
		//	return;
		//}

		//clear the slot
		Span[allocToken.allocSlot.rowSlotIndex] = default(TComponent);
	}
	public unsafe ref TComponent GetWriteRef(AllocToken allocToken)
	{
		return ref GetWriteRef(ref *&allocToken); //cast to ptr using *& to circumvent return ref safety check
	}
	public ref TComponent GetWriteRef(ref AllocToken allocToken)
	{
		//lock (Chunk<AllocMetadata>._GLOBAL_LOOKUP)
		{
			_CHECKED_VerifyIntegrity(ref allocToken);
			var rowIndex = allocToken.allocSlot.rowSlotIndex;
			//inform metadata that a write is occuring.  //TODO: is this needed?  If not, remove it to reduce random memory access
			//var result = Chunk<AllocMetadata>._GLOBAL_LOOKUP.TryGetValue(_chunkLookupId, out var chunk);
			//__ERROR.Throw(result);
			var allocMetadataChunk = Chunk<AllocMetadata>._LOOKUP._storage[allocToken.allocatorId]._AsSpan_Unsafe()[allocToken.allocSlot.chunkIndex];
			ref var allocMetadata = ref allocMetadataChunk.Span[rowIndex];
			allocMetadata.fieldWrites++;

			_writeVersion++;
			return ref Span[rowIndex];
		}
	}
	public unsafe ref readonly TComponent GetReadRef(AllocToken allocToken)
	{
		return ref GetReadRef(ref *&allocToken); //cast to ptr using *& to circumvent return ref safety check
	}
	public ref readonly TComponent GetReadRef(ref AllocToken allocToken)
	{
		_CHECKED_VerifyIntegrity(ref allocToken);
		var rowIndex = allocToken.allocSlot.rowSlotIndex;
		return ref Span[rowIndex];

	}

	[Conditional("CHECKED")]
	private void _CHECKED_VerifyIntegrity(ref AllocToken allocToken)
	{
		//lock (_GLOBAL_LOOKUP)
		{
			allocToken.GetAllocator().__CHECKED_INTERNAL_VerifyAllocToken(ref allocToken);
			//__DEBUG.Throw(allocToken.GetChunkLookupId() == _chunkLookupId, "allocToken does not belong to this chunk");
			__DEBUG.Throw(allocToken.allocatorId == allocatorId && allocToken.allocatorVersion == allocatorVersion && allocToken.allocSlot.chunkIndex == columnIndex, "allocToken does not belong to this chunk");

			//var result = Chunk<TComponent>._GLOBAL_LOOKUP.TryGetValue(_chunkLookupId, out var chunk);
			//__ERROR.Throw(result);
			var chunk = _LOOKUP._storage[allocToken.allocatorId]._AsSpan_Unsafe()[allocToken.allocSlot.chunkIndex];
			__CHECKED.Throw(chunk == this, "alloc system internal integrity failure");
			__CHECKED.Throw(!IsDisposed, "use after dispose");

			var rowIndex = allocToken.allocSlot.rowSlotIndex;
			//result = Chunk<AllocMetadata>._GLOBAL_LOOKUP.TryGetValue(_chunkLookupId, out var allocMetadataChunk);
			//__ERROR.Throw(result);
			var allocMetadataChunk = Chunk<AllocMetadata>._LOOKUP._storage[allocToken.allocatorId]._AsSpan_Unsafe()[allocToken.allocSlot.chunkIndex];
			ref var allocMetadata = ref allocMetadataChunk.Span[rowIndex];
			__DEBUG.Throw(allocMetadata.allocToken == allocToken, "invalid alloc token.   why?");
		}
	}
}


public class AllocSlotList<T> : IDisposable where T : class
{
	private List<T> _storage = new();
	public int _count;

	/// <summary>
	/// get storage as a span.  reads are safe with AllocSlot() as long as slots being added are not access via this Span.
	/// </summary>
	public Span<T> Span
	{
		get => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_storage);
	}

	private List<int> _freeSlots = new();

	public int AllocSlot()
	{
		int slot;
		bool result;
		lock (_freeSlots)
		{
			result = _freeSlots._TryTakeLast(out slot);
		}
		if (!result)
		{
			lock (_storage)
			{
				slot = _storage.Count;
				_storage.Add(default(T));
				_count++;
			}
		}
		__DEBUG.Throw(_storage[slot] == default(T));
		return slot;
	}

	/// <summary>
	/// This will null the slot, no need to do so manually
	/// </summary>
	/// <param name="slot"></param>
	public void FreeSlot(int slot)
	{
		lock (_freeSlots)
		{
			_freeSlots.Add(slot);

			lock (_storage)
			{
				_count--;
				_storage[slot] = default(T);


				__DEBUG.Throw(_storage[slot] == default(T));
				//try to pack if possible
				if (slot == _storage.Count - 1)
				{
					//the slot we are freeing is the last slot in the _storage array.    
					lock (_freeSlots)
					{
						//now have exclusive lock on _freeSlots and _storage

						//sort free so highest at end
						_freeSlots.Sort();

						//while the last free slot is the last slot in storage, remove both
						while (_freeSlots.Count > 0 && _freeSlots[_freeSlots.Count - 1] == _storage.Count - 1)
						{

							var result = _freeSlots._TryTakeLast(out var removedFreeSlot);
							__DEBUG.Throw(result && removedFreeSlot == _storage.Count - 1);
							_storage._RemoveLast();
						}
					}
				}
			}
		}
	}

	public bool IsDisposed { get; private set; }
	public void Dispose()
	{
		if (IsDisposed)
		{
			__DEBUG.Assert(false, "why already disposed?");
			return;
		}
		IsDisposed = true;
		_storage.Clear();
		_storage = null;
		_freeSlots.Clear();
		_freeSlots = null;
		_count = -1;
	}
}