using DumDum.Bcl;
using DumDum.Bcl.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
public record struct AllocToken
{
	public bool isInit;
	public long externalId;


	/// <summary>
	/// the id of the allocator that created/tracks this allocSlot.  //TODO: make the archetype also use this ID as it's own (on create allocator, use it's ID)
	/// If needed, the allocator can be accessed via `Allocator._GLOBAL_LOOKUP(allocatorId)`
	/// </summary>
	public int allocatorId;
	public AllocSlot allocSlot;
	/// <summary>
	/// needs to match Allocator._packVersion, otherwise a pack took place and the token needs to be refreshed.
	/// </summary>
	public int packVersion;
	/// <summary>
	/// can be used to directly find a chunk from `Chunk[TComponent]._GLOBAL_LOOKUP(chunkId)`
	/// </summary>
	public long GetChunkLookupId()
	{
		//create a long from two ints
		//long correct = (long)left << 32 | (long)(uint)right;  //from: https://stackoverflow.com/a/33325313/1115220
		return (long)allocatorId << 32 | (uint)allocSlot.columnChunkIndex;
	}

	public ref T GetComponentWriteRef<T>()
	{
		_CHECKED_VerifyInstance<T>();
		var chunk = GetContainingChunk<T>();
		return ref chunk.GetWriteRef(this);

	}
	public ref readonly T GetComponentReadRef<T>()
	{
		_CHECKED_VerifyInstance<T>();
		var chunk = GetContainingChunk<T>();
		return ref chunk.GetReadRef(this);
	}
	public Chunk<T> GetContainingChunk<T>()
	{
		_CHECKED_VerifyInstance<T>();
		var chunkLookupId = GetChunkLookupId();

		if (!Chunk<T>._GLOBAL_LOOKUP.TryGetValue(chunkLookupId, out var chunk))
		{
			__ERROR.Throw(GetAllocator().HasComponent<T>(), $"the archetype this element is attached to does not have a component of type {typeof(T).FullName}. Be aware that base classes do not match.");
			//need to refresh token
			__ERROR.Throw(false, "the chunk this allocToken points to does not exist.  either entity was deleted or it was packed.  Do not use AllocTokens beyond the frame aquired unless archetype.allocator.AutoPack==false");
		}
		return chunk;
	}
	public Allocator GetAllocator()
	{
		return Allocator._GLOBAL_LOOKUP[allocatorId];
	}
	[Conditional("CHECKED")]
	private void _CHECKED_VerifyInstance<T>()
	{
		if (typeof(T) == typeof(AllocMetadata))
		{
			//otherwise can cause infinite recursion
			return;
		}
		ref readonly var allocMetadata = ref GetComponentReadRef<AllocMetadata>();
		__CHECKED.Throw(allocMetadata.allocToken == this, "mismatch");
		//get chunk via the allocator, where the default way is direct through the Chunk<T>._GLOBAL_LOOKUP
		var chunk = GetAllocator()._componentColumns[typeof(AllocMetadata)][allocSlot.columnChunkIndex] as Chunk<AllocMetadata>;
		__CHECKED.Throw(GetContainingChunk<AllocMetadata>() == chunk,"chunk lookup between both techniques does not match");
		__CHECKED.Throw(this == chunk.Span[allocSlot.chunkRowIndex].allocToken);
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
	/// index to the chunk from `allocator._componentColumns(type)[columnIndex]`
	/// </summary>
	[FieldOffset(0)]
	public int columnChunkIndex;


	[FieldOffset(4)]
	public int chunkRowIndex;

	public AllocSlot(//short allocatorId, 
		int columnIndex, int chunkRowIndex)
	{
		this = default;
		//this.allocatorId = allocatorId;
		this.columnChunkIndex = columnIndex;
		this.chunkRowIndex = chunkRowIndex;
	}

	public long GetChunkLookupId(int allocatorId)
	{
		return (long)allocatorId << 32 | (uint)columnChunkIndex;
	}

	public int CompareTo(AllocSlot other)
	{
		return _packedValue.CompareTo(other._packedValue);
	}
}

public partial class Allocator //unit test
{


	[Conditional("TEST")]
	public static unsafe void __TEST_Unit_ParallelAllocators()
	{
		for(var i = 0; i < 10; i++)
		{
			Task.Run(() => __TEST_Unit_SingleAllocator());
		}
	}

	[Conditional("TEST")]
	public static unsafe void __TEST_Unit_SingleAllocator()
	{
		var allocator = new Allocator()
		{
			AutoPack=__.Rand._NextBoolean(),
			ChunkSize=3,
			ComponentTypes = new() {typeof(int),typeof(string) },
			
			
		};
		allocator.Initialize();

		Span<long> externalIds = stackalloc long[] { 2,4,8,7 ,-2};
		Span<AllocToken> tokens = stackalloc AllocToken[5];
		allocator.Alloc(externalIds, tokens);

	}
}



/// <summary>
/// allocator for archetypes 
/// </summary>
public partial class Allocator : IDisposable //init logic
{

	public static int _allocatorId_GlobalCounter;
	public static Dictionary<int, Allocator> _GLOBAL_LOOKUP = new();
	public int _allocatorId = _allocatorId_GlobalCounter._InterlockedIncrement();

	/// <summary>
	/// if you want to add additional custom components to each entity, list them here.  These are not used to compute the <see cref="_componentsHashId"/>
	/// <para>be sure not to remove the items already in the list.</para>
	/// </summary>
	public List<Type> CustomMetaComponents = new List<Type>() { typeof(AllocMetadata) };


	public List<Type> ComponentTypes { get; init; }
	/// <summary>
	/// used to quickly identify what collection of ComponentTypes this allocator is in charge of
	/// </summary>
	public int _componentsHashId;

	public Dictionary<Type, List<Chunk>> _componentColumns = new();

	public bool HasComponent<T>()
	{
		return _componentColumns.ContainsKey(typeof(T));
	}

	public void Initialize()
	{
		__DEBUG.AssertOnce(_allocatorId < 10, "//TODO: change allocatorId to use a pool, not increment, otherwise risk of collisions with long-running programs");
		__DEBUG.Throw(ComponentTypes != null, "need to set properties before init");
		//generate hash for fast matching of archetypes
		foreach (var type in ComponentTypes)
		{
			_componentsHashId += type.GetHashCode();
		}

		//create columns
		foreach (var type in ComponentTypes)
		{
			_componentColumns.Add(type, new());
		}
		//add our special metadata component column
		__DEBUG.Throw(CustomMetaComponents.Contains(typeof(AllocMetadata)), "we must have allocMetadata to store info on each entity added");
		foreach (var type in CustomMetaComponents)
		{
			_componentColumns.Add(type, new());
		}



		//create our next slot alloc tracker
		_nextSlotTracker = new()
		{
			chunkSize = ChunkSize,
			nextAvailable = new(0, 0),
		};

		//create the first (blank) chunk for each column
		_AllocNextChunk();

		lock (_GLOBAL_LOOKUP)
		{
			_GLOBAL_LOOKUP.Add(_allocatorId, this);
		}
	}
	public bool IsDisposed { get; private set; } = false;
	public void Dispose()
	{
		if (IsDisposed)
		{
			return;
		}
		IsDisposed = true;
		lock (_GLOBAL_LOOKUP)
		{
			_GLOBAL_LOOKUP.Remove(_allocatorId);
		}
		foreach (var (type, columnList) in _componentColumns)
		{
			foreach (var chunk in columnList)
			{
				chunk.Dispose();
			}
			columnList.Clear();
		}
		_componentColumns.Clear();
		_componentColumns = null;
		_free.Clear();
		_free = null;
		_lookup.Clear();
		_lookup = null;
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
		foreach (var (type, columnList) in _componentColumns)
		{
			var chunkType = typeof(Chunk<>).MakeGenericType(type);
			var chunk = Activator.CreateInstance(chunkType) as Chunk;
			chunk.Initialize(_nextSlotTracker.chunkSize, _nextSlotTracker.nextAvailable.GetChunkLookupId(_allocatorId));
			__DEBUG.Assert(columnList.Count == _nextSlotTracker.nextAvailable.columnChunkIndex, "somehow our column allocations is out of step with our next free tracking.");
			columnList.Add(chunk);
		}
	}

	private void _FreeLastChunk()
	{
		foreach (var (type, columnList) in _componentColumns)
		{
			var result = columnList._TryTakeLast(out var chunk);
			__DEBUG.Throw(result && chunk._count == 0);
			chunk.Dispose();
			__DEBUG.Assert(columnList.Count == _nextSlotTracker.nextAvailable.columnChunkIndex, "somehow our column allocations is out of step with our next free tracking.");
		}
	}
}

public partial class Allocator  //alloc/free logic
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



	public void Alloc(Span<long> externalIds, Span<AllocToken> output)
	{
		__DEBUG.Assert(output.Length == externalIds.Length);
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
			allocToken = new()
			{
				isInit= true,
				allocatorId = _allocatorId,
				allocSlot = slot,
				externalId = externalId,
				packVersion = _packVersion,
			};



#if CHECKED
			//verify chunk accessor workflows are correct
			var manualGetChunk = _componentColumns[typeof(AllocMetadata)][allocToken.allocSlot.columnChunkIndex] as Chunk<AllocMetadata>;
			var autoGetChunk = allocToken.GetContainingChunk<AllocMetadata>();
			__CHECKED.Throw(manualGetChunk == autoGetChunk, "should match");


			//make sure proper chunk is referenced, and field
			foreach (var (type, columnList) in _componentColumns)
			{
				var columnChunk = columnList[allocToken.allocSlot.columnChunkIndex];

				__CHECKED.Throw(columnChunk._chunkLookupId == allocToken.GetChunkLookupId(), "lookup id mismatch");

			}
#endif



			//set the allocMetadata builtin componenent
			ref var allocMetadata = ref allocToken.GetContainingChunk<AllocMetadata>().Span[allocToken.allocSlot.chunkRowIndex];
			__CHECKED.Throw(allocMetadata == default(AllocMetadata), "expect this to be cleared out, why not?");
			allocMetadata = new AllocMetadata()
			{
				allocToken = allocToken,
				componentCount = _componentColumns.Count,
			};
			__CHECKED.Throw(allocMetadata == allocToken.GetComponentReadRef<AllocMetadata>(), "component reference verification failed.  why?");



		}


	}

	//lkajsdflkasjdf  //Do FREE next
	public void Free(Span<long> externalIds)
	{

		//be sure to handle AutoPack if set, and update allocMetadata
		//clear the component cells to default upon free if we are not packing to take it's place.
		throw new NotImplementedException();
	}


	public bool Pack(int maxCount)
	{
		_packVersion++;
		throw new NotImplementedException();

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
}

/// <summary>
/// a helper struct that tracks and computes the next slot available.  when getting a new slot just check the value of `newChunk` to determine if a new chunk needs to be allocated.
/// </summary>
public struct AllocPositionTracker
{
	public int chunkSize;
	public AllocSlot nextAvailable;
	public AllocSlot AllocNext(out bool newChunk)
	{
		var toReturn = nextAvailable;
		nextAvailable.chunkRowIndex++;
		if (nextAvailable.chunkRowIndex >= chunkSize)
		{
			nextAvailable.chunkRowIndex = 0;
			nextAvailable.columnChunkIndex++;
			newChunk = true;
		}
		else
		{
			newChunk = false;
		}
		__CHECKED.Throw(nextAvailable != toReturn, "make sure these are still structs?");
		return toReturn;
	}
	public void FreeLast(out bool freeChunk)
	{
		
		nextAvailable.chunkRowIndex--;
		if (nextAvailable.chunkRowIndex < 0)
		{
			nextAvailable.chunkRowIndex = chunkSize - 1;
			nextAvailable.columnChunkIndex--;
			freeChunk = true;
			__DEBUG.Throw(nextAvailable.columnChunkIndex >= 0,"less than zero allocations");
		}
		else
		{
			freeChunk = false;
		}
	}
}



public abstract class Chunk : IDisposable
{
	public long _chunkLookupId = -1;
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
	public abstract void Initialize(int length, long chunkLookupId);
}

public class Chunk<TComponent> : Chunk
{
	public static Dictionary<long, Chunk<TComponent>> _GLOBAL_LOOKUP = new();

	private MemoryOwner<TComponent> _storage;
	public Memory<TComponent> Memory { get => _storage.Memory; }
	public Span<TComponent> Span { get => _storage.Span; }

	private TComponent[] _refStorage;
	public bool IsDisposed { get; private set; } = false;
	public override void Dispose()
	{
		IsDisposed = true;
		lock (_GLOBAL_LOOKUP)
		{
			_GLOBAL_LOOKUP.Remove(_chunkLookupId);
		}
		__ERROR.Throw(_count == 0);
		_refStorage = null;
		_storage.Dispose();
		_storage = null;
	}

	public override void Initialize(int length, long chunkLookupId)
	{
		_chunkLookupId = chunkLookupId;
		_length = length;

		__DEBUG.Throw(_chunkLookupId != -1 && _length != -1, "need to set before init");
		lock (_GLOBAL_LOOKUP)
		{
			_GLOBAL_LOOKUP.Add(_chunkLookupId, this);
		}

		_storage = MemoryOwner<TComponent>.Allocate(_length, AllocationMode.Clear); //TODO: maybe no need to clear?
		_refStorage = _storage.DangerousGetArray().Array;
	}

	public unsafe ref TComponent GetWriteRef(AllocToken allocToken)
	{
		return ref GetWriteRef(ref *&allocToken); //cast to ptr using *& to circumvent return ref safety check
	}
	public ref TComponent GetWriteRef(ref AllocToken allocToken)
	{
		_CHECKED_VerifyIntegrity(ref allocToken);
		var rowIndex = allocToken.allocSlot.chunkRowIndex;
		//inform metadata that a write is occuring.  //TODO: is this needed?  If not, remove it to reduce random memory access
		ref var allocMetadata = ref Chunk<AllocMetadata>._GLOBAL_LOOKUP[_chunkLookupId]._refStorage[rowIndex];
		allocMetadata.fieldWrites++;
		_writeVersion++;
		return ref _refStorage[rowIndex];
	}
	public unsafe ref readonly TComponent GetReadRef(AllocToken allocToken)
	{
		return ref GetReadRef(ref *&allocToken); //cast to ptr using *& to circumvent return ref safety check
	}
	public ref readonly TComponent GetReadRef(ref AllocToken allocToken)
	{
		_CHECKED_VerifyIntegrity(ref allocToken);
		var rowIndex = allocToken.allocSlot.chunkRowIndex;
		return ref _refStorage[rowIndex];

	}

	[Conditional("CHECKED")]
	private void _CHECKED_VerifyIntegrity(ref AllocToken allocToken)
	{
		__DEBUG.Throw(allocToken.GetChunkLookupId() == _chunkLookupId, "allocToken does not belong to this chunk");
		__CHECKED.Throw(Chunk<TComponent>._GLOBAL_LOOKUP[_chunkLookupId] == this, "alloc system internal integrity failure");
		__CHECKED.Throw(!IsDisposed, "use after dispose");
		var rowIndex = allocToken.allocSlot.chunkRowIndex;
		ref var allocMetadata = ref Chunk<AllocMetadata>._GLOBAL_LOOKUP[_chunkLookupId]._refStorage[rowIndex];
		__DEBUG.Throw(allocMetadata.allocToken == allocToken, "invalid alloc token.   why?");

	}
}

