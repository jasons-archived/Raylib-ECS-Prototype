using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine.Ecs;

namespace NotNot.Engine.Sim;

/// <summary>
/// Manages execution of <see cref="SimNode"/> in parallel based on order-of-execution requirements (see <see cref="SimNode._updateBefore"/>) and resource requirements (see <see cref="SimNode._readResources"/> and <see cref="SimNode._writeResources"/>)
/// </summary>
public partial class SimManager : DisposeGuard //tree management
{
	public ConcurrentDictionary<string, SimNode> _nodeRegistry = new();
	public RootNode _root;

	public SimManager()
	{
		_root = new RootNode { Name = "root", HierarchyDepth = 0 };
		_root.Register(this);
		__DEBUG.Throw(_nodeRegistry.ContainsKey(_root.Name));
		//var result = _nodeRegistry.TryAdd(_root.Name, _root);
		//__DEBUG.Throw(result);
		
	}

	/// <summary>
	/// if you know the ParentName and that parent is already registered, you can attach to it indirectly here
	/// </summary>
	/// <param name="node"></param>
	public void Register(SimNode node)
	{
		__ERROR.Throw(node.ParentName != null, "you must know the parent name and store it as string in node.ParentName ");
		Register(node, node.ParentName);



		//__ERROR.Throw(node.Name != null, $"your node of type {node.GetType().Name} has a blank name.  It must have a unique .Name property");

		////find parent
		////SimNode parent;
		//if(!_nodeRegistry.TryGetValue(node.ParentNameNew, out var parent))
		//{
		//	//no parent found
		//	__ERROR.Throw(false, $"Registration of node '{node.Name}' failed because it's parent, named '{node.ParentNameNew}' was not already registered");
		//	return;
		//}
		//__CHECKED.Throw(parent._managerNew == this);
		//parent.AddChild(node);




		//lock (_nodeRegistry)
		//{
		//	__ERROR.Throw(_nodeRegistry.TryAdd(node.Name, node), $"A node with the same name of '{node.Name}' is already registered");
		//	var result = _nodeRegistry.TryGetValue(node.ParentName, out parent);
		//	__ERROR.Throw(result, $"Node registration failed.  Node '{node.Name}' parent of '{node.ParentName}' is not registered.");
		//}



		//__ERROR.Throw(node._children.Count == 0, "register/unregister hiearchies is not currently supported due to SimManager._nodeRegistry not getting updated for children");
		//SimNode parent;
		//lock (_nodeRegistry)
		//{
		//	__ERROR.Throw(_nodeRegistry.TryAdd(node.Name, node), $"A node with the same name of '{node.Name}' is already registered");
		//	var result = _nodeRegistry.TryGetValue(node.ParentName, out parent);
		//	__ERROR.Throw(result, $"Node registration failed.  Node '{node.Name}' parent of '{node.ParentName}' is not registered.");
		//}

		//parent.OnChildRegister(node);
	}

	public void Register(SimNode node, string parentName)
	{

		__ERROR.Throw(node.Name != null, $"your node of type {node.GetType().Name} has a blank name.  It must have a unique .Name property");
		__CHECKED.Throw(node.IsRegistered==false && node.IsAdded==false);

		//find parent
		//SimNode parent;
		if (!_nodeRegistry.TryGetValue(parentName, out var parent))
		{
			//no parent found
			__ERROR.Throw(false, $"Registration of node '{node.Name}' failed because it's parent, named '{node.ParentName}' was not already registered");
			return;
		}
		__CHECKED.Throw(parent.manager == this);
		parent.AddChild(node);


		//node.ParentNameNew = parentName;
		//Register(node);

		//__ERROR.Throw(node._children.Count == 0, "register/unregister hiearchies is not currently supported due to SimManager._nodeRegistry not getting updated for children");
		//__ERROR.Throw(node.Name != null, $"your node of type {node.GetType().Name} has a blank name.  It must have a unique .Name property");
		//SimNode parent;
		//lock (_nodeRegistry)
		//{
		//	__ERROR.Throw(_nodeRegistry.TryAdd(node.Name, node), $"A node with the same name of '{node.Name}' is already registered");
		//	var result = _nodeRegistry.TryGetValue(node.ParentName, out parent);
		//	__ERROR.Throw(result, $"Node registration failed.  Node '{node.Name}' parent of '{node.ParentName}' is not registered.");
		//}

		//parent.OnChildRegister(node);
	}


	//public void Unregister(SimNode node)
	//{
	//	node._parentNew.
	//	__ERROR.Throw(node._children.Count == 0, "register/unregister hiearchies is not currently supported due to SimManager._nodeRegistry not getting updated for children");

	//	node._parent.OnChildUnregister(node);

	//	lock (_nodeRegistry)
	//	{
	//		var result = _nodeRegistry.TryGetValue(node.Name, out var foundNode);
	//		_nodeRegistry.Remove(node.Name);
	//		__ERROR.Throw(result);
	//		__ERROR.Throw(node == foundNode, "should ref equal");
	//	}
	//}

	protected override void OnDispose()
	{
		//dispose entire hirearchy
		_root.Dispose();
		_nodeRegistry.Clear();
		_nodeRegistry = null;
		_root = null;
		_resourceLocks.Clear();
		_resourceLocks = null;
		_frame?.Dispose();
		_frame = null;
		_priorFrameTask?.Dispose();
		_priorFrameTask = null;
		base.OnDispose();
	}
}
public partial class SimManager //thread execution
{
	/// <summary>
	/// stores current locks of all resources used by SimNodes.   This central location is needed for coordination of frame executions.
	/// </summary>
	public Dictionary<object, ReadWriteCounter> _resourceLocks = new();

	private TimeStats _stats;
	private Task _priorFrameTask;
	private Frame _frame;

	//private TimeSpan _realElapsedBuffer;
	//private const int _targetFramerate = 240;
	//private TimeSpan _targetFrameElapsed = TimeSpan.FromTicks(TimeSpan.TicksPerSecond/ _targetFramerate);
	public async Task Update(TimeSpan _targetFrameElapsed)
	{
		//_realElapsedBuffer += realElapsed;
		//if (_realElapsedBuffer >= _targetFrameElapsed)
		//{
		//	var isRunningSlowly = false;
		//	_realElapsedBuffer -= _targetFrameElapsed;
		//	if (_realElapsedBuffer > _targetFrameElapsed)
		//	{
		//		//sim is running slowly (it takes more than _targetFrameElapsed time to do an update)
		//		isRunningSlowly = true;
		//		if (_realElapsedBuffer > _targetFrameElapsed*2)
		//		{
		//			//more than 2 frames behind.  start dropping
		//			_realElapsedBuffer *= 0.9f;
		//		}

		//	}
		_stats.Update(_targetFrameElapsed);
		_frame = Frame.FromPool(_stats, _frame, this);


		await _frame.InitializeNodeGraph(_root);

		await _frame.ExecuteNodeGraph();
		// }





	}


}

/// <summary>
/// apply this interface to SimNodes that you do not need to invoke the Update() method for.  
/// This optimizes our execution engine so it does not need to asynchronously wait for an empty update method to execute in the thread pool before invoking dependent nodes.
/// </summary>
public interface IIgnoreUpdate { }

/// <summary>
/// a dummy node, special because it is the root of the simulation hierarchy.  All other <see cref="SimNode"/> get added under it.
/// </summary>
public class RootNode : SimNode, IIgnoreUpdate
{
	protected override Task OnUpdate(Frame frame, NodeFrameState nodeState)
	{
		throw new Exception("This exception will never be thrown because this implements IIgnoreUpdate");
	}

	protected override void OnAdded()
	{
		__ERROR.Throw(false, "root node is special, should not be added/removed, only manually placed at root of SimManager");
		base.OnAdded();
	}
	protected override void OnRemoved()
	{
		__ERROR.Throw(false, "root node is special, should not be added/removed, only manually placed at root of SimManager");
		base.OnRemoved();
	}
}
/// <summary>
/// A node holds logic in it's <see cref="Update(Frame)"/> method that is executed in parallel with other Simnodes.  See <see cref="SimManager"/> for detail.
/// </summary>
/// <remarks>
/// <para>Lifecycle:  Create --> Add --> Register --> Update --> Unregister --> Remove --> Dispose.    
/// Initialize() can occur any time between Create and Register.
/// Dispose() can occur at any time, ahd the node doesn't nessicarly have to be Removed or Unregistered first, however it will crash if used when disposed.  
/// This is acceptable in situations when the World is shutdown.
/// For the Parent/Child hiearchy, parents get initialized/added/registered before children.  children get unregistered/removed/disposed before parents.
/// </para>
/// </remarks>
public abstract partial class SimNode   //tree logic
{
	public string Name { get; init; } = InstanceNameCounter.CreateName<SimNode>();


	private string _parentNameCached;
	/// <summary>
	/// The name of the parent node.
	/// <para>you can set this to a string upon node creation, and then pass to simManager.Register(), which will find the parent and attach this node as it's child.</para>
	/// <para>but it's usually better to just have a reference to the parent, and call parent.AddChild() instead</para>
	/// </summary>
	public string ParentName
	{
		get
		{
			if (parent != null)
			{
				return parent.Name;
			}
			return _parentNameCached;
		}
		set
		{
			__ERROR.Throw(parent == null, "you should only set node.ParentName if there isn't a node.parent already set");
			_parentNameCached = value;
		}
	}

	public SimNode parent;
	public SimManager manager;

	public List<SimNode> children = new();
	/// <summary>
	/// how far down the node hiearchy this is.  RootNode has depth 0.  
	/// <para>when not registered with the SimManager (not attached to a running simulation) the depth is -1</para>
	/// </summary>
	protected internal int HierarchyDepth { get;internal set; } = -1;





	public ReadMem<SimNode> GetHierarchy()
	{
		var toReturn = ReadMem<SimNode>.Allocate(HierarchyDepth + 1, true);

		if (toReturn.length == 0)
		{
			return toReturn;
		}

		var array = toReturn.DangerousGetArray().Array;
		var span = array.AsSpan(0, toReturn.length);

		span[0] = this;
		var curNode = this;
		var index = 1;
		while (curNode.parent != null)
		{
			span[index] = curNode.parent;
			curNode = curNode.parent;
			index++;
		}
		span.Reverse();  //so root is at item 0
		return toReturn;


		////var toReturn = new List<SimNode>();
		////toReturn.Add(this);
		//var curNode = this;
		//while (curNode._parent != null)
		//{
		//	toReturn.Add(curNode._parent);
		//	curNode = curNode._parent;
		//}
		//toReturn.Reverse();
		//return toReturn;
	}

	/// <summary>
	/// returns hierarchy in the string format "NodeName|root->ParentName->NodeName"
	/// </summary>
	/// <returns></returns>
	public string GetHierarchyName()
	{
		var chain = GetHierarchy();

		var query = from node in chain.DangerousGetArray() select node.Name;
		return $"{Name}|{String.Join("->", query)}";
	}

	public override string ToString()
	{
		return $"{GetHierarchyName()}";

		//return $"{Name}  parent={ParentName}";

	}

	/// <summary>
	/// count of all nodes under this node (children+their children)
	/// </summary>
	/// <returns></returns>
	public int HiearchyCount()
	{
		var toReturn = children.Count;
		foreach (var child in children)
		{
			toReturn += child.HiearchyCount();
		}

		return toReturn;
	}

	public bool FindNode(string name, out SimNode node)
	{
		return manager._nodeRegistry.TryGetValue(name, out node);
	}



	public bool IsRegistered { get; private set; }

	internal void Register(SimManager manager)
	{
		__ERROR.Throw(IsRegistered == false);
		IsRegistered = true;
		this.manager = manager;
		var result = this.manager._nodeRegistry.TryAdd(Name, this);
		__DEBUG.Throw(result);
		OnRegister();
		if (IsInitialized == false)
		{
			Initialize();
		}
		foreach (var child in children)
		{
			child.Register(manager);
		}
	}

	/// <summary>
	/// invoked when registered with the engine, meaning it will start getting <see cref="OnUpdate"/> calls
	/// </summary>
	protected virtual void OnRegister()
	{

	}
	internal void Unregister()
	{
		__ERROR.Throw(IsRegistered == true);
		IsRegistered = false;

		foreach (var child in children)
		{
			child.Unregister();
		}

		OnUnregister();
		var result = manager._nodeRegistry.TryRemove(Name,out var self);
		__DEBUG.Throw(result && self == this);
		manager = null;

	

	}
	/// <summary>
	/// invoked upon call to UnRegister, which detaches from the Engine so that it no longer recieves Update() calls. 
	/// first unregisters children, then unregisters this
	/// </summary>
	protected virtual void OnUnregister()
	{

	}



	public bool IsAdded { get; private set; }
	public void AddChild(SimNode child)
	{
		__DEBUG.Throw(child.IsRegistered == false && child.IsAdded == false && children.Contains(child) == false);
		children.Add(child);
		child.Added(this);
		OnChildAdded(child);

		if (IsRegistered)
		{
			child.Register(manager);
		}

	}

	private void Added(SimNode parent)
	{
		__DEBUG.Throw(this.parent == null && HierarchyDepth == -1);
		this.parent = parent;
		HierarchyDepth = parent.HierarchyDepth + 1;
		OnAdded();
	}
	/// <summary>
	/// invoked when added to a parent, regardless of if registered with the engine or not.
	/// </summary>
	protected virtual void OnAdded()
	{

	}

	protected virtual void OnChildAdded(SimNode node)
	{
	}


	public void RemoveChild(SimNode child)
	{
		__DEBUG.Throw(child.IsAdded == true);
		__DEBUG.Throw(children.Contains(child));

		children.Remove(child);
		if (IsRegistered)
		{
			child.Unregister();
		}
		child.Removed();

		OnChildRemoved(child);
	}

	private void Removed()
	{
		parent = null;
		HierarchyDepth = -1;
		OnRemoved();
	}

	protected virtual void OnRemoved() { }



	internal virtual void OnChildRemoved(SimNode node)
	{
	}








	//protected virtual void OnAdded(SimNode parent, SimManager manager)
	//{
	//	_parentNew = parent;
	//	_managerNew = manager;
	//}

	///// <summary>
	///// triggered when removed from the simNode parent
	///// </summary>
	//protected virtual void OnRemoved()
	//{
	//	_parentNew = null;
	//	_managerNew = null;
	//}

	//public void RegisterChild(SimNode child)
	//{
	//	if (child.ParentName != null && child.ParentName != this.Name)
	//	{
	//		__ERROR.Throw(false, $"you are adding the node {child.Name} as a child of ${this.Name} but it's Parent parameter is already set to {child.ParentName}.  either set the ParentName to null or to {this.Name}  ");
	//	}

	//	child._parentName = this.Name;

	//	_manager.Register(child);
	//}
	//public void UnregisterSelf()
	//{
	//	_manager.Unregister(this);
	//}



}



public interface ITargetFramerate
{
	public int TargetFps { get; set; }
}

public abstract class FixedTimestepNode : SimNode
{
	/// <summary>
	/// the target framerate you want to execute at.
	/// Note that nested groups will already be constrained by the parent group TargetFrameRate,
	/// but this property can still be set for the child group. In that case the child group will update at the slowest of the two TargetFrameRates.
	/// <para>default is int.MaxValue (update as fast as possible (every tick))</para>
	/// </summary>
	public int TargetFps;
	//{
	//	get => (int)(TimeSpan.TicksPerSecond / _targetUpdateInterval.Ticks);
	//	set => _targetUpdateInterval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / value);
	//}
	private TimeSpan _targetUpdateInterval { get => TimeSpan.FromTicks(TimeSpan.TicksPerSecond / TargetFps); }

	/// <summary>
	/// If set to true, attempts to ensure we update at precisely the rate specified.  if we execute slightly later than the TargetFrameRate, 
	/// the extra delay is not ignored and is taken into consideration on future updates.  
	/// </summary>
	public bool FixedStep = false;

	/// <summary>
	/// if our update is more than this many frames out of date, we ignore others.
	/// <para> only matters if FixedStep==true</para>
	/// </summary>
	public int CatchUpMaxFrames = 1;


	private TimeSpan _nextRunTime;

	/// <summary>
	/// by default, all FixedTimestepNodes of the same interval update on the same frame. 
	/// If you don't want this node to update at the same time as others, set this to however many frames you want it's update to be offset by;
	/// </summary>
	public int FrameOffset;
	//	get => (int)(_nextRunOffset * TargetFps / _targetUpdateInterval);
	//	set => _nextRunOffset = _targetUpdateInterval / TargetFps * value; 
	//}
	private TimeSpan _nextRunOffset { get => _targetUpdateInterval / TargetFps * FrameOffset; }

	private TimeSpan _intervalOffset = TimeSpan.Zero;

	internal override void OnFrameStarting(Frame frame, ICollection<SimNode> allNodesToUpdateInFrame)
	{
		if (TargetFps == 0)
		{
			//register this node and it's children for participation in this frame.
			base.OnFrameStarting(frame, allNodesToUpdateInFrame);
			//skip this logic
			return;
		}
		//run our node at a fixed interval, based on the target frame rate.  
		if (frame._stats._wallTime >= _nextRunTime)
		{
			var offset = _nextRunOffset;
			_nextRunTime = offset + _nextRunTime._IntervalNext(_targetUpdateInterval);
			if (frame._stats._wallTime >= _nextRunTime)
			{
				//we are running behind our target framerate.

				frame._slowRunningNodes.Add(this);
				var nextCatchupRunTime = offset + frame._stats._wallTime._IntervalNext(_targetUpdateInterval) - (_targetUpdateInterval * CatchUpMaxFrames);
				if (nextCatchupRunTime > _nextRunTime)
				{
					//further behind than our CatchUpMaxFrames so ignore missing updates beyond that.
					_nextRunTime = nextCatchupRunTime;
				}
			}
			//register this node and it's children for participation in this frame.
			base.OnFrameStarting(frame, allNodesToUpdateInFrame);
		}
		else
		{
			//not time to run this node or it's children!
			//do nothing.
		}
	}
}



public abstract partial class SimNode //update logic
{
	/// <summary>
	/// stats about the time at which the update() last completed successfully
	/// </summary>
	public NodeUpdateStats _lastUpdate = new();

	/// <summary> triggered for root node and propigating down.  allows node to decide if it, or it+children should participate in this frame. </summary>
	internal virtual void OnFrameStarting(Frame frame, ICollection<SimNode> allNodesToUpdateInFrame)
	{
		//TODO:  store last updateTime metric, use this+children to estimate node priority (costly things run first!)  add out TimeSpan hiearchyLastElapsed
		//TODO:  when doing that, maybe take the average of executions.   store in nodeState

		if (IsDisabled != _isDisableCached)
		{
			_isDisableCached = IsDisabled;
			if (IsDisabled)
			{
				OnDisabled_Phase0(frame);
			}
			else
			{
				OnEnabled_Phase0(frame);
			}
		}


		if (IsDisabled == false)
		{
			//add this node to be executed this frame
			allNodesToUpdateInFrame.Add(this);  //TODO: node filtering logic here based on FPS limiting, etc
		}
		foreach (var child in children)
		{
			child.IsDisabled = IsDisabled; //current hiearchy state is propagated
			child.OnFrameStarting(frame, allNodesToUpdateInFrame);
		}

	}

	/// <summary>
	/// //IMPORTANT: a node.Update() will fire and complete BEFORE it's children are started. 
	/// </summary>
	/// <param name="frame"></param>
	/// <returns></returns>
	internal Task DoUpdate(Frame frame, NodeFrameState nodeState)
	{
		try
		{
			//if (IsInitialized == false)
			//{
			//	Initialize();
			//}
			return OnUpdate(frame, nodeState);
		}
		finally
		{

		}
	}

	protected abstract Task OnUpdate(Frame frame, NodeFrameState nodeState);



	/// <summary> frame is totally done.  clean up anything created/stored for this frame </summary>
	internal virtual void OnFrameFinished(Frame frame)
	{
		//_frameStates.Remove(frame);
	}

	/// <summary>
	/// helper to detect when enable/disable occurs
	/// </summary>
	private bool _isDisableCached;
	/// <summary>
	/// setting this cascades to all children in the <see cref="OnFrameStarting"/> method.  
	/// If Disabled <see cref="OnUpdate"/> will not run.  
	/// Also when this occurs, <see cref="OnDisabled_Phase0"/> or <see cref="OnEnabled_Phase0"/> will be triggered.
	/// </summary>
	public bool IsDisabled { get; set; }

	/// <summary>
	/// the node and all children are becomming disabled.  OnUpdate will not be called anymore
	/// </summary>
	/// <param name="frame"></param>
	protected virtual void OnDisabled_Phase0(Frame frame)
	{

	}
	/// <summary>
	/// the node and all children are becomming enabled.   OnUpdate will resume being called every frame
	/// </summary>
	/// <param name="frame"></param>
	protected virtual void OnEnabled_Phase0(Frame frame)
	{

	}


}


public abstract partial class SimNode : DisposeGuard, IComparable<SimNode> //frame blocking / update order
{
	public int _executionPriority = 0;
	public int CompareTo(SimNode other)
	{
		if (other == null)
		{
			return 1;
		}
		return _executionPriority - other._executionPriority;
	}

	/// <summary>
	/// name of nodes this needs to update before
	/// </summary>
	public List<string> _updateBefore = new();
	/// <summary>
	/// name of nodes this needs to update after
	/// </summary>
	public List<string> _updateAfter = new();


	/// <summary>
	/// object "token" used as a shared read-access key when trying to execute this node.  
	/// <para>Nodes with read access to the same key may execute in parallel</para>
	/// </summary>
	public List<object> _readResources = new();

	/// <summary>
	/// object "token" used as an exclusive write-access key when trying to execute this node.  
	/// <para>Nodes with write access to a key will run seperate from any other node using the same resource (Read or Write)</para>
	/// </summary>
	public List<object> _writeResources = new();

	/// <summary>
	/// dispose self and all children
	/// </summary>
	protected override void OnDispose()
	{
		foreach (var child in children)
		{
			if (child.IsDisposed == false)
			{
				child.Dispose();
			}
		}
		base.OnDispose();
	}

	public bool IsInitialized { get; private set; }


	internal void Initialize()
	{
		if (IsInitialized == true)
		{
			return;
		}
		IsInitialized = true;
		OnInitialize();
	}
	/// <summary>
	/// if your node has initialization steps, override this method, but be sure to call it's base.OnInitialize();
	/// <para>If Initialize is not called by the time this node is registered with the Engine, initialize will be called automatically.</para>
	/// </summary>
	protected virtual void OnInitialize()
	{

	}
}



/// <summary>
/// A frame of execution.  We need to store state regarding SimNode execution status and that is done here, with the accompanying logic.
/// </summary>
public partial class Frame : DisposeGuard //general setup
{
	public TimeStats _stats;
	public SimManager _manager;

	internal static Frame FromPool(TimeStats stats, Frame priorFrame, SimManager manager)
	{
		//TODO: make chain, recycle old stuff every update
		if (priorFrame != null)
		{
			priorFrame.Dispose();
		}
		return new Frame() { _stats = stats, _manager = manager };

	}
	public override string ToString()
	{
		return _stats.ToString();
	}

	public bool IsRunningSlowly { get => _slowRunningNodes.Count > 0; }
	public List<FixedTimestepNode> _slowRunningNodes = new();


	protected override void OnDispose()
	{
		_manager = null;
		_stats = default(TimeStats);
		_allNodesInFrame.Clear();
		_allNodesInFrame = null;
		_allNodesToProcess.Clear();
		_allNodesToProcess = null;
		_frameStates.Clear();
		_frameStates = null;
		_priorFrame = null;
		_readRequestsRemaining.Clear();
		_readRequestsRemaining = null;
		_writeRequestsRemaining.Clear();
		_writeRequestsRemaining = null;


		base.OnDispose();
	}

}

public partial class Frame ////node graph setup and execution
{
	private List<SimNode> _allNodesInFrame = new();
	private List<SimNode> _allNodesToProcess = new();
	private Dictionary<SimNode, NodeFrameState> _frameStates = new();

	/// <summary>
	/// informs all registered nodes that a frame is going to start.  
	/// This initializes the frame's state.
	/// </summary>
	/// <param name="root"></param>
	/// <returns></returns>
	public async Task InitializeNodeGraph(RootNode root)
	{

		//notify all nodes of our intent to execute a frame, and obtain a flat listing of all nodes
		__DEBUG.Assert(_allNodesInFrame.Count == 0 && _allNodesToProcess.Count == 0 && _frameStates.Count == 0,
			"should be cleared at end of last frame / from pool recycle");
		root.OnFrameStarting(this, _allNodesInFrame);


		//TODO: allow multiple frames to run overlapped once we have read/write component locks implemented.  allow next frame to start/run if conditions met:
		//TODO: example conditions:  NF system can run IF (NF Read + CF No Writes) OR (NF Write + CF No Reads or Writes)


		//TODO: this section  should be moved into Frame class probably, as it is per-frame data.   will need to do this when we allow starting next frame early.

		//sort so at bottom are those that should be executed at highest priority
		_allNodesInFrame.Sort();  //TODO: sort by hierarchy execution time. so longest running nodes get executed first.
#if CHECKED
		//when #CHECKED, randomize order to uncover order-of-execution bugs
		_allNodesInFrame._Randomize();
#endif
		_allNodesToProcess.AddRange(_allNodesInFrame);

		//generate per-frame tracking state for each node
		foreach (var node in _allNodesInFrame)
		{
			var nodeState = new NodeFrameState()
			{
				_node = node
			};
			_frameStates.Add(node, nodeState);
		}
		//reloop building tree of children active in scene (for this frame)
		////This could probably be combined with prior foreach, but leaving sepearate for clarity and not assuming certain implementation
		foreach (var node in _allNodesInFrame)
		{
			if (node.parent == null)
			{
				__DEBUG.Assert(node.GetType() == typeof(RootNode), "only root node should not have parent");
				continue;
			}
			var thisNodeState = _frameStates[node];
			var parentNodeState = _frameStates[node.parent];
			parentNodeState._activeChildren.Add(thisNodeState);
			thisNodeState._parent = parentNodeState;
		}

		//now that our tree of NodeFrameStates is setup properly, reloop calculating execution order dependencies
		foreach (var node in _allNodesInFrame)
		{
			var nodeState = _frameStates[node];


			//TODO: calculate and store all runBefore/ runAfter dependencies

			
			//updateAfter
			foreach (var afterName in node._updateAfter)
			{

				if (!node.FindNode(afterName, out var afterNode))
				{
					__DEBUG.AssertOnce(false, $"'{afterName}' node is listed as an updateAfter dependency in '{node.GetHierarchyName()}' node.  target node not registered");
					continue;
				}
				if (!_frameStates.TryGetValue(afterNode, out var afterNodeState))
				{
					__DEBUG.Assert(false, "missing?  maybe ok.  node not participating in this frame");
					continue;
				}
				__DEBUG.Throw(node.GetHierarchy().Span._Contains(afterNodeState._node) == false, $"updateBefore('{afterName}') is invalid.  Node '{node.Name}' is a (grand)child of '{afterName}'.  You can not mark a parent as updateBefore/After." +
					$"{node.Name} will always update during it's it's parent's update (parent updates first, but is not marked as complete until all it's children finish).");
				nodeState._updateAfter.Add(afterNodeState);
			}

			//updateBefore
			foreach (var beforeName in node._updateBefore)
			{
				if (!node.FindNode(beforeName, out var beforeNode))
				{
					__DEBUG.AssertOnce(false, $"'{beforeName}' node is listed as an updateBefore dependency in '{node.Name}' node.  target node not registered");
					continue;
				}
				if (!_frameStates.TryGetValue(beforeNode, out var beforeNodeState))
				{
					__DEBUG.Assert(false, "missing?  maybe ok.  node not participating in this frame");
					continue;
				}
				__DEBUG.Throw(node.GetHierarchy().Span._Contains(beforeNodeState._node) == false, $"updateBefore('{beforeName}') is invalid.  Node '{node.Name}' is a (grand)child of '{beforeName}'.  You can not mark a parent as updateBefore/After." +
					$"{node.Name} will always update during it's it's parent's update (parent updates first, but is not marked as complete until all it's children finish).");
				beforeNodeState._updateAfter.Add(nodeState);
			}

			var lockTest = new DotNext.Threading.AsyncReaderWriterLock();



			//calc and store resource R/W locking

			//reads
			foreach (var obj in node._readResources)
			{
				var readRequests = _readRequestsRemaining._GetOrAdd(obj, () => new());
				readRequests.Add(node);
			}
			//writes
			foreach (var obj in node._writeResources)
			{
				var writeRequests = _writeRequestsRemaining._GetOrAdd(obj, () => new());
				writeRequests.Add(node);
			}
		}
		//var rwLock = new ReaderWriterLockSlim();
		//rwLock.h

		//System.Threading.res
		//DotNext.Threading.
	}



	/// <summary>
	/// Execute the nodes Update methods.
	/// </summary>
	/// <returns></returns>
	public async Task ExecuteNodeGraph()  //TODO: this should be moved to an execution manager class, shared by all Frames of the same SimManager, otherwise each Frame has it's own threadpool.
	{
		//TODO: when Frames executed in parallel, do at least 1 pass through prior frame nodes before starting next frame.
		//process nodes
		var maxThreads = Environment.ProcessorCount + 2;
		var greedyFillThreads = Environment.ProcessorCount / 2;
		var currentTasks = new List<Task>();
		var activeNodes = new List<NodeFrameState>();
		var waitingOnChildrenNodes = new List<NodeFrameState>();
		var finishedNodes = new List<NodeFrameState>();

		while (_allNodesToProcess.Count > 0 || currentTasks.Count > 0)
		{
			var DEBUG_startedThisPass = 0;
			var DEBUG_finishedNodeUpdate = 0;
			var DEBUG_finishedHierarchy = 0;


			//try to execute highest priority first
			for (var i = _allNodesToProcess.Count - 1; i >= 0; i--)
			{
				var node = _allNodesToProcess[i];
				var nodeState = _frameStates[node];
				if (nodeState.CanUpdateNow() && AreResourcesAvailable(node))
				{
					//execute the node's .Update() method
					__DEBUG.Assert(nodeState._status == FrameStatus.SCHEDULED);
					nodeState._status = FrameStatus.PENDING;
					LockResources(node);

					_allNodesToProcess.RemoveAt(i);

					nodeState._updateStopwatch = Stopwatch.StartNew();

					__DEBUG.Assert(nodeState._status == FrameStatus.PENDING);
					nodeState._status = FrameStatus.RUNNING;
					activeNodes.Add(nodeState);


					//helper to update our nodeState when the update method is done running
					var doneUpdateTask = (Task updateTask) =>
					{
						__DEBUG.Assert(nodeState._status == FrameStatus.RUNNING);
						nodeState._status = FrameStatus.FINISHED_WAITING_FOR_CHILDREN;
						nodeState._updateTime = nodeState._updateStopwatch.Elapsed;
						nodeState._updateTcs.SetFromTask(updateTask);
					};

					if (node is IIgnoreUpdate)
					{
						//node has no update loop, it's done immediately.
						doneUpdateTask(Task.CompletedTask);
						DEBUG_finishedNodeUpdate++;
					}
					else
					{
						//node update() may be async, so need to monitor it to track when it completes.
						var updateTask = Task.Run(() => node.DoUpdate(this, nodeState)).ContinueWith(doneUpdateTask);
						currentTasks.Add(updateTask);
						DEBUG_startedThisPass++;

					}

					if (currentTasks.Count > greedyFillThreads)
					{
						//OPTIMIZATION?: once our threads are half filled, we become more picky about node prioritization.
						//Since our _allNodesToProccess is sorted in priority order,
						//With the following line (starting the for loop over) we will always pick the higest priority node that's available.
						//we don't do this by default in case the simulation has hundreds of nodes with lots of blocking.
						//(constantly starting the for-loop over every Task start seems wasteful)
						i = _allNodesToProcess.Count - 1;
					}

				}


				if (currentTasks.Count >= maxThreads)
				{
					//too many enqueued, stop trying to enqueue more
					break;
				}

			}

			if (currentTasks.Count != 0)
			{
				//wait on at least one task			
				await Task.WhenAny(currentTasks);
			}

			//remove done
			for (var i = currentTasks.Count - 1; i >= 0; i--)
			{
				if (currentTasks[i].IsCompleted)
				{
					//NOTE: task may complete LONG AFTER the actual update() is finished.
					//so we have a counter to help debuggers understand this.
					DEBUG_finishedNodeUpdate++;
					currentTasks.RemoveAt(i);
				}
			}

			//loop through all active nodes, and if FINISHED unlock resources and move to waiting on children collection
			for (var i = activeNodes.Count - 1; i >= 0; i--)
			{
				var nodeState = activeNodes[i];
				__DEBUG.Assert(nodeState._status is FrameStatus.RUNNING or FrameStatus.FINISHED_WAITING_FOR_CHILDREN);

				if (nodeState._status != FrameStatus.FINISHED_WAITING_FOR_CHILDREN)
				{
					continue;
				}
				//node is finished, free resource locks
				UnlockResources(nodeState._node);

				//move to waiting on Children group
				waitingOnChildrenNodes.Add(nodeState);
				activeNodes.RemoveAt(i);
			}

			//loop through all waitingOnChildrenNodes, and if all children are HIERARCHY_FINISHED then mark this as finished and remove from active

			for (var i = waitingOnChildrenNodes.Count - 1; i >= 0; i--)
			{
				var nodeState = waitingOnChildrenNodes[i];
				__DEBUG.Assert(nodeState._status is FrameStatus.FINISHED_WAITING_FOR_CHILDREN);

				//node is finished, check children if can remove
				var childrenFinished = true;
				foreach (var child in nodeState._activeChildren)
				{
					if (child._status != FrameStatus.HIERARCHY_FINISHED)
					{
						childrenFinished = false;
						break;
					}
				}
				if (childrenFinished)
				{
					//this node and it's children are all done for this frame!
					DEBUG_finishedHierarchy++;
					nodeState._status = FrameStatus.HIERARCHY_FINISHED;
					nodeState._updateHierarchyTime = nodeState._updateStopwatch.Elapsed;
					waitingOnChildrenNodes.RemoveAt(i);
					finishedNodes.Add(nodeState);
					//record stats about this frame for easy access in the node
					nodeState._node._lastUpdate.Update(this, nodeState);
				}
			}


			if (DEBUG_startedThisPass > 0 || DEBUG_finishedHierarchy > 0 || currentTasks.Count > 0 || DEBUG_finishedNodeUpdate > 0)
			{
				//ok
			}
			else
			{
				var errorStr = $"Node execution deadlocked for frame {_stats._frameId}.  " +
					$"There are {_allNodesToProcess.Count} nodes that can not execute due to circular dependencies in UpdateBefore/After settings.  " +
					$"These are their settings (set in code) and their runtimeUpdateAfter computed values for this frame.  Check any nodes mentioned:\n";
				foreach (var node in _allNodesToProcess)
				{
					var nodeState = _frameStates[node];
					errorStr += $"   {node.GetHierarchyName()} " +
						$"updateBefore=[{String.Join(',', node._updateBefore)}] updateAfter=[{String.Join(',', node._updateAfter)}]  calculatedUpdateAfter=[{String.Join(',', nodeState._updateAfter)}]\n";
				}
				__ERROR.Throw(false, errorStr);
			}
		}

		__DEBUG.Assert(currentTasks.Count == 0);
		//nothing else to process, wait on all remaining tasks
		await Task.WhenAll(currentTasks);

		foreach (var (resource, resourceRequests) in _readRequestsRemaining)
		{
			__DEBUG.Assert(resourceRequests.Count == 0, "in single frame execution mode, expect all resourcesRequests to be fulfilled by end of frame");
		}
		foreach (var (resource, resourceRequests) in _writeRequestsRemaining)
		{
			__DEBUG.Assert(resourceRequests.Count == 0, "in single frame execution mode, expect all resourcesRequests to be fulfilled by end of frame");
		}

		//notify all nodes that our frame is done
		foreach (var node in _allNodesInFrame)
		{
			node.OnFrameFinished(this);
		}

	}
}
public partial class Frame //resource locking
{

	Frame _priorFrame;
	/// <summary>
	/// track what reads are remaining for this frame.   used so next frame writes will not start until these are empty.
	/// </summary>
	public Dictionary<object, HashSet<SimNode>> _readRequestsRemaining = new();
	/// <summary>
	/// track what writes remain for this frame.  used so next frame R/W will not start until these are empty.
	/// </summary>
	public Dictionary<object, HashSet<SimNode>> _writeRequestsRemaining = new();

	/// <summary>
	/// Returns true if the read/write resources used by this frame are available.
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	private bool AreResourcesAvailable(SimNode node)
	{
		//make sure that there are no pending resource usage in prior frames
		if (_priorFrame != null)
		{
			//for a READ resource, make sure no WRITES remaining from last frame, otherwise can not proceed yet.
			foreach (var obj in node._readResources)
			{
				if (_priorFrame._writeRequestsRemaining.TryGetValue(obj, out var nodesRemaining) && nodesRemaining.Count > 0)
				{
					return false;
				}
			}
			//for a WRITE resource, make sure no READS or WRITES remaining from last frame, otherwise can not proceed yet.
			foreach (var obj in node._writeResources)
			{
				{
					if (_priorFrame._writeRequestsRemaining.TryGetValue(obj, out var nodesRemaining) && nodesRemaining.Count > 0)
					{
						return false;
					}
				}
				{
					if (_priorFrame._readRequestsRemaining.TryGetValue(obj, out var nodesRemaining) && nodesRemaining.Count > 0)
					{
						return false;
					}
				}
			}

		}
		//reads
		foreach (var obj in node._readResources)
		{
			var rwCounter = _manager._resourceLocks._GetOrAdd(obj, () => new());
			if (rwCounter.IsWriteHeld)
			{
				return false;
			}
		}
		//writes
		foreach (var obj in node._writeResources)
		{
			var rwCounter = _manager._resourceLocks._GetOrAdd(obj, () => new());
			if (rwCounter.IsAnyHeld)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// lock the resource and decrement our remaining locks needed for this frame.
	/// </summary>
	private bool LockResources(SimNode node)
	{
		//reads
		foreach (var obj in node._readResources)
		{
			var rwCounter = _manager._resourceLocks._GetOrAdd(obj, () => new());
			rwCounter.EnterRead();
			var result = _readRequestsRemaining[obj].Remove(node);
			__DEBUG.Assert(result);

		}
		//writes
		foreach (var obj in node._writeResources)
		{
			var rwCounter = _manager._resourceLocks._GetOrAdd(obj, () => new());
			rwCounter.EnterWrite();
			var result = _writeRequestsRemaining[obj].Remove(node);
			__DEBUG.Assert(result);
		}
		return true;
	}
	private bool UnlockResources(SimNode node)
	{
		//reads
		foreach (var obj in node._readResources)
		{
			var rwLock = _manager._resourceLocks._GetOrAdd(obj, () => new());
			rwLock.ExitRead();

		}
		//writes
		foreach (var obj in node._writeResources)
		{
			var rwLock = _manager._resourceLocks._GetOrAdd(obj, () => new());
			rwLock.ExitWrite();
		}
		return true;
	}


}


public class NodeFrameState
{
	/// <summary>
	/// not all nodes are active in all frames.   this is a listing of the node's active children.
	/// </summary>
	public List<NodeFrameState> _activeChildren = new();

	/// <summary> the target node </summary>
	public SimNode _node { get; init; }
	public NodeFrameState _parent;
	public TaskCompletionSource _updateTcs { get; init; } = new();
	public FrameStatus _status { get; set; } = FrameStatus.SCHEDULED;
	public Task UpdateTask
	{
		get => _updateTcs.Task;
	}

	/// <summary>
	/// how long the update took.  used for prioritizing future frame execution order
	/// </summary>
	public TimeSpan _updateTime = TimeSpan.Zero;

	/// <summary>
	/// nodes that this node must run after
	/// </summary>
	public List<NodeFrameState> _updateAfter = new();
	internal Stopwatch _updateStopwatch;
	internal TimeSpan _updateHierarchyTime;

	public override string ToString()
	{
		return $"{_node.Name} ({_status})";
	}

	public bool CanUpdateNow()
	{


		//TODO:  check for blocking nodes (update before/after)
		//TODO:  check for r/w locks
		//TODO:  check prior frame:  this node complete + r/w locks

		__DEBUG.Assert(_status == FrameStatus.SCHEDULED, "only should run this method if scheduled and trying to put to pending");

		////ensure all children are finished
		//foreach (var child in _activeChildren)
		//{
		//	if (child._status < FrameStatus.HIERARCHY_FINISHED)
		//	{
		//		return false;
		//	}
		//}

		//ensure parent has run to SELF_FINISHED before starting		
		if (_parent == null)
		{
			__DEBUG.Assert(_node is RootNode);
		}
		else
		{
			var testStatus = _parent._status;
			var result = testStatus is FrameStatus.FINISHED_WAITING_FOR_CHILDREN or FrameStatus.SCHEDULED or FrameStatus.PENDING or FrameStatus.RUNNING;
			__DEBUG.Assert(result);
			if (_parent._status != FrameStatus.FINISHED_WAITING_FOR_CHILDREN)
			{
				return false;
			}
		}

		//ensure nodes we run after are completed
		foreach (var otherNode in _updateAfter)
		{
			if (otherNode._status != FrameStatus.HIERARCHY_FINISHED)
			{
				return false;
			}
		}


		return true;
	}
}






//}

/// <summary>
/// cheap non-blocking way to track resource availabiltiy
/// <para>NOT thread safe.</para>
/// </summary>
[ThreadSafety(ThreadSituation.Never)]
public class ReadWriteCounter
{
	public int _writes;
	public int _reads;
	private int _version;


	public bool IsReadHeld { get { return _reads > 0; } }
	public bool IsWriteHeld { get { return _writes > 0; } }
	public bool IsAnyHeld { get => IsReadHeld || IsWriteHeld; }

	public void EnterWrite()
	{
		_version++;
		var ver = _version;
		__DEBUG.Throw(IsAnyHeld == false, "a lock already held");
		_writes++;
		__DEBUG.Throw(_writes == 1, "writes out of balance");
		__DEBUG.Assert(ver == _version);
	}
	public void ExitWrite()
	{
		_version++;
		var ver = _version;
		_writes--;
		__DEBUG.Throw(_writes == 0, "writes out of balance");
		__DEBUG.Assert(ver == _version);
	}
	public void EnterRead()
	{
		_version++;
		var ver = _version;
		__DEBUG.Throw(IsWriteHeld == false, "write lock already held");
		_reads++;
		__DEBUG.Throw(_reads > 0, "reads out of balance");
		__DEBUG.Assert(ver == _version);
	}
	public void ExitRead()
	{
		_version++;
		var ver = _version;
		_reads--;
		__DEBUG.Throw(_reads >= 0, "reads out of balance");
		__DEBUG.Assert(ver == _version);
	}

}
