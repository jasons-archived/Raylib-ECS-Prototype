using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DumDum.Bcl;
using DumDum.Bcl.Diagnostics;

namespace DumDum.Engine.Ecs;


public partial class SimManager //tree management
{
	public Dictionary<string, SimNode> _nodeRegistry = new();
	public RootNode _root;

	public SimManager()
	{
		_root = new RootNode { Name = "root", _manager = this };
		_nodeRegistry.Add(_root.Name, _root);
	}

	public void Register(SimNode node)
	{
		SimNode parent;
		lock (_nodeRegistry)
		{
			__ERROR.Throw(_nodeRegistry.TryAdd(node.Name, node),$"A node with the same name of '{node.Name}' is already registered");
			var result = _nodeRegistry.TryGetValue(node.ParentName, out parent);
			__ERROR.Throw(result);
		}

		parent.OnChildRegister(node);


	}


	public void Unregister(SimNode node)
	{

		node._parent.OnChildUnregister(node);

		lock (_nodeRegistry)
		{
			var result = _nodeRegistry.TryGetValue(node.Name, out var foundNode);
			_nodeRegistry.Remove(node.Name);
			__ERROR.Throw(result);
			__ERROR.Throw(node == foundNode, "should ref equal");
		}


	}

}
public partial class SimManager //thread execution
{

	private TimeStats _stats;
	private Task _priorFrameTask;
	private Frame _frame;
	public async Task Update(TimeSpan elapsed)
	{
		_stats.Update(elapsed);
		_frame = Frame.FromPool(_stats, _frame);


		await _frame.InitializeNodeGraph(_root);

		await _frame.ExecuteNodeGraph();







		////////////////////  OLD BELOW



		//TODO: let prior frame run late, as inter-frame coordination is taken care of in the frame object
		//if (_priorFrameTask != null)
		//{
		//	while (_priorFrameTask.IsCompleted == false)
		//	{
		//		_priorFrameTask.Wait(TimeSpan.FromMilliseconds(10));
		//	}
		//	_priorFrameTask.Dispose();
		//}
		//_priorFrameTask = task;



	}


}

public class RootNode : SimNode
{
	public override Task Update(Frame frame)
	{
		return Task.CompletedTask;
	}
}

public abstract partial class SimNode  //tree logic
{
	public string Name { get; init; }
	public string ParentName { get; init; }
	public SimNode _parent;
	public SimManager _manager;

	public List<SimNode> _children = new();

	public List<SimNode> GetHierarchyChain()
	{
		var toReturn = new List<SimNode>();
		toReturn.Add(this);
		var curNode = this;
		while(curNode._parent != null)
		{
			toReturn.Add(curNode._parent);
			curNode = curNode._parent;
		}
		toReturn.Reverse();
		return toReturn;
	}

	/// <summary>
	/// returns hierarchy in the string format "NodeName|root->ParentName->NodeName"
	/// </summary>
	/// <returns></returns>
	public string GetHierarchyString()
	{
		var chain = GetHierarchyChain();

		var query = from node in chain select node.Name;
		return $"{Name}|{String.Join("->", query)}";
	}

	public override string ToString()
	{
		return $"{GetHierarchyString()}" ;

		//return $"{Name}  parent={ParentName}";

	}

	/// <summary>
	/// count of all nodes under this node (children+their children)
	/// </summary>
	/// <returns></returns>
	public int HiearchyCount()
	{
		var toReturn = _children.Count;
		foreach (var child in _children)
		{
			toReturn += child.HiearchyCount();
		}

		return toReturn;
	}

	public bool FindNode(string name, out SimNode node)
	{
		return _manager._nodeRegistry.TryGetValue(name, out node);
	}

	internal void OnChildRegister(SimNode node)
	{
		_children.Add(node);
		node.OnRegister(this, _manager);
	}

	internal void OnChildUnregister(SimNode node)
	{
		_children.Remove(node);
		node.OnUnregister();
	}

	private void OnRegister(SimNode parent, SimManager manager)
	{
		_parent = parent;
		_manager = manager;
	}

	private void OnUnregister()
	{
		_parent = null;
		_manager = null;
	}

}







public abstract partial class SimNode //update logic
{

	/// <summary> triggered for root node and propigating down.  allows node to decide if it, or it+children should participate in this frame. </summary>
	internal void OnFrameStarting(Frame frame, ICollection<SimNode> allNodesToUpdateInFrame)
	{
		//TODO:  store last updateTime metric, use this+children to estimate node priority (costly things run first!)  add out TimeSpan hiearchyLastElapsed
		//TODO:  when doing that, maybe take the average of executions.   store in frameState

		//add this node to be executed this frame
		allNodesToUpdateInFrame.Add(this);  //TODO: node filtering logic here based on FPS limiting, etc

		foreach (var child in _children)
		{
			child.OnFrameStarting(frame, allNodesToUpdateInFrame);
		}

	}

	/// <summary>
	/// //IMPORTANT: a node.Update() will fire and complete BEFORE it's children are started. 
	/// </summary>
	/// <param name="frame"></param>
	/// <returns></returns>
	public abstract Task Update(Frame frame);


	///////// <summary>
	///////// allows await of this and all children
	///////// </summary>
	///////// <param name="frame"></param>
	///////// <returns></returns>
	//////internal async Task FrameUpdateHiearchyFinished(Frame frame)
	//////{
	//////	await _frameStates[frame].UpdateTask;

	//////	__DEBUG.Assert(_frameStates[frame].UpdateTask.IsCompletedSuccessfully);

	//////	//unblock when self and all children are complete
	//////	foreach (var child in _children)
	//////	{
	//////		await child.FrameUpdateHiearchyFinished(frame);
	//////	}
	//////	kjhkhlkhj //remove above FrameUpdateHiearchyFinished() function.  instead have a per-frame (framestate) counter for children remaining
	//////	//that is allocated during the FrameUpdateHiearchyStart() method.  and decremented (down hiearchy) on every Update() complete.
	//////	//when completed, logic in the FrameState should unblock a CountdownLockSlim
	//////}

	/// <summary> frame is totally done.  clean up anything created/stored for this frame </summary>
	internal void OnFrameFinished(Frame frame)
	{
		//_frameStates.Remove(frame);
	}
}


public abstract partial class SimNode : IComparable<SimNode> //frame blocking / update order
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

	public List<string> _updateBefore = new();
	public List<string> _updateAfter = new();



}



//public abstract partial class SimNode  //frame logic
//{
//	public List<string> _updateBefore = new();
//	public List<string> _updateAfter = new();

//	internal void OnFrameInitialize(Frame frame)
//	{
//		_updateCriteria.OnFrameInitialize(frame);
//		foreach (var child in _children)
//		{
//			child.OnFrameInitialize(frame);
//		}

//		if (_updateCriteria.ShouldSkipFrame(frame))
//		{
//			SkipFrame(frame);
//		}
//	}

//}


//public class NodeFrameState
//{
//	public Frame _frame;
//	public HashSet<SimNode> _blockedBy = new();
//	public HashSet<SimNode> _blocking = new();

//	public int Priority
//	{
//		get
//		{
//			if (_blockedBy.Count > 0)
//			{
//				return 0;
//			}

//			return _blocking.Count;
//		}
//	}

//}

//public class UpdateCriteria
//{
//	public List<string> _updateBefore = new();
//	public List<string> _updateAfter = new();

//	public SimNode _owner{ get; init; }

//	public void OnFrameInitialize(Frame frame)
//	{asdfasdfasdfasdfa  
//			//TODO: start here with constructing the blocking criterias
//		//populate frame with blocking criteria
//		throw new NotImplementedException();
//	}

//	public bool IsBlocked(Frame frame)
//	{
//		throw new NotImplementedException();
//	}

//	internal bool ShouldSkipFrame(Frame frame)
//	{
//		throw new NotImplementedException();
//	}
//}

public partial class Frame //general setup
{
	public TimeStats _stats;

	internal static Frame FromPool(TimeStats stats, Frame priorFrame)
	{
		//TODO: make chain, recycle old stuff every update
		return new Frame() { _stats = stats };

	}
	public override string ToString()
	{
		return _stats.ToString();
	}

}

public partial class Frame ////node graph setup and execution
{
	private List<SimNode> _allNodesInFrame = new();
	private List<SimNode> _allNodesToProcess = new();
	private Dictionary<SimNode, NodeFrameState> _frameStates = new();


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
		_allNodesInFrame.Sort();
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
			if (node._parent == null)
			{
				__DEBUG.Assert(node.GetType() == typeof(RootNode), "only root node should not have parent");
				continue;
			}
			var thisNodeState = _frameStates[node];
			var parentNodeState = _frameStates[node._parent];
			parentNodeState._activeChildren.Add(thisNodeState);
			thisNodeState._parent = parentNodeState;
		}

		//now that our tree of NodeFrameStates is setup properly, reloop calculating execution order dependencies
		foreach (var node in _allNodesInFrame)
		{
			var frameState = _frameStates[node];


			//TODO: calculate and store all runBefore/ runAfter dependencies


			//updateAfter
			foreach(var afterName in node._updateAfter)
			{				
				if(!node.FindNode(afterName, out var afterNode))
				{
					__DEBUG.AssertOnce(false, $"'{afterName}' node is listed as an updateAfter dependency in '{node.GetHierarchyString()}' node.  target node not registered");
					continue;
				}
				if(!_frameStates.TryGetValue(afterNode, out var afterNodeState))
				{
					__DEBUG.Assert(false, "missing?  maybe ok.  node not participating in this frame");
					continue;
				}
				frameState._updateAfter.Add(afterNodeState);
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
				beforeNodeState._updateAfter.Add(frameState);
			}





			//TODO: calc and store resource R/W locking
		}


	}
	//gc perf cleanup
	//todo now:  add before/after node features
	//test with example nodes
	//fixup files/namespaces ("runtime" namespace)
	//generic node exclusion (read/write) policy

	public async Task ExecuteNodeGraph()
	{

		//process nodes
		var maxThreads = Environment.ProcessorCount + 2;
		var currentTasks = new List<Task>();
		var activeNodes = new List<NodeFrameState>();
		var finishedNodes = new List<NodeFrameState>();

		while (_allNodesToProcess.Count > 0 || currentTasks.Count>0)
		{
			var DEBUG_startedThisPass = 0;
			var DEBUG_finishedNodeUpdate = 0;
			var DEBUG_finishedHierarchy = 0;


			//try to execute highest priority first
			for (var i = _allNodesToProcess.Count - 1; i >= 0; i--)
			{
				var node = _allNodesToProcess[i];
				var frameState = _frameStates[node];
				if (frameState.CanUpdateNow())
				{
					__DEBUG.Assert(frameState._status == FrameStatus.SCHEDULED);
					frameState._status = FrameStatus.PENDING;

					_allNodesToProcess.RemoveAt(i);

					var updateTimer = Stopwatch.StartNew();

					__DEBUG.Assert(frameState._status == FrameStatus.PENDING);
					frameState._status = FrameStatus.RUNNING;
					activeNodes.Add(frameState);

					var updateTask = node.Update(this).ContinueWith(async (task) =>
					{
						__DEBUG.Assert(frameState._status == FrameStatus.RUNNING);
						frameState._status = FrameStatus.FINISHED_WAITING_FOR_CHILDREN;
						frameState._updateTime = updateTimer.Elapsed;
						frameState._updateTcs.SetFromTask(task);
					});
					currentTasks.Add(updateTask);
					DEBUG_startedThisPass++;
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

			//loop through all active nodes, and if FINISHED and all children are HIERARCHY_FINISHED then mark this as finished and remove from active
			for (var i = activeNodes.Count - 1; i >= 0; i--)
			{
				var nodeState = activeNodes[i];
				__DEBUG.Assert(nodeState._status == FrameStatus.RUNNING || nodeState._status == FrameStatus.FINISHED_WAITING_FOR_CHILDREN);

				if (nodeState._status != FrameStatus.FINISHED_WAITING_FOR_CHILDREN)
				{
					continue;
				}
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
					DEBUG_finishedHierarchy++;
					nodeState._status = FrameStatus.HIERARCHY_FINISHED;
					activeNodes.RemoveAt(i);
					finishedNodes.Add(nodeState);
				}
			}


			if(DEBUG_startedThisPass > 0 || DEBUG_finishedHierarchy > 0 || currentTasks.Count > 0 || DEBUG_finishedNodeUpdate > 0)
			{
				//ok
			}
			else
			{
				var errorStr = $"Node execution deadlocked for frame {_stats._frameId}.  " +
					$"There are {_allNodesToProcess.Count} nodes that can not execute due to circular dependencies in UpdateBefore/After settings.  " +
					$"These are their settings (set in code) and their runtimeUpdateAfter computed values for this frame.  Check any nodes mentioned:\n";
				foreach(var node in _allNodesToProcess)
				{
					var nodeState = _frameStates[node];
					errorStr += $"   {node.GetHierarchyString()} " +
						$"updateBefore=[{String.Join(',', node._updateBefore)}] updateAfter=[{String.Join(',', node._updateAfter)}]  runtimeUpdateAfter=[{String.Join(',', nodeState._updateAfter)}]\n";
				}
				__ERROR.Throw(false, errorStr);
			}
		}

		__DEBUG.Assert(currentTasks.Count == 0);
		//nothing else to process, wait on all remaining tasks
		await Task.WhenAll(currentTasks);

		//notify all nodes that our frame is done
		foreach (var node in _allNodesInFrame)
		{
			node.OnFrameFinished(this);
		}

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
		foreach(var otherNode in _updateAfter)
		{
			if (otherNode._status != FrameStatus.HIERARCHY_FINISHED)
			{
				return false;
			}
		}


		return true;
	}
}



///// <summary>
///// constrain your node's execution by specifying what nodes need to run first, and what components your node needs read/write access to.
///// </summary>
//public class ExecCriteria
//{
//	public List<string> _updateAfterNodeNames = new();
//	public List<string> _updateAfterNodeCategories = new();

//	//TODO: figure out compnent r/w access
//	public List<Type> _readAccess = new();
//	public List<Type> _writeAccess = new();

//	internal bool IsUpdateBlocked(Node node, ExecManager execManager)
//	{
//		//ensure updateAfter nodes 
//		throw new NotImplementedException();
//	}

//	internal bool IsUpdateAfterBlockedBy(Node otherPending)
//	{
//		if (_updateAfterNodeNames.Contains(otherPending.Name))
//		{
//			return true;
//		}

//		foreach (var cat in _updateAfterNodeCategories)
//		{
//			if (otherPending.Categories.Contains(cat))
//			{
//				return true;
//			}
//		}
//		return false;
//	}

//	internal void UpdateScheduled(Node node, ExecManager execManager)
//	{
//		throw new NotImplementedException();
//	}




//	/// <summary>
//	/// the target framerate you want to execute at.
//	/// Note that nested groups will already be constrained by the parent group TargetFrameRate,
//	/// but this property can still be set for the child group. In that case the child group will update at the slowest of the two TargetFrameRates.
//	/// <para>default is int.MaxValue (update as fast as possible (every tick))</para>
//	/// </summary>
//	public double TargetFrameRate
//	{
//		get => 1 / _targetUpdateInterval.TotalSeconds;
//		set => _targetUpdateInterval = TimeSpan.FromSeconds(1 / value);
//	}
//	private TimeSpan _targetUpdateInterval = TimeSpan.Zero;
//	/// <summary>
//	/// If set to true, attempts to ensure we update at precisely the rate specified.  if we execute slightly later than the TargetFrameRate, the extra delay is not ignored and is taken into consideration on future updates.  
//	/// </summary>
//	public bool FixedStep = false;

//	/// <summary>
//	/// if our update is more than this many frames out of date, we ignore others.
//	/// <para> only matters if FixedStep==true</para>
//	/// </summary>
//	public int CatchUpMaxFrames = 1;


//}
