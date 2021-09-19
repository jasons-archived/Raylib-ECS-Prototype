using DumDum.Bcl;
using DumDum.Bcl.Collections._unused;
using DumDum.Bcl.Diagnostics;
using DumDum.Engine.Sim;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DumDum.Engine.Ecs;




public delegate void EntityCreateCallback(Archetype archetype, ReadOnlySpan<EntityHandle> entities);


public abstract class SystemBase : Sim.FixedTimestepNode
{
	protected override Task Update(Frame frame, NodeFrameState nodeState)
	{
		return Update();
		
	}

	protected abstract Task Update();

	

}

public partial class EntityManager : SystemBase //archetype management
{
	//TODO: make adding archetypes threadsafe
	//TODO: make racecheck versions of all collections

	public List<Archetype> _archetypes = new();


	public bool TryGetArchetype(ReadOnlySpan<Type> componentTypes, out Archetype archetype)
	{
		var archHashId = Archetype.ComputeArchetypeHash(componentTypes);
		foreach (var potential in _archetypes)
		{
			if (potential._hashId != archHashId)
			{
				continue;
			}
			var componentCount = 0;
			foreach (var componentType in componentTypes)
			{
				componentCount++;
				if (potential._componentTypes.Contains(componentType) == false)
				{
					continue;
				}
			}
			if (potential._componentTypes.Count != componentCount)
			{
				continue;
			}
			archetype = potential;
			return true;
		}
		archetype = null;
		return false;
	}

	public Archetype GetOrCreateArchetype(string name, ReadOnlySpan<Type> componentTypes)
	{
		if (!TryGetArchetype(componentTypes, out var archetype))
		{
			archetype = new Archetype(name, componentTypes);
			_archetypes.Add(archetype);
		}
		return archetype;
	}

	/// <summary>
	/// allow adding a custom archetype.  be sure to call this before another archetype matching the same componentTypes is added/created.
	/// </summary>
	public bool TryAddArchetype(Archetype archetype)
	{
		if(TryGetArchetype(archetype._componentTypes.ToList()._AsSpan_Unsafe(),out var found))
		{
			return false;
		}
		_archetypes.Add(archetype);
		return true;
	}

}
public partial class EntityManager //entity creation
{
	private System.Collections.Concurrent.ConcurrentQueue<(int count, Archetype archetype, Action_RoSpan<EntityHandle> doneCallback)> _createQueue = new();
	public void EnqueueCreateEntity(int count, Archetype archetype, Action_RoSpan<EntityHandle> doneCallback)
	{
		_createQueue.Enqueue((count, archetype, doneCallback));
	}
	private System.Collections.Concurrent.ConcurrentQueue<(EntityHandle[] toDelete, Action_RoSpan<EntityHandle> doneCallback)> _deleteQueue = new();
	public void EnqueueDeleteEntity(ReadOnlySpan<EntityHandle> toDelete, Action_RoSpan<EntityHandle> doneCallback)
	{
		_deleteQueue.Enqueue((toDelete.ToArray(), doneCallback));
	}

	/// <summary>
	/// process all enqueued entity changes.  If a callback enqueues additional changes, those are also done immediately. 
	/// </summary>
	protected void ProcessEnqueued()
	{
		var didWork = false;
		while (didWork) //in case a callback enqueues more entity changes, keep doing all of them now
		{
			didWork = false;
			while (_createQueue.TryDequeue(out var tuple))
			{
				didWork = true;
				var (count, archetype, doneCallback) = tuple;
				DoCreateEntities_Sync(count, archetype, doneCallback);
			}
			while (_deleteQueue.TryDequeue(out var tuple))
			{
				didWork = true;
				var (toDelete, doneCallback) = tuple;
				DoDeleteEntities_Sync(toDelete, doneCallback);
			}
		}
	}

	private void DoDeleteEntities_Sync(EntityHandle[] toDelete, Action_RoSpan<EntityHandle> doneCallback)
	{
		throw new NotImplementedException();
	}

	private void DoCreateEntities_Sync(int count, Archetype archetype, Action_RoSpan<EntityHandle> doneCallback)
	{
		throw new NotImplementedException();
	}

	private void TryRepackEntities_Sync()
	{
		//TODO: add expected cost of update metrics for current frame and past frames (to SimNode/Frame)
		//check expected cost of update, if equal to lowest point in last 10 frames, or if at least 10 frames has gone by and we are expected lower than past frame, do a repack.

		//repace should 

		throw new NotImplementedException();
	}

	protected override Task Update()
	{
		ProcessEnqueued();
		TryRepackEntities_Sync();
		throw new NotImplementedException();
	}
}




public record struct ArchetypeSlotInfo
{
	public int _archtypeId;
	public int _chunkId;
	public int _slotId;
	//public int _version;
}



/// <summary>
/// archetype tracks what components are associated with a specific kind of entity.
/// </summary>
public partial class Archetype //ctor
{
	private static int _archetypeGlobalCounter;
	public int _archetypeId = _archetypeGlobalCounter++;
	/// <summary>
	/// the components are stored here
	/// </summary>
	public List<DataColumn> _componentColumns = new();
	public HashSet<Type> _componentTypes = new();

	/// <summary>
	/// friendly name.  archetypes are created automatically when an entity using them is created, but can be named for debugging purposes
	/// </summary>
	public string _name;

	//IMPLEMENTATION NOTE: don't need to have a generic form of archetype class.  it should build it's internal structure procedurally based on input Component types.

	/// <summary>
	/// hash of component types belonging to this archetype.  used to quickly narrow down possible archetype matches when searching for one based on componentTypes
	/// </summary>
	public int _hashId;
	public static int ComputeArchetypeHash(ReadOnlySpan<Type> componentTypes)
	{
		int hashCode = 0;
		foreach (var type in componentTypes)
		{
			hashCode += type.GetHashCode();
		}
		return hashCode;
	}


	public Archetype(string name, ReadOnlySpan<Type> componentTypes)
	{
		_name = name;
		_hashId = ComputeArchetypeHash(componentTypes);
		foreach (var type in componentTypes)
		{
			var result = _componentTypes.Add(type);
			__ERROR.Throw(result, "each component type must only be listed once");
			var dataColumnType = (typeof(DataColumn<>)).MakeGenericType(type);
			var dataColumn = Activator.CreateInstance(dataColumnType) as DataColumn;
			dataColumn._parent = this;
			_componentColumns.Add(dataColumn);
		}
	}
}




public partial class Archetype  //entity allocations
{

	/** 
	 * Each DataColumn is split into chunks of 1000 slots each.
	 * we need to allocate a chunkId+slotId for a given input entityHandle.


	*/


	public SlotStore<EntityHandle> _storage = new();

	public void Allocate(ref EntityHandle entityHandle)
	{
		__ERROR.Throw(entityHandle._archetypeSlot == default(ArchetypeSlotInfo), "entityHandle already has arechetype allocated");

		var storageId = _storage.Alloc();
		var chunkId = storageId / DataChunk.CHUNK_SIZE;
		var slotId = storageId % DataChunk.CHUNK_SIZE;

		entityHandle._archetypeSlot = new()
		{
			_archtypeId = _archetypeId,
			_chunkId = chunkId,
			_slotId = slotId
		};
		_storage[storageId] = entityHandle;

		foreach (var dataColumn in _componentColumns)
		{
			dataColumn.Alloc(chunkId, slotId, ref entityHandle, this);
		}
	}

	public void Free(ref EntityHandle entityHandle)
	{
		__ERROR.Throw(entityHandle._archetypeSlot._archtypeId == _archetypeId, "entityHandle not assigned to this archetype");
		var chunkId = entityHandle._archetypeSlot._chunkId;
		var slotId = entityHandle._archetypeSlot._slotId;
		var storageId = (chunkId * DataChunk.CHUNK_SIZE) + slotId;
		foreach (var dataColumn in _componentColumns)
		{
			dataColumn.Free(chunkId, slotId, ref entityHandle, this);
		}
		_storage.Free(storageId);
	}

	




}




public record struct EntityHandle
{

	public int _worldId;
	public int _id;
	public int _version;

	/// <summary>
	/// store archetype so can default to looking up directly here, not the global entityStore
	/// </summary>
	public ArchetypeSlotInfo _archetypeSlot;



	/// <summary>
	/// will dispose this entity in the Housekeeping phase (at start of update loop)
	/// </summary>
	public void EnqueueDispose()
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// get a component for this entity.  Need to be sure the calling system has the proper data permissions (read/write)
	/// </summary>
	/// <typeparam name="TComponent"></typeparam>
	/// <param name="callback"></param>
	/// <returns>if the entity doesn't have a component of this type, returns false</returns>
	public bool TryGetComponent<TComponent>(Action_Ref<TComponent> callback)
	{
		throw new NotImplementedException();
	}
	public bool TryGetComponent<TComponent1, TComponent2>(Action_Ref<TComponent1, TComponent2> callback)
	{
		throw new NotImplementedException();
	}

	public ref TComponent Component<TComponent>()
	{
		throw new NotImplementedException();
	}

}


public class EntityHandleAllocator
{

	public struct InternalData
	{
		public EntityHandle _handle;
		public ArchetypeSlotInfo _slotInfo;
	}

	private static int _ALLOCATOR_VERSION;
	public int _allocatorId = _ALLOCATOR_VERSION++;

	private int _nextNewId;
	private int _version;

	public Dictionary<int, InternalData> _storage = new();
	public List<int> _free = new();


	public EntityHandle Alloc(ArchetypeSlotInfo slotInfo)
	{
		var version = _version++;
		if (!_free._TryTakeLast(out var id))
		{
			id = _nextNewId++;
		}
		var toReturn = new EntityHandle() { _allocatorId = _allocatorId, _id = id, _version = version };
		_storage.Add(id, new() { _handle = toReturn, _slotInfo = slotInfo });
		return toReturn;
	}

	public bool TryFree(EntityHandle handle)
	{
		if (!_storage.TryGetValue(handle._id, out var internalData))
		{
			//__ERROR.Throw(false, "handle not found to Free.  maybe already freed?");
			return false;
		}
		if (internalData._handle != handle)
		{
			//__ERROR.Throw(internalData._handle==handle,"handles do not match.  old handle?")
			return false;
		}
		_storage.Remove(handle._id);
		_free.Add(handle._id);
		return true;
	}
	public bool TryGet(EntityHandle handle, out ArchetypeSlotInfo slotInfo)
	{
		if (_storage.TryGetValue(handle._id, out var internalData))
		{
			if (internalData._handle == handle)
			{
				slotInfo = internalData._slotInfo;
				return true;
			}

		}
		slotInfo = default;
		return false;
	}
	public bool TryChange(EntityHandle handle, in ArchetypeSlotInfo slotInfo)
	{
		if (_storage.TryGetValue(handle._id, out var internalData))
		{
			if (internalData._handle == handle)
			{
				internalData._slotInfo = slotInfo;
				_storage[handle._id] = internalData;
				return true;
			}


		}
		return false;
	}
}


public class DataChunk
{
	public const int CHUNK_SIZE = 1000;
}

/// <summary>
/// the actual data for components is split into fixed-sized "chunks"
/// </summary>
/// <typeparam name="TComponent"></typeparam>
public class DataChunk<TComponent> : DataChunk
{

	public static List<DataChunk<TComponent>> _GLOBAL_DATACHUNK_LOOKUP = new();
	public int _globalLookupId;

	public TComponent[] _storage = new TComponent[CHUNK_SIZE];

	public DataColumn _parent;

	public DataChunk()
	{
		_globalLookupId = _GLOBAL_DATACHUNK_LOOKUP.Count;
		_GLOBAL_DATACHUNK_LOOKUP.Add(this);
	}
}


/// <summary>
/// A column is all the chunks of a specific ComponentType, for a single archetype.
/// </summary>
public abstract class DataColumn
{
	/// <summary>
	/// the Type of the component is used for query purposes
	/// </summary>
	public Type _componentType;
	public Archetype _parent;

	public DataColumn(Type componentType)
	{
		_componentType = componentType;
	}

	internal abstract void Alloc(int chunkId, int slotId, ref EntityHandle entityHandle, Archetype archetype);
	internal abstract void Free(int chunkId, int slotId, ref EntityHandle entityHandle, Archetype archetype);
}


public class DataColumn<TComponent> : DataColumn
{
	public List<DataChunk<TComponent>> _pages = new();

	public DataColumn()
		: base(typeof(TComponent))
	{

	}

	internal override void Alloc(int chunkId, int slotId, ref EntityHandle entityHandle, Archetype archetype)
	{
		throw new NotImplementedException();
	}

	internal override void Free(int chunkId, int slotId, ref EntityHandle entityHandle, Archetype archetype)
	{
		throw new NotImplementedException();
	}
}



public class ArchetypeSlotAllocator
{

	public struct InternalData
	{
		public EntityHandle _handle;
		public ArchetypeSlotInfo _slotInfo;
	}

	private static int _ALLOCATOR_VERSION;
	public int _allocatorId = _ALLOCATOR_VERSION++;

	private int _nextNewId;
	private int _version;

	public Dictionary<int, InternalData> _storage = new();
	public List<int> _free = new();


	public EntityHandle Alloc(ArchetypeSlotInfo slotInfo)
	{
		var version = _version++;
		if (!_free._TryTakeLast(out var id))
		{
			id = _nextNewId++;
		}
		var toReturn = new EntityHandle() { _allocatorId = _allocatorId, _id = id, _version = version };
		_storage.Add(id, new() { _handle = toReturn, _slotInfo = slotInfo });
		return toReturn;
	}

	public bool TryFree(EntityHandle handle)
	{
		if (!_storage.TryGetValue(handle._id, out var internalData))
		{
			//__ERROR.Throw(false, "handle not found to Free.  maybe already freed?");
			return false;
		}
		if (internalData._handle != handle)
		{
			//__ERROR.Throw(internalData._handle==handle,"handles do not match.  old handle?")
			return false;
		}
		_storage.Remove(handle._id);
		_free.Add(handle._id);
		return true;
	}
	public bool TryGet(EntityHandle handle, out ArchetypeSlotInfo slotInfo)
	{
		if (_storage.TryGetValue(handle._id, out var internalData))
		{
			if (internalData._handle == handle)
			{
				slotInfo = internalData._slotInfo;
				return true;
			}

		}
		slotInfo = default;
		return false;
	}
	public bool TryChange(EntityHandle handle, in ArchetypeSlotInfo slotInfo)
	{
		if (_storage.TryGetValue(handle._id, out var internalData))
		{
			if (internalData._handle == handle)
			{
				internalData._slotInfo = slotInfo;
				_storage[handle._id] = internalData;
				return true;
			}

		}
		return false;
	}
}
