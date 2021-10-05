using DumDum.Bcl;
using DumDum.Bcl.Collections._unused;
using DumDum.Bcl.Diagnostics;
using DumDum.Engine.Ecs.Allocation;
using DumDum.Engine.Sim;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DumDum.Engine.Ecs;



/// <summary>
/// A "Simulation World".  all archetypes, their entities, their components, and systems are under a single world.
/// </summary>
public class World : SystemBase
{
	public EntityManager entityManager = new();



	protected override Task Update()
	{
		throw new NotImplementedException();
	}
}


public delegate void EntityCreateCallback(Archetype archetype, ReadOnlySpan<EntityHandle> entities);


public abstract class SystemBase : Sim.FixedTimestepNode
{
	protected override Task OnUpdate(Frame frame, NodeFrameState nodeState)
	{
		return Update();

	}

	protected abstract Task Update();



}

public partial class EntityManager : SystemBase //init / setup
{
	public EntityRegistry _entityRegistry = new();

	protected override void OnDispose()
	{
		_lookup = null;
		_archetypes.Clear();
		_archetypes = null;
		base.OnDispose();
	}
}

public partial class EntityManager //archetype management
{



	//TODO: make adding archetypes threadsafe
	//TODO: make racecheck versions of all collections


	//public Dictionary<long, List<Archetype>> _archetypeLookup = new();


	/// <summary>
	/// helper for finding archetypes by components
	/// </summary>
	public ArchetypeFinder _lookup = new();
	/// <summary>
	/// increments when archetypes are added/removed, signaling queries are invalidated.
	/// </summary>
	public int _version { get => _lookup._version; }

	/// <summary>
	/// helper for finding archetypes by components.   use via <see cref="_lookup"/>
	/// </summary>
	public class ArchetypeFinder
	{
		//public List<Archetype> _archetypes = new();
		public HashSet<Archetype> _archetypes = new();

		public Dictionary<long, List<Archetype>> _storage = new();

		public int _version;

		public void Add(Archetype archetype)
		{
			//__CHECKED.Throw(_archetypes.Contains(archetype) == false, "why already added");
			var checkHash = GetCheckHash(archetype);
			if (!_storage.TryGetValue(checkHash, out var list))
			{
				list = new();
				_storage.Add(checkHash, list);				
			}
			__ERROR.Throw(list.Contains(archetype) == false, "already contains");
			__DEBUG.Assert(list.Count == 0, "we have an archetype checkHash collision.  this is expected to be very rare so investigate.  the design supports this though");

			foreach (var other in list)
			{
				__ERROR.Throw(other._componentTypes._IsIdentical(archetype._componentTypes) == false, "can not add archetype that manages identical components");
			}
			__ERROR.Throw(archetype.IsInitialized, "archetype should be initialized before adding to lookup.");

			list.Add(archetype);
			_archetypes.Add(archetype);
			_version++;
		}

		public void Remove(Archetype archetype)
		{
			var hash = GetCheckHash(archetype);
			var list = _storage[hash];
			var result = list.Remove(archetype);
			__ERROR.Throw(result);
			if (list.Count == 0)
			{
				_storage.Remove(hash);
			}
			_version++;
		}

		public long GetCheckHash(Archetype archetype)
		{
			return GetCheckHash(archetype._componentTypes);
		}
		public long GetCheckHash(List<Type> types)
		{
			long checkHash = 0;
			foreach (var type in types)
			{
				checkHash += type.GetHashCode();
			}
			return checkHash;
		}

		public bool TryGetArchetype(List<Type> componentTypes, out Archetype archetype)
		{
			__ERROR.AssertOnce(false, "todo: change List of componentTypes to something better performant");
			var checkHash = GetCheckHash(componentTypes);
			if (!_storage.TryGetValue(checkHash, out var list) || list.Count == 0)
			{
				archetype = null;
				return false;
			}
			if (list.Count == 1)
			{
				var toReturn = list[0];
				__ERROR.Throw(toReturn._componentTypes.Count == componentTypes.Count);
			}
			//if here, there is a checkHash collision, so multiple items in list need to be picked from
			foreach (var potential in list)
			{
				if (potential._componentTypes._IsIdentical(componentTypes))
				{
					archetype = potential;
					return true;
				}
			}
			archetype = null;
			return false;
		}

		[ThreadStatic]
		private List<Archetype> _queryTemp = new();
		public MemoryOwner<Archetype> Query<TC1>()
		{
			__DEBUG.Throw(_queryTemp.Count == 0);
			foreach(var archetype in _archetypes)
			{
				if (archetype._componentTypes.Contains(typeof(TC1)))
				{
					_queryTemp.Add(archetype);
				}
			}
			var toReturn = MemoryOwner<Archetype>.Allocate(_queryTemp.Count);
			_queryTemp.CopyTo(toReturn.DangerousGetArray().Array);
			_queryTemp.Clear();
			return toReturn;
			
		}

	}


	public Archetype GetOrCreateArchetype(string name, List<Type> componentTypes)
	{
		if (!_lookup.TryGetArchetype(componentTypes, out var archetype))
		{
			archetype = new Archetype(name, componentTypes);
			archetype.Initialize(_entityRegistry);
			_lookup.Add(archetype);
		}

		return archetype;
	}

	/// <summary>
	/// allow adding a custom archetype.  be sure to call this before another archetype matching the same componentTypes is added/created.
	/// </summary>
	public bool TryAddArchetype(Archetype archetype)
	{
		if (_lookup.TryGetArchetype(archetype._componentTypes, out var found))
		{
			return false;
		}
		if (archetype.IsInitialized == false)
		{
			archetype.Initialize(_entityRegistry);
		}
		_lookup.Add(archetype);
		return true;
	}
}



public partial class EntityManager //entity creation
{
	private System.Collections.Concurrent.ConcurrentQueue<(int count, Archetype archetype, Action_RoSpan<AccessToken, Archetype> doneCallback)> _createQueue = new();



	public void EnqueueCreateEntity(int count, Archetype archetype, Action_RoSpan<AccessToken, Archetype> doneCallback)
	{
		_createQueue.Enqueue((count, archetype, doneCallback));
	}
	private System.Collections.Concurrent.ConcurrentQueue<(EntityHandle[] toDelete, Action_RoSpan<AccessToken, Archetype> doneCallback)> _deleteQueue = new();
	public void EnqueueDeleteEntity(ReadOnlySpan<EntityHandle> toDelete, Action_RoSpan<AccessToken, Archetype> doneCallback)
	{
		_deleteQueue.Enqueue((toDelete.ToArray(), doneCallback));
	}

	/// <summary>
	/// process all enqueued entity changes.  If a callback enqueues additional changes, those are also done immediately. 
	/// </summary>
	protected void ProcessEnqueued_Phase0()
	{
		var didWork = false;
		while (didWork) //in case a callback enqueues more entity changes, keep doing all of them now
		{
			didWork = false;
			while (_deleteQueue.TryDequeue(out var tuple))
			{
				didWork = true;
				var (toDelete, doneCallback) = tuple;
				DoDeleteEntities_Phase0(toDelete, doneCallback);
			}
			while (_createQueue.TryDequeue(out var tuple))
			{
				didWork = true;
				var (count, archetype, doneCallback) = tuple;
				DoCreateEntities_Phase0(count, archetype, doneCallback);
			}
		}
	}

	private void DoDeleteEntities_Phase0(Span<EntityHandle> toDelete, Action_RoSpan<AccessToken, Archetype> doneCallback)
	{
		if (toDelete.Length == 0)
		{
			return;
		}
		using var accessTokensSO = SpanGuard<AccessToken>.Allocate(toDelete.Length);
		var accessTokens = accessTokensSO.Span;


		_entityRegistry.Get(toDelete, accessTokens);

		//sort so that in order by page
		accessTokens.Sort();
		//group be pageId and send off for deletion
		var pageStartIndex = 0;
		var pageLength = 0;
		var pageId = accessTokens[0].pageId;


		static void DoDeleteHelper(Action_RoSpan<AccessToken, Archetype> doneCallback, Span<AccessToken> accessTokens, int pageStartIndex, int pageLength)
		{
			//that page finished.
			//find archetype and delte for it then call callback
			var pageSpan = accessTokens.Slice(pageStartIndex, pageLength);
			var archetype = pageSpan[0].GetArchetype();
			archetype.DoDeleteEntities_Phase0(pageSpan, doneCallback);
		}

		for (var i = 0; i < accessTokens.Length; i++)
		{
			if (accessTokens[i].pageId == pageId)
			{
				pageLength++;
				continue;
			}
			else
			{
				DoDeleteHelper(doneCallback, accessTokens, pageStartIndex, pageLength);

				//start marking our next page
				pageStartIndex = i;
				pageLength = 1;
			}
		}
		//handle our last page
		DoDeleteHelper(doneCallback, accessTokens, pageStartIndex, pageLength);



	}

	private void DoCreateEntities_Phase0(int count, Archetype archetype, Action_RoSpan<AccessToken, Archetype> doneCallback)
	{
		archetype.DoCreateEntities_Phase0(count, doneCallback);
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
		ProcessEnqueued_Phase0();
		TryRepackEntities_Sync();
		return Task.CompletedTask;
	}
}

public partial class EntityManager //entity query
{
}
public delegate void SelectCallback<TC1>(Span<AccessToken> tokens, Span<TC1> c1);
public struct EntityQuery 
{
	private EntityManager _entityManager;
	private int _entityManagerVersion;
	public MemoryOwner<Archetype> _archetypesCache;

	/// <summary>
	/// archetypes have been added/removed from the EntityManager.  This query needs to be redone.
	/// </summary>
	public bool IsOutOfDate { get => _entityManagerVersion != _entityManager._version; }


	public EntityQuery RefineQuery<TC1>(SelectCallback<TC1> callback)
	{
		return RefineQuery<TC1>();
		
	}

	asdfasdfljalskjf
		  //todo tomorrow:  allow easy EntityQuery workflow for single query, allow refined queries to avoid computorial explosion

	[ThreadStatic]
	private static List<Archetype> _queryTemp = new();
	public EntityQuery RefineQuery<TC1>()
	{
		__ERROR.Throw(IsOutOfDate == false, "archetypes have been added/removed from the EntityManager.  This query needs to be redone.");

		__DEBUG.Throw(_queryTemp.Count == 0);
		foreach (var archetype in _archetypesCache.Span)
		{
			if (archetype._componentTypes.Contains(typeof(TC1)))
			{
				_queryTemp.Add(archetype);
			}
		}
		var toReturn = MemoryOwner<Archetype>.Allocate(_queryTemp.Count);
		_queryTemp.CopyTo(toReturn.DangerousGetArray().Array);
		_queryTemp.Clear();

		return this with { _archetypesCache = toReturn };



	}



	public async Task Select<TC1>(SelectCallback<TC1> callback)
	{
		if(_archetypesCache == null)
		{
			RefineQuery(callback);
		}
		//get all matching archetypes
		_lookup.Query<TC1>()


	}
}

public partial class Archetype : DisposeGuard //initialization
{
	//internal int _hashId;

	public Page _page;

	public short ArchtypeId { get => _page._pageId; }
	public int Version { get => _page._version; }

	public List<Type> _componentTypes;
	public string Name { get; set; }

	public bool AutoPack { get; init; } = true;
	public int ChunkSize { get; init; } = 10000;

	private EntityRegistry _entityRegistry;
	public Archetype(string name, List<Type> componentTypes)
	{
		Name = name;
		_componentTypes = componentTypes;
	}

	//public bool IsInitialized { get=>_initTask!= null && _initTask.IsCompleted; }
	//internal Task _initTask;

	public bool IsInitialized { get; private set; }
	protected internal virtual void Initialize(EntityRegistry entityRegistry)
	{
		__ERROR.Throw(!IsInitialized, "already initialized");
		IsInitialized = true;

		if (_page == null)
		{
			_page = new Page(AutoPack, ChunkSize, _componentTypes);
		}
		_entityRegistry = entityRegistry;
		_page.Initialize(this,entityRegistry);
	}



	protected override void OnDispose()
	{
		_page.Dispose();

		base.OnDispose();
	}

}

public partial class Archetype //passthrough of page stuff
{
	internal void DoCreateEntities_Phase0(int count, Action_RoSpan<AccessToken, Archetype> doneCallback)
	{
		//create entityHandles
		using var entityHandlesSO = SpanGuard<EntityHandle>.Allocate(count);
		var entityHandles = entityHandlesSO.Span;
		using var accessTokensSO = SpanGuard<AccessToken>.Allocate(count);
		var accessTokens = accessTokensSO.Span;
		_entityRegistry.Alloc(entityHandles);

		_page.AllocEntityNew(accessTokens, entityHandles);

		doneCallback(accessTokens, this);
	}
	internal void DoDeleteEntities_Phase0(Span<AccessToken> toDelete, Action_RoSpan<AccessToken, Archetype> doneCallback)
	{
		_page.Free(toDelete);
		doneCallback(toDelete, this);
	}

}

/// <summary>
/// callback used when querying components
/// </summary>
public delegate void ComponentQueryCallback_w<TComponent>(in AccessToken accessTokens, ref TComponent c1);
public delegate void ComponentQueryCallback_r<TComponent>(in AccessToken accessTokens, in TComponent c1);
public delegate void ComponentQueryCallback_writeAll<TC1, TC2>(in AccessToken accessTokens, ref TC1 c1, ref TC2 c2);
public delegate void ComponentQueryCallback<TC1, TC2, TC3>(in AccessToken accessTokens, ref TC1 c1, ref TC2 c2, ref TC3 c3);

public partial class Archetype  //query entities owned by this archetype
{
	/// <summary>
	/// efficient query of entities owned by this archetype.
	/// </summary>
	public void Query<TComponent>(MemoryOwner<AccessToken> accessTokens, ComponentQueryCallback_w<TComponent> callback)
	{
		var span = accessTokens.Span;
		foreach (ref var token in span)
		{
			ref var c1 = ref token.GetComponentWriteRef<TComponent>();
			callback(in token, ref c1);
		}
	}
	public unsafe async Task QueryAsnyc<TComponent>(MemoryOwner<AccessToken> accessTokens, ComponentQueryCallback_w<TComponent> callback)
	{
		var array = accessTokens.DangerousGetArray();

		fixed (AccessToken* _pArray = &array.Array[0])
		{
			var pArray = _pArray;
			ParallelFor.Range(0, array.Count, (start, end) =>
			{

				for (var i = start; i < end; i++)
				{
					ref var token = ref pArray[i];
					ref var c1 = ref token.GetComponentWriteRef<TComponent>();
					callback(in token, ref c1);
				}
			});
		}

	}

	/// <summary>
	/// given a column and token, return a ref to component
	/// </summary>
	private static ref TC __QueryHelper<TC>(Span<Chunk<TC>> column, ref AccessToken token)
	{
		return ref column[token.slotRef.chunkIndex].UnsafeArray[token.slotRef.slotIndex];
	}

	public unsafe void QueryAsync<TC1, TC2>(MemoryOwner<AccessToken> accessTokens, ComponentQueryCallback_writeAll<TC1, TC2> callback)
	{
		//OPTIMIZE LATER: assumign input is sorted, can get the current chunk and itterate through, then move to the next chunk.
		//currently each chunk is re-aquired from the column for each element.   maybe that is okay perfwise tho.

		var array = accessTokens.DangerousGetArray();
		fixed (AccessToken* _pArray = &array.Array[0])
		{
			var pArray = _pArray;
			ParallelFor.Range(0, array.Count, (start, end) =>
			{
				var col1 = Chunk<TC1>._GLOBAL_LOOKUP[ArchtypeId]._AsSpan_Unsafe();
				var col2 = Chunk<TC2>._GLOBAL_LOOKUP[ArchtypeId]._AsSpan_Unsafe();

				for (var i = start; i < end; i++)
				{
					ref var pageToken = ref pArray[i];
					var chunkIndex = pageToken.slotRef.chunkIndex;
					var slotIndex = pageToken.slotRef.slotIndex;
					//ref var c1 = ref token.GetComponentWriteRef<TC1>();
					callback(in pageToken, ref __QueryHelper(col1, ref pageToken), ref __QueryHelper(col2, ref pageToken));
				}
			});
		}
	}
	public unsafe void QueryAsync<TC1, TC2>(ComponentQueryCallback_writeAll<TC1, TC2> callback)
	{
		var colMeta = Chunk<EntityMetadata>._GLOBAL_LOOKUP[ArchtypeId];

		ParallelFor.Range(0, colMeta.Count, (start, end) =>
		{
			var col0 = colMeta._AsSpan_Unsafe();

			var col1 = Chunk<TC1>._GLOBAL_LOOKUP[ArchtypeId]._AsSpan_Unsafe(); ;
			var col2 = Chunk<TC2>._GLOBAL_LOOKUP[ArchtypeId]._AsSpan_Unsafe(); ;
			for (var chunkIndex = start; chunkIndex < end; chunkIndex++)
			{
				var metaChunk = col0[chunkIndex];
				var metaArray = metaChunk.UnsafeArray;

				if (metaChunk._count == 0)
				{
					continue;
				}

				for (var slotIndex = 0; slotIndex < metaChunk._length; slotIndex++)
				{
					var entityToken = metaArray[slotIndex];
					var pageToken = entityToken.pageToken;
					if (entityToken.IsAlive == false)
					{
						continue;
					}
					callback(in pageToken, ref __QueryHelper(col1, ref pageToken), ref __QueryHelper(col2, ref pageToken));
				}
			}
		});

	}



}


