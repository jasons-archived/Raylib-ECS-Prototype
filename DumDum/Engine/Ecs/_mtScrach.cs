using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DumDum.Bcl.Diagnostics;
using DumDum.Engine._internal;

namespace DumDum.Engine.Ecs;







///// <summary>
///// just a singlethreaded, simple execution manager
///// </summary>
//public class ExecManager
//{



//	private List<SystemGroup> _childGroups = new();
//	/// <summary>
//	/// ExecManager  root of the system graph.  It's direct children are listed here.  if you need a sub-child, look at <see cref="RegisteredNodes"/>
//	/// </summary>
//	public IReadOnlyList<SystemGroup> ChildGroups => _childGroups;



//	//private HashSet<string> _registeredNodeNames = new();
//	private Dictionary<string, Node> _registeredNodes = new();
//	public IReadOnlyDictionary<string, Node> RegisteredNodes => _registeredNodes;



//	/// <summary>
//	/// called by SystemGroup when a child registers
//	/// </summary>
//	/// <param name="node"></param>
//	internal void _InformOnRegister(Node node)
//	{
//		__ERROR.Throw(_registeredNodes.TryAdd(node.Name, node), $"Another node with name '{node.Name}' has already been added");
//	}
//	internal void _InformOnUnregister(Node node)
//	{
//		__ERROR.Throw(_registeredNodes.Remove(node.Name), $"Node with name '{node.Name}' was not found to be removed");
//	}


//	public void Register(SystemGroup group)
//	{

//		_InformOnRegister(group);
//		_childGroups.Add(group);
//		group.OnRegister(null, this);

//	}

//	public void Unregister(SystemGroup group)
//	{
//		_InformOnUnregister(group);
//		_childGroups.Remove(group);
//		group.OnUnregister(null, this);
//	}

//	public void BeginRun()
//	{

//	}

//	//TODO:  updateBefore() should store a temp record of this, and every update when adding a node to the _tempToUpdateThisTick we add an updateAfter remark.



//	private ExecState _execState = new();
//	private Random _rand = new();




//	public Dictionary<string, ExecStatus> _currentFrameNodeStatus = new();
//	//private HashSet<Node> _tempFinishedNodes = new();
//	private List<Node> _currentFrameScheduled = new();
//	/// <summary>
//	/// helper to track what nodes are ready for updating.  removed as soon as they are updated.
//	/// </summary>
//	private List<Node> _currentFrameIMMINENT = new();
//	///// <summary>
//	///// helper to track what nodes we have already marked as "ready to execute" this tick.
//	///// </summary>
//	//private HashSet<Node> _tempUnblockedAndReadyForUpdate_Lookup = new();
//	private List<Node> _currentFrameRunning = new();
//	private HashSet<Node> _currentFrameFinished = new();


//	internal void _ScheduleNodesForThisFrame(Node childNode, List<Node> _children)
//	{
//		foreach (var node in _children)
//		{
//			CurrentFrameScheduleNode(node);
//		}
//	}
//	internal void _SkipNodesForThisFrame(Node childNode, List<Node> _children)
//	{
//		foreach (var node in _children)
//		{
//			__DEBUG.Assert(_currentFrameNodeStatus.ContainsKey(node.Name) == false, "being added, shouldn't exist");
//			_currentFrameNodeStatus.Add(node.Name, ExecStatus.SKIPPED);
//			_currentFrameFinished.Add(node);
//		}
//	}

//	protected internal void CurrentFrameScheduleNode(Node node)
//	{
//		__DEBUG.Assert(_currentFrameNodeStatus.ContainsKey(node.Name) == false, "being added, shouldn't exist");
//		_currentFrameNodeStatus.Add(node.Name, ExecStatus.SCHEDULED);
//		_currentFrameScheduled.Add(node);
//		node.UpdateScheduled();
//	}

//	public void Update(TimeSpan elapsed)
//	{
//		_execState.Update(elapsed);
//		_currentFrameFinished.Clear();

//		//algo:  loop through all our nodes, executing those not blocked by anything (either no blocks or blocks are finished), and moving those into the _tempFinished set.


//		__DEBUG.Assert(
//			_currentFrameNodeStatus.Count == 0
//			&& _currentFrameScheduled.Count == 0
//			&& _currentFrameIMMINENT.Count == 0
//			&& _currentFrameRunning.Count == 0
//			&& _currentFrameFinished.Count == 0
//			, "algo error: should clear these"
//		);

//		//loop through all registered nodes and record their blockers
//		foreach ((var nodeName, var node) in _registeredNodes)
//		{
//			node._execCriteria.
//		}


//		//put all our direct children into the update queue
//		foreach (var group in _childGroups)
//		{
//			CurrentFrameScheduleNode(group);
//		}



//		while (_currentFrameScheduled.Count > 0 || _currentFrameIMMINENT.Count > 0 || _currentFrameRunning.Count > 0)
//		{
//			var loopExecCount = 0;

//			//try to add all unblocked nodes to IMMINENT
//			for (var i = _currentFrameScheduled.Count - 1; i >= 0; i--)
//			{
//				var node = _currentFrameScheduled[i];
//				if (node._execCriteria.IsBlocked(this) == false)
//				{
//					_currentFrameScheduled.RemoveAt(i);
//					_currentFrameNodeStatus[node.Name] = ExecStatus.PENDING;
//					_currentFrameIMMINENT.Add(node);
//				}
//			}


//			//get a random Node from our pending, execute it
//			{
//				var result = _currentFrameIMMINENT._TryRemoveRandom(out var node);
//				__ERROR.Throw(result, "no nodes are unblocked though we have more scheduled for this frame");
//				_currentFrameRunning.Add(node);
//				_currentFrameNodeStatus[node.Name] = ExecStatus.RUNNING;
//				node.Update(_execState);
//			}

//			//loop through all currently running, and those finished remove and complete.
//			for (var i = _currentFrameRunning.Count - 1; i >= 0; i--)
//			{
//				var node = _currentFrameRunning[i];
//				if (node.CurrentFrameIsRunning())
//				{
//					//right now, groups will be blocked waiting for children.  later this will block while nodes are executing async
//					continue;
//				}
//				//node is done
//				_currentFrameRunning.Remove(node);
//				node.EndUpdate();
//				_currentFrameNodeStatus[node.Name] = ExecStatus.FINISHED;
//				_currentFrameFinished.Add(node);
//				loopExecCount++;
//			}

//			__DEBUG.Assert(loopExecCount > 0, "are we deadlocked?");


//		}















//		//		//check and verify that all nodes being waited on exist.
//		//		//TODO: rewrite following checked code
//		//#if CHECKED

//		//		//foreach (var group in _childGroups)
//		//		//{
//		//		//	foreach (var updateAfterName in node._execCriteria._updateAfterNodeNames)
//		//		//	{
//		//		//		__CHECKED.AssertOnce(_registeredNodeNames.Contains(updateAfterName), $"The node {node.Name} is specified to update after node {updateAfterName} but that node is not registered with the ExecManager.  This dependency will be assumed to be fulfuilled (nothing to wait on)");
//		//		//	}
//		//		//}
//		//#endif


//		//		//TODO:  ensure that all nodes that wait on named nodes actually have nodes of that name added.
//		//		//throw new NotImplementedException();




//		//		//randomize execution order of those nodes that can execute now (good for debugging dependency problems)
//		//		while (this._tempToUpdateThisTick.Count > 0)
//		//		{

//		//			//try to add all unblocked nodes 
//		//			foreach (var currentInspectedPending in this._tempToUpdateThisTick)
//		//			{
//		//				if (_tempUnblockedAndReadyForUpdate_Lookup.Contains(currentInspectedPending))
//		//				{
//		//					//already marked as unblocked so skip checking again
//		//					continue;
//		//				}

//		//				var isBlocked = false;
//		//				//if the current node has no blocks or all its blocks have already finished
//		//				foreach (var otherPending in _tempToUpdateThisTick)
//		//				{
//		//					if (currentInspectedPending._execCriteria.IsUpdateAfterBlockedBy(otherPending))
//		//					{
//		//						isBlocked = true;
//		//						break;
//		//					}
//		//				}

//		//				if (isBlocked)
//		//				{
//		//					continue;
//		//				}
//		//				//not blocked
//		//				_tempUnblockedAndReadyForUpdate_Lookup.Add(currentInspectedPending);
//		//				_tempUnblockedAndReadyForUpdate.Add(currentInspectedPending);

//		//				//if (!node._updateAfter.Any() || _tempFinished.IsSupersetOf(node._updateAfter))
//		//				//{
//		//				//	if (_tempUnblockedAndReadyForUpdate_Lookup.Add(node))
//		//				//	{
//		//				//		_tempUnblockedAndReadyForUpdate.Add(node);
//		//				//	}
//		//				//}
//		//			}

//		//			var loopExecCount = 0;

//		//			//NOTE: this was a WHILE loop.
//		//			//changing to an IF so that we can maximize randomness of node execution order.
//		//			// Why this change to IF allows it:  only execute one node randomly from all "_tempUnblockedAndReadyForUpdate" choices,
//		//			//then go back to the above WHILE to add any more just-unblocked nodes to our choices.
//		//			if (_tempUnblockedAndReadyForUpdate.Count > 0)
//		//			{
//		//				//get a random node
//		//				var index = _rand.Next(0, _tempUnblockedAndReadyForUpdate.Count);
//		//				var node = _tempUnblockedAndReadyForUpdate[index];
//		//				_tempUnblockedAndReadyForUpdate.RemoveAt(index);
//		//				this._tempToUpdateThisTick.Remove(node);

//		//				//execute it
//		//				_tempNowUpdating.Add(node);
//		//				node.BeginUpdate();
//		//				node.Update(_execState);
//		//				node.EndUpdate();
//		//				_tempNowUpdating.Remove(node);


//		//				//mark it as finished
//		//				var result = _tempFinishedNodes.Add(node);
//		//				__DEBUG.Assert(result);
//		//				loopExecCount++;
//		//			}




//		//			__DEBUG.Throw(loopExecCount > 0, $"No nodes executed.  We are in a deadlock state.  There are {_tempToUpdateThisTick.Count} nodes still remaining.  " +
//		//											 $"At least one node should have executed per WHILE loop, or we will be stuck in an infinite loop." +
//		//											 $" Possible cause is a node that has an invalid .UpdateAfter() dependency");
//		//		}

//		//		__DEBUG.Assert(_tempUnblockedAndReadyForUpdate.Count == 0, "above should have cleared as part of algo");
//		//		_tempUnblockedAndReadyForUpdate_Lookup.Clear();
//		//		this._tempToUpdateThisTick.Clear();
//		//		_tempFinishedNodes.Clear();
//	}



//}
