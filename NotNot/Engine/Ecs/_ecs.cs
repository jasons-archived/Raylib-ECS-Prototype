using NotNot.Bcl;
using NotNot.Bcl.Collections._unused;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine.Ecs.Allocation;
using NotNot.Engine.Sim;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace NotNot.Engine.Ecs;



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

public class AccessGuard
{
	//TODO: Read/Write sentinels should just track when reads/writes are permitted.
	//if they occur outside of those times, assert.   This way we don't need to track who does all writes.
	private EntityManager _entityManager;

	public AccessGuard(EntityManager entityManager)
	{
		this._entityManager = entityManager;
	}
	[Conditional("DEBUG")]
	public void ReadNotify<TComponent>() { }
	[Conditional("DEBUG")]
	public void WriteNotify<TComponent>() { }



}
public partial class EntityManager : SystemBase //init / setup
{
	public EntityRegistry _entityRegistry = new();
	public AccessGuard _accessGuard;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_accessGuard = new AccessGuard(this);
	}
	protected override void OnDispose()
	{
		_lookup.Dispose();
		_lookup = null;
		//_archetypes.Clear();
		//_archetypes = null;
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

		public void Dispose()
		{
			_archetypes.Clear();
			_archetypes = null;
			foreach (var (hash, list) in _storage)
			{
				list.Clear();
			}
			_storage.Clear();
			_storage = null;
		}

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
				__ERROR.Throw(other._componentTypes.SetEquals(archetype._componentTypes) == false, "can not add archetype that manages identical components");
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
		public long GetCheckHash(IEnumerable<Type> types)
		{
			long checkHash = 0;
			foreach (var type in types)
			{
				checkHash += type.GetHashCode();
			}
			return checkHash;
		}

		public bool TryGetArchetype(HashSet<Type> componentTypes, out Archetype archetype)
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
				if (potential._componentTypes.SetEquals(componentTypes))
				{
					archetype = potential;
					return true;
				}
			}
			archetype = null;
			return false;
		}

		//[ThreadStatic]
		//private List<Archetype> _queryTemp = new();
		//public MemoryOwner<Archetype> Query<TC1>()
		//{
		//	__DEBUG.Throw(_queryTemp.Count == 0);
		//	foreach (var archetype in _archetypes)
		//	{
		//		if (archetype._componentTypes.Contains(typeof(TC1)))
		//		{
		//			_queryTemp.Add(archetype);
		//		}
		//	}
		//	var toReturn = MemoryOwner<Archetype>.Allocate(_queryTemp.Count);
		//	_queryTemp.CopyTo(toReturn.DangerousGetArray().Array);
		//	_queryTemp.Clear();
		//	return toReturn;

		//}

	}


	public Archetype GetOrCreateArchetype(string name, HashSet<Type> componentTypes)
	{
		if (!_lookup.TryGetArchetype(componentTypes, out var archetype))
		{
			archetype = new Archetype(name, componentTypes);
			archetype.Initialize(this,_entityRegistry);
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
			archetype.Initialize(this,_entityRegistry);
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
	/// <summary>
	/// create a query to efficiently loop through entities and their components
	/// </summary>
	/// <param name="options"></param>
	/// <returns></returns>
	public EntityQuery Query(QueryOptions options)
	{
		var toReturn = new EntityQuery(this, options);
		toReturn.RefreshQuery();
		return toReturn;
	}
}


public delegate void SelectRangeCallback_R<TC1>(ReadMem<EntityMetadata> meta, ReadMem<TC1> c1);
public delegate void SelectRangeCallback_W<TC1>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1);
public delegate void SelectRangeCallback_RR<TC1, TC2>(ReadMem<EntityMetadata> meta, ReadMem<TC1> c1, ReadMem<TC2> c2);
public delegate void SelectRangeCallback_WR<TC1, TC2>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, ReadMem<TC2> c2);
public delegate void SelectRangeCallback_WW<TC1, TC2>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, WriteMem<TC2> c2);
public delegate void SelectRangeCallback_RRR<TC1, TC2, TC3>(ReadMem<EntityMetadata> meta, ReadMem<TC1> c1, ReadMem<TC2> c2, ReadMem<TC3> c3);
public delegate void SelectRangeCallback_WRR<TC1, TC2, TC3>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, ReadMem<TC2> c2, ReadMem<TC3> c3);
public delegate void SelectRangeCallback_WWR<TC1, TC2, TC3>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, WriteMem<TC2> c2, ReadMem<TC3> c3);
public delegate void SelectRangeCallback_WWW<TC1, TC2, TC3>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, WriteMem<TC2> c2, WriteMem<TC3> c3);
public delegate void SelectRangeCallback_RRRR<TC1, TC2, TC3, TC4>(ReadMem<EntityMetadata> meta, ReadMem<TC1> c1, ReadMem<TC2> c2, ReadMem<TC3> c3, ReadMem<TC4> c4);
public delegate void SelectRangeCallback_WRRR<TC1, TC2, TC3, TC4>(ReadMem<EntityMetadata> meta, ReadMem<TC1> c1, ReadMem<TC2> c2, ReadMem<TC3> c3, ReadMem<TC4> c4);
public delegate void SelectRangeCallback_WWRR<TC1, TC2, TC3, TC4>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, ReadMem<TC2> c2, ReadMem<TC3> c3, ReadMem<TC4> c4);
public delegate void SelectRangeCallback_WWWR<TC1, TC2, TC3, TC4>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, ReadMem<TC2> c2, WriteMem<TC3> c3, ReadMem<TC4> c4);
public delegate void SelectRangeCallback_WWWW<TC1, TC2, TC3, TC4>(ReadMem<EntityMetadata> meta, WriteMem<TC1> c1, ReadMem<TC2> c2, WriteMem<TC3> c3, WriteMem<TC4> c4);


/// <summary>
/// filtering criteria to narrow down an EntityQuery
/// </summary>
public class QueryOptions
{
	/// <summary>
	/// as long as one or more of these components exist on an archetype, it (all it's entities) are included in the query.  otherwise the archetype is rejected.
	/// </summary>
	public HashSet<Type> Any { get; set; } = new();
	/// <summary>
	/// all of these components must exist on an archetype, otherwise the archetype (all it's entities) are rejected from the query.
	/// </summary>
	public HashSet<Type> All { get; set; } = new();
	/// <summary>
	/// If any of the specified Component Types are included on an archetype, that archetype (all it's entities) are rejected from this query.
	/// </summary>
	public HashSet<Type> None { get; set; } = new();
	/// <summary>
	/// an optional delegate to let you add/remove archetypes from the query.
	/// </summary>

	public Action<List<Archetype>> custom;
	/// <summary>
	/// default is false.   Set to True to disable the automatic requery when archetypes are added/removed from the world.
	/// Setting to false means that when archetypes are added that match your query, they won't be included until you Manually Refresh.
	/// </summary>
	public bool DisableAutoRefresh { get; set; }

	public QueryOptions() { }
	public QueryOptions(QueryOptions cloneFrom) {
		Any.UnionWith(cloneFrom.Any);
		All.UnionWith(cloneFrom.All);
		None.UnionWith(cloneFrom.None);
		custom = cloneFrom.custom;
		DisableAutoRefresh = cloneFrom.DisableAutoRefresh;
	}


}

public class EntityQuery
{
	internal EntityManager _entityManager;
	internal int _entityManagerVersion = -1;
	/// <summary>
	/// archetypes matching our query
	/// </summary>
	public List<Archetype> archetypes;

	/// <summary>
	/// archetypes have been added/removed from the EntityManager.  This query needs to be refreshed to pick up changes.
	/// </summary>
	public bool IsOutOfDate { get => _entityManagerVersion != _entityManager._version; }


	public QueryOptions _options;

	public EntityQuery(EntityManager entityManager, QueryOptions options)
	{
		_entityManager = entityManager;
		archetypes = new(entityManager._lookup._archetypes);
		_options = options;
	}

	public EntityQuery(EntityQuery cloneFrom)
	{
		_entityManager = cloneFrom._entityManager;
		archetypes = new(cloneFrom.archetypes);
		_options = new(cloneFrom._options);
	}



	public void RefreshQuery()
	{
		archetypes.Clear();
		archetypes.AddRange(_entityManager._lookup._archetypes);
		_entityManagerVersion = _entityManager._version;


		for (var i = archetypes.Count - 1; i >= 0; i--)
		{
			var archetype = archetypes[i];
			var toRemove = false;
			//apply option filters
			if (_options.All != null && _options.All.Count > 0 && archetype._componentTypes.IsSupersetOf(_options.All) == false)
			{
				toRemove = true;
			}
			else if (_options.None != null && _options.None.Count>0 && archetype._componentTypes.Overlaps(_options.None))
			{
				toRemove = true;
			}
			else if (_options.Any != null && _options.Any.Count>0 && archetype._componentTypes.Overlaps(_options.Any) != true)
			{
				toRemove = true;
			}
			if (toRemove)
			{
				archetypes.RemoveAt(i);
			}
		}
		//apply option custom callback
		if (_options.custom != null)
		{
			_options.custom(archetypes);
		}
	}
	private void _TryAutoRefresh()
	{
		if (IsOutOfDate && _options.DisableAutoRefresh != true)
		{
			RefreshQuery();
		}
	}

	public int Count()
	{
		_TryAutoRefresh();

		var toReturn = 0;


		//for all our archetypes, get the columns requested
		foreach (var archetype in archetypes)
		{
			toReturn += archetype.Count;

		}
		return toReturn;
	}

	private void _ReadNotify<TC>()
	{
		_entityManager._accessGuard.ReadNotify<TC>();
	}
	private void _WriteNotify<TC>()
	{
		_entityManager._accessGuard.WriteNotify<TC>();
	}

	#region //////////////////    SelectQuery   ///////////////////////////////////////////////////////////

	/// <summary>
	/// helper to find chunks for the SelectRange methods
	/// </summary>
	private void _SelectRangeHelper<TC1>(Action<Chunk<EntityMetadata>, Chunk<TC1>> helperCallback)
	{
		_TryAutoRefresh();

		//for all our archetypes, get the columns requested
		foreach (var archetype in archetypes)
		{
			if (archetype.IsDisposed)
			{
				continue;
			}
			var page = archetype._page;
			var entityMetaCol = page.GetColumn<EntityMetadata>();

			for (var i = 0; i < entityMetaCol.Count; i++)
			{
				helperCallback(
					entityMetaCol[i] as Chunk<EntityMetadata>
					, page.GetColumn<TC1>()[i] as Chunk<TC1>
					);
			}
		}
	}

	/// <summary>
	/// helper to find chunks for the SelectRange methods
	/// </summary>
	private void _SelectRangeHelper<TC1, TC2>(Action<Chunk<EntityMetadata>, Chunk<TC1>, Chunk<TC2>> helperCallback)
	{
		_TryAutoRefresh();

		//for all our archetypes, get the columns requested
		foreach (var archetype in archetypes)
		{
			if (archetype.IsDisposed)
			{
				continue;
			}
			var page = archetype._page;
			var entityMetaCol = page.GetColumn<EntityMetadata>();

			for (var i = 0; i < entityMetaCol.Count; i++)
			{
				helperCallback(
					entityMetaCol[i] as Chunk<EntityMetadata>
					, page.GetColumn<TC1>()[i] as Chunk<TC1>
					, page.GetColumn<TC2>()[i] as Chunk<TC2>
					);
			}
		}
	}
	/// <summary>
	/// helper to find chunks for the SelectRange methods
	/// </summary>
	private void _SelectRangeHelper<TC1, TC2, TC3>(Action<Chunk<EntityMetadata>, Chunk<TC1>, Chunk<TC2>, Chunk<TC3>> helperCallback)
	{
		_TryAutoRefresh();

		//for all our archetypes, get the columns requested
		foreach (var archetype in archetypes)
		{
			if (archetype.IsDisposed)
			{
				continue;
			}
			var page = archetype._page;
			var entityMetaCol = page.GetColumn<EntityMetadata>();

			for (var i = 0; i < entityMetaCol.Count; i++)
			{
				helperCallback(
					entityMetaCol[i] as Chunk<EntityMetadata>
					, page.GetColumn<TC1>()[i] as Chunk<TC1>
					, page.GetColumn<TC2>()[i] as Chunk<TC2>
					, page.GetColumn<TC3>()[i] as Chunk<TC3>
					);
			}
		}
	}
	/// <summary>
	/// helper to find chunks for the SelectRange methods
	/// </summary>
	private void _SelectRangeHelper<TC1, TC2, TC3, TC4>(Action<Chunk<EntityMetadata>, Chunk<TC1>, Chunk<TC2>, Chunk<TC3>, Chunk<TC4>> helperCallback)
	{
		_TryAutoRefresh();

		//for all our archetypes, get the columns requested
		foreach (var archetype in archetypes)
		{
			if (archetype.IsDisposed)
			{
				continue;
			}
			var page = archetype._page;
			var entityMetaCol = page.GetColumn<EntityMetadata>();

			for (var i = 0; i < entityMetaCol.Count; i++)
			{
				helperCallback(
					entityMetaCol[i] as Chunk<EntityMetadata>
					, page.GetColumn<TC1>()[i] as Chunk<TC1>
					, page.GetColumn<TC2>()[i] as Chunk<TC2>
					, page.GetColumn<TC3>()[i] as Chunk<TC3>
					, page.GetColumn<TC4>()[i] as Chunk<TC4>
					);
			}
		}
	}
	public void SelectRange<TC1>(SelectRangeCallback_R<TC1> callback)
	{
		_ReadNotify<TC1>();

		_SelectRangeHelper<TC1>((meta, c1) => callback(
			ReadMem.Allocate(meta._storage)
			, ReadMem.Allocate(c1._storage)
			));
	}

	public void SelectRange<TC1>(SelectRangeCallback_W<TC1> callback)
	{
		_WriteNotify<TC1>();

		_SelectRangeHelper<TC1>((meta, c1) =>
		{
			callback(
				ReadMem.Allocate(meta._storage)
				, WriteMem.Allocate(c1._storage)
				);
		});

	}


	public void SelectRange<TC1, TC2>(SelectRangeCallback_RR<TC1, TC2> callback)
	{

		_ReadNotify<TC1>();
		_ReadNotify<TC2>();

		_SelectRangeHelper<TC1, TC2>((meta, c1, c2) => callback(
			ReadMem.Allocate(meta._storage)
			, ReadMem.Allocate(c1._storage)
			, ReadMem.Allocate(c2._storage)
			));
	}
	public void SelectRange<TC1, TC2>(SelectRangeCallback_WR<TC1, TC2> callback)
	{
		_WriteNotify<TC1>();
		_ReadNotify<TC2>();

		_SelectRangeHelper<TC1, TC2>((meta, c1, c2) => callback(
			ReadMem.Allocate(meta._storage)
			, WriteMem.Allocate(c1._storage)
			, ReadMem.Allocate(c2._storage)
			));
	}
	public void SelectRange<TC1, TC2>(SelectRangeCallback_WW<TC1, TC2> callback)
	{
		_WriteNotify<TC1>();
		_WriteNotify<TC2>();

		_SelectRangeHelper<TC1, TC2>((meta, c1, c2) => callback(
			ReadMem.Allocate(meta._storage)
			, WriteMem.Allocate(c1._storage)
			, WriteMem.Allocate(c2._storage)
			));
	}

	public void SelectRange<TC1, TC2, TC3>(SelectRangeCallback_RRR<TC1, TC2, TC3> callback)
	{
		_ReadNotify<TC1>();
		_ReadNotify<TC2>();
		_ReadNotify<TC3>();

		_SelectRangeHelper<TC1, TC2, TC3>((meta, c1, c2, c3) => callback(
			ReadMem.Allocate(meta._storage)
			, ReadMem.Allocate(c1._storage)
			, ReadMem.Allocate(c2._storage)
			, ReadMem.Allocate(c3._storage)
			));
	}
	public void SelectRange<TC1, TC2, TC3>(SelectRangeCallback_WRR<TC1, TC2, TC3> callback)
	{
		_WriteNotify<TC1>();
		_ReadNotify<TC2>();
		_ReadNotify<TC3>();

		_SelectRangeHelper<TC1, TC2, TC3>((meta, c1, c2, c3) => callback(
			ReadMem.Allocate(meta._storage)
			, WriteMem.Allocate(c1._storage)
			, ReadMem.Allocate(c2._storage)
			, ReadMem.Allocate(c3._storage)
			));
	}
	public void SelectRange<TC1, TC2, TC3>(SelectRangeCallback_WWR<TC1, TC2, TC3> callback)
	{
		_WriteNotify<TC1>();
		_WriteNotify<TC2>();
		_ReadNotify<TC3>();

		_SelectRangeHelper<TC1, TC2, TC3>((meta, c1, c2, c3) => callback(
			ReadMem.Allocate(meta._storage)
			, WriteMem.Allocate(c1._storage)
			, WriteMem.Allocate(c2._storage)
			, ReadMem.Allocate(c3._storage)
			));
	}
	public void SelectRange<TC1, TC2, TC3>(SelectRangeCallback_WWW<TC1, TC2, TC3> callback)
	{
		_WriteNotify<TC1>();
		_WriteNotify<TC2>();
		_WriteNotify<TC3>();

		_SelectRangeHelper<TC1, TC2, TC3>((meta, c1, c2, c3) => callback(
			ReadMem.Allocate(meta._storage)
			, WriteMem.Allocate(c1._storage)
			, WriteMem.Allocate(c2._storage)
			, WriteMem.Allocate(c3._storage)
			));
	}

	#endregion SelectQuery
}


public partial class Archetype : DisposeGuard //initialization
{
	//internal int _hashId;

	public Page _page;

	public short ArchtypeId { get => _page._pageId; }
	public int Version { get => _page._version; }

	public HashSet<Type> _componentTypes;
	public string Name { get; set; }

	public bool AutoPack { get; init; } = true;
	public int ChunkSize { get; init; } = 1000;

	private EntityRegistry _entityRegistry;
	public EntityManager _entityManager;
	public Archetype(string name, HashSet<Type> componentTypes)
	{
		Name = name;
		_componentTypes = componentTypes;
	}

	//public bool IsInitialized { get=>_initTask!= null && _initTask.IsCompleted; }
	//internal Task _initTask;

	public bool IsInitialized { get; private set; }
	protected internal virtual void Initialize(EntityManager entityManager, EntityRegistry entityRegistry)
	{
		__ERROR.Throw(!IsInitialized, "already initialized");
		IsInitialized = true;

		_entityManager = entityManager;

		if (_page == null)
		{
			_page = new Page(AutoPack, ChunkSize, _componentTypes);
		}
		_entityRegistry = entityRegistry;
		_page.Initialize(this, entityRegistry);
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


	public int Count { get => _page.Count; }


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
		//ComponentQueryCallback_w<int> test1 = (in AccessToken accessToken, ref int c1) => { };
		//Query<int>(null, (in AccessToken accessToken, ref int c1) => {

		//});

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
					var pageToken = entityToken.accessToken;
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


