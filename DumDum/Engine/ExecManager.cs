using DumDum.Bcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DumDum.Bcl.Diagnostics;
using DumDum.Engine._internal;

namespace DumDum.Engine;

/**
 * A special node that specifies groupings of nodes that should update together
 */
public class SystemGroup : ExecNodeBase
{



	/// <summary>
	/// the target framerate you want to execute at.
	/// Note that nested groups will already be constrained by the parent group TargetFrameRate,
	/// but this property can still be set for the child group. In that case the child group will update at the slowest of the two TargetFrameRates.
	/// <para>default is int.MaxValue (update as fast as possible (every tick))</para>
	/// </summary>
	public double TargetFrameRate
	{
		get => 1 / _targetUpdateInterval.TotalSeconds;
		set => _targetUpdateInterval = TimeSpan.FromSeconds(1 / value);
	}
	private TimeSpan _targetUpdateInterval = TimeSpan.Zero;
	/// <summary>
	/// If set to true, attempts to ensure we update at precisely the rate specified.  if we execute slightly later than the TargetFrameRate, the extra delay is not ignored and is taken into consideration on future updates.  
	/// </summary>
	public bool FixedStep = false;

	/// <summary>
	/// if our update is more than this many frames out of date, we ignore others.
	/// <para> only matters if FixedStep==true</para>
	/// </summary>
	public int CatchUpMaxFrames = 1;

	public SystemGroup _parent;
	public List<ExecNodeBase> _children = new();
	public HashSet<ExecNodeBase> _childrenCurrentlyUpdating = new();
	public TimeSpan _elapsedSinceLastUpdate = TimeSpan.Zero;

	private ExecManager _execManager;



	public void Register(ExecNodeBase node)
	{
		//check with exec mnager to be sure node name is unique
		_execManager._InformOnRegister(node);

		//add to local hiearchy
		_children.Add(node);

		//inform node that registered
		node.OnRegister(this, _execManager);
	}

	public void Unregister(ExecNodeBase node)
	{
		_execManager._InformOnUnregister(node);

		_children.Remove(node);
		node.OnUnregister(this, _execManager);
	}


	//TODO: add SystemGroup registration to ExecManager

	internal override void Update(ExecState state)
	{
		//determine if we should execute
		_elapsedSinceLastUpdate.Add(state._frameElapsed);
		if (_elapsedSinceLastUpdate < _targetUpdateInterval)
		{
			//not yet time
			_execManager._SkipNodesForThisFrame(this, _children);
			return;
		}

		if (FixedStep == false)
		{
			//don't keep extra
			_elapsedSinceLastUpdate = TimeSpan.Zero;
		}
		else
		{
			//time to execute, lets update our internal tracking of when next to update (_elapsedSinceLastUpdate)
			if (CatchUpMaxFrames == 0)
			{
				//no catchup so skip frames if we are way too out of date
				_elapsedSinceLastUpdate = TimeSpan.FromSeconds(_elapsedSinceLastUpdate.TotalSeconds % _targetUpdateInterval.TotalSeconds);
			}
			else
			{
				//logic to deal with catchup frames
				if (_elapsedSinceLastUpdate > (_targetUpdateInterval * CatchUpMaxFrames))
				{
					_elapsedSinceLastUpdate = (_targetUpdateInterval * CatchUpMaxFrames).Add(state._frameElapsed);
				}
				_elapsedSinceLastUpdate -= _targetUpdateInterval;
			}
		}

		//now executing.   enqueue our children to execute.
		_execManager._ScheduleNodesForThisFrame(this, _children);
	}
}



/// <summary>
/// just a singlethreaded, simple execution manager
/// </summary>
public class ExecManager
{



	private List<SystemGroup> _childGroups = new();
	/// <summary>
	/// ExecManager  root of the system graph.  It's direct children are listed here.  if you need a sub-child, look at <see cref="RegisteredNodes"/>
	/// </summary>
	public IReadOnlyList<SystemGroup> ChildGroups => _childGroups;



	//private HashSet<string> _registeredNodeNames = new();
	private Dictionary<string, ExecNodeBase> _registeredNodes = new();
	public IReadOnlyDictionary<string, ExecNodeBase> RegisteredNodes => _registeredNodes;



	/// <summary>
	/// called by SystemGroup when a child registers
	/// </summary>
	/// <param name="node"></param>
	internal void _InformOnRegister(ExecNodeBase node)
	{
		__ERROR.Throw(_registeredNodes.TryAdd(node.Name, node), $"Another node with name '{node.Name}' has already been added");
	}
	internal void _InformOnUnregister(ExecNodeBase node)
	{
		__ERROR.Throw(_registeredNodes.Remove(node.Name), $"Node with name '{node.Name}' was not found to be removed");
	}


	public void Register(SystemGroup group)
	{

		_InformOnRegister(group);
		_childGroups.Add(group);
		group.OnRegister(null, this);

	}

	public void Unregister(SystemGroup group)
	{
		_InformOnUnregister(group);
		_childGroups.Remove(group);
		group.OnUnregister(null, this);
	}

	public void BeginRun()
	{

	}

	//TODO:  updateBefore() should store a temp record of this, and every update when adding a node to the _tempToUpdateThisTick we add an updateAfter remark.



	private ExecState _execState = new();
	private Random _rand = new();


	/// <summary>
	/// key should not update yet, because it's being stopped by value, which wants to complete first.
	/// <para>records added by VALUE</para>
	/// </summary>
	public Dictionary<string, ExecNodeBase> _currentFrameUpdateBeforeBlocks = new();
	/// <summary>
	/// key should not update yet, becasue it wants to wait until VALUE has completed.
	/// <para>records added by KEY</para>
	/// </summary>
	public Dictionary<ExecNodeBase, string> _currentFrameUpdateAfterBlocks = new();


	public Dictionary<string, ExecStatus> _currentFrameNodeStatus = new();
	//private HashSet<ExecNodeBase> _tempFinishedNodes = new();
	private List<ExecNodeBase> _currentFrameScheduled = new();
	/// <summary>
	/// helper to track what nodes are ready for updating.  removed as soon as they are updated.
	/// </summary>
	private List<ExecNodeBase> _currentFrameIMMINENT = new();
	///// <summary>
	///// helper to track what nodes we have already marked as "ready to execute" this tick.
	///// </summary>
	//private HashSet<ExecNodeBase> _tempUnblockedAndReadyForUpdate_Lookup = new();
	private List<ExecNodeBase> _currentFrameRunning = new();
	private HashSet<ExecNodeBase> _currentFrameFinished = new();


	internal void _ScheduleNodesForThisFrame(ExecNodeBase childNode, List<ExecNodeBase> _children)
	{
		foreach (var node in _children)
		{
			CurrentFrameScheduleNode(node);
		}
	}
	internal void _SkipNodesForThisFrame(ExecNodeBase childNode, List<ExecNodeBase> _children)
	{
		foreach (var node in _children)
		{
			__DEBUG.Assert(_currentFrameNodeStatus.ContainsKey(node.Name) == false, "being added, shouldn't exist");
			_currentFrameNodeStatus.Add(node.Name, ExecStatus.SKIPPED);
			_currentFrameFinished.Add(node);
		}
	}

	protected internal void CurrentFrameScheduleNode(ExecNodeBase node)
	{
		__DEBUG.Assert(_currentFrameNodeStatus.ContainsKey(node.Name) == false, "being added, shouldn't exist");
		_currentFrameNodeStatus.Add(node.Name, ExecStatus.SCHEDULED);
		_currentFrameScheduled.Add(node);
		node.UpdateScheduled();
	}

	public void Update(TimeSpan elapsed)
	{
		_execState.Update(elapsed);
		_currentFrameFinished.Clear();

		//algo:  loop through all our nodes, executing those not blocked by anything (either no blocks or blocks are finished), and moving those into the _tempFinished set.


		__DEBUG.Assert(
			_currentFrameNodeStatus.Count == 0
			&& _currentFrameScheduled.Count == 0
			&& _currentFrameIMMINENT.Count == 0
			&& _currentFrameRunning.Count == 0
			&& _currentFrameFinished.Count == 0
			, "algo error: should clear these"
		);




		//put all our direct children into the update queue
		foreach (var group in _childGroups)
		{
			CurrentFrameScheduleNode(group);
		}



		while (_currentFrameScheduled.Count > 0 || _currentFrameIMMINENT.Count > 0 || _currentFrameRunning.Count > 0)
		{
			var loopExecCount = 0;

			//try to add all unblocked nodes to IMMINENT
			for (var i = _currentFrameScheduled.Count - 1; i >= 0; i--)
			{
				var node = _currentFrameScheduled[i];
				if (node._execCriteria.IsBlocked(this) == false)
				{
					_currentFrameScheduled.RemoveAt(i);
					_currentFrameNodeStatus[node.Name] = ExecStatus.PENDING;
					_currentFrameIMMINENT.Add(node);
				}
			}


			//get a random Node from our pending, execute it
			{
				var result = _currentFrameIMMINENT._TryRemoveRandom(out var node);
				__ERROR.Throw(result, "no nodes are unblocked though we have more scheduled for this frame");
				_currentFrameRunning.Add(node);
				_currentFrameNodeStatus[node.Name] = ExecStatus.RUNNING;
				node.Update(_execState);
			}

			//loop through all currently running, and those finished remove and complete.
			for (var i = _currentFrameRunning.Count - 1; i >= 0; i--)
			{
				var node = _currentFrameRunning[i];
				if (node.CurrentFrameIsRunning())
				{
					//right now, groups will be blocked waiting for children.  later this will block while nodes are executing async
					continue;
				}
				//node is done
				_currentFrameRunning.Remove(node);
				node.EndUpdate();
				_currentFrameNodeStatus[node.Name] = ExecStatus.FINISHED;
				_currentFrameFinished.Add(node);
				loopExecCount++;
			}

			__DEBUG.Assert(loopExecCount > 0, "are we deadlocked?");


		}















		//		//check and verify that all nodes being waited on exist.
		//		//TODO: rewrite following checked code
		//#if CHECKED

		//		//foreach (var group in _childGroups)
		//		//{
		//		//	foreach (var updateAfterName in node._execCriteria._updateAfterNodeNames)
		//		//	{
		//		//		__CHECKED.AssertOnce(_registeredNodeNames.Contains(updateAfterName), $"The node {node.Name} is specified to update after node {updateAfterName} but that node is not registered with the ExecManager.  This dependency will be assumed to be fulfuilled (nothing to wait on)");
		//		//	}
		//		//}
		//#endif


		//		//TODO:  ensure that all nodes that wait on named nodes actually have nodes of that name added.
		//		//throw new NotImplementedException();




		//		//randomize execution order of those nodes that can execute now (good for debugging dependency problems)
		//		while (this._tempToUpdateThisTick.Count > 0)
		//		{

		//			//try to add all unblocked nodes 
		//			foreach (var currentInspectedPending in this._tempToUpdateThisTick)
		//			{
		//				if (_tempUnblockedAndReadyForUpdate_Lookup.Contains(currentInspectedPending))
		//				{
		//					//already marked as unblocked so skip checking again
		//					continue;
		//				}

		//				var isBlocked = false;
		//				//if the current node has no blocks or all its blocks have already finished
		//				foreach (var otherPending in _tempToUpdateThisTick)
		//				{
		//					if (currentInspectedPending._execCriteria.IsUpdateAfterBlockedBy(otherPending))
		//					{
		//						isBlocked = true;
		//						break;
		//					}
		//				}

		//				if (isBlocked)
		//				{
		//					continue;
		//				}
		//				//not blocked
		//				_tempUnblockedAndReadyForUpdate_Lookup.Add(currentInspectedPending);
		//				_tempUnblockedAndReadyForUpdate.Add(currentInspectedPending);

		//				//if (!node._updateAfter.Any() || _tempFinished.IsSupersetOf(node._updateAfter))
		//				//{
		//				//	if (_tempUnblockedAndReadyForUpdate_Lookup.Add(node))
		//				//	{
		//				//		_tempUnblockedAndReadyForUpdate.Add(node);
		//				//	}
		//				//}
		//			}

		//			var loopExecCount = 0;

		//			//NOTE: this was a WHILE loop.
		//			//changing to an IF so that we can maximize randomness of node execution order.
		//			// Why this change to IF allows it:  only execute one node randomly from all "_tempUnblockedAndReadyForUpdate" choices,
		//			//then go back to the above WHILE to add any more just-unblocked nodes to our choices.
		//			if (_tempUnblockedAndReadyForUpdate.Count > 0)
		//			{
		//				//get a random node
		//				var index = _rand.Next(0, _tempUnblockedAndReadyForUpdate.Count);
		//				var node = _tempUnblockedAndReadyForUpdate[index];
		//				_tempUnblockedAndReadyForUpdate.RemoveAt(index);
		//				this._tempToUpdateThisTick.Remove(node);

		//				//execute it
		//				_tempNowUpdating.Add(node);
		//				node.BeginUpdate();
		//				node.Update(_execState);
		//				node.EndUpdate();
		//				_tempNowUpdating.Remove(node);


		//				//mark it as finished
		//				var result = _tempFinishedNodes.Add(node);
		//				__DEBUG.Assert(result);
		//				loopExecCount++;
		//			}




		//			__DEBUG.Throw(loopExecCount > 0, $"No nodes executed.  We are in a deadlock state.  There are {_tempToUpdateThisTick.Count} nodes still remaining.  " +
		//											 $"At least one node should have executed per WHILE loop, or we will be stuck in an infinite loop." +
		//											 $" Possible cause is a node that has an invalid .UpdateAfter() dependency");
		//		}

		//		__DEBUG.Assert(_tempUnblockedAndReadyForUpdate.Count == 0, "above should have cleared as part of algo");
		//		_tempUnblockedAndReadyForUpdate_Lookup.Clear();
		//		this._tempToUpdateThisTick.Clear();
		//		_tempFinishedNodes.Clear();
	}



}

public enum ExecStatus
{
	NONE,
	SCHEDULED,
	PENDING,
	RUNNING,
	FINISHED,
	SKIPPED,
}

/// <summary>
/// constrain your node's execution by specifying what nodes need to run first, and what components your node needs read/write access to.
/// </summary>
public class ExecCriteria
{
	public List<string> _updateAfterNodeNames = new();
	public List<string> _updateAfterNodeCategories = new();

	//TODO: figure out compnent r/w access
	public List<Type> _readAccess = new();
	public List<Type> _writeAccess = new();

	internal bool IsUpdateBlocked(ExecNodeBase execNodeBase, ExecManager execManager)
	{
		//ensure updateAfter nodes 
		throw new NotImplementedException();
	}

	internal bool IsUpdateAfterBlockedBy(ExecNodeBase otherPending)
	{
		if (_updateAfterNodeNames.Contains(otherPending.Name))
		{
			return true;
		}

		foreach (var cat in _updateAfterNodeCategories)
		{
			if (otherPending.Categories.Contains(cat))
			{
				return true;
			}
		}
		return false;
	}

	internal void UpdateScheduled(ExecNodeBase execNodeBase, ExecManager execManager)
	{
		throw new NotImplementedException();
	}
}


public abstract class ExecNodeBase
{
	public string _parentSystemGroupName;

	protected SystemGroup _parent;
	protected ExecManager _execManager;

	private static HashSet<string> _names = new();

	public List<string> Categories = new();

	private string _name;
	public string Name
	{
		get => _name;
		init
		{
			lock (_names)
			{
				var result = _names.Add(value);
				__ERROR.Throw(result,
					$"Error naming node '{value}' because another node already exists with the same name");
			}
			_name = value;

		}
	}
	~ExecNodeBase()
	{
		lock (_names)
		{
			_names.Remove(Name);
		}
	}

	public ExecCriteria _execCriteria = new();
	///// <summary>
	///// names of nodes this will update after
	///// </summary>
	//internal List<string> _updateAfter = new();
	//protected void UpdateAfter(string nodeName)
	//{
	//	_updateAfter.Add(nodeName);
	//}
	//protected void UpdateAfter(ExecNodeBase node)
	//{
	//	_updateAfter.Add(node.Name);
	//}

	/// <summary>
	/// triggered when attaching to the engine.  
	/// </summary>
	/// <param name="execManager"></param>
	internal void OnRegister(SystemGroup parent, ExecManager execManager)
	{
		if (_parentSystemGroupName != null)
		{
			__DEBUG.Assert(parent.Name == _parentSystemGroupName);
		}
		_parent = parent;
		_execManager = execManager;
	}

	/// <summary>
	/// triggered when detached
	/// </summary>
	/// <param name="execManager"></param>
	internal void OnUnregister(SystemGroup parent, ExecManager execManager)
	{
		_parent = null;
		_execManager = null;
	}


	internal abstract void Update(ExecState state);


	private int _frameCurrentlyScheduled = 0;
	private int _frameLastFinished = 0;
	/// <summary>
	/// this gets triggered when the node becomes scheduled for execution in the current tick.
	/// execution may not happen immediately because of other nodes
	/// </summary>
	internal void UpdateScheduled(ExecState execState)
	{
		__DEBUG.Assert(_frameCurrentlyScheduled < execState._totalFrames && _frameCurrentlyScheduled==_frameLastFinished);
		_frameCurrentlyScheduled = execState._totalFrames;

		//inform parent hiearchy that we are updating (blocking it's completion)
		var curParent = _parent;
		while (curParent != null)
		{
			lock (curParent._childrenCurrentlyUpdating)
			{
				curParent._childrenCurrentlyUpdating.Add(this);
				curParent = curParent._parent;
			}
		}

		_execCriteria.UpdateScheduled(this, _execManager);
	}

	internal bool IsUpdateBlocked()
	{
		return _execCriteria.IsUpdateBlocked(this, _execManager);
	}
	/// <summary>
	/// any housekeeping your node needs to do after the main update loop can go here
	/// </summary>
	internal void EndUpdate(ExecState execState)
	{
		//inform parent hiearchy that we are updating (blocking it's completion)
		var curParent = _parent;
		while (curParent != null)
		{
			lock (curParent._childrenCurrentlyUpdating)
			{
				curParent._childrenCurrentlyUpdating.Remove(this);
				curParent = curParent._parent;
			}
		}

		__DEBUG.Assert(_frameCurrentlyScheduled == execState._totalFrames &&
		               _frameLastFinished < _frameCurrentlyScheduled);
		_frameLastFinished = execState._totalFrames;

	}

	internal abstract bool CurrentFrameIsRunning();
}

//public class A : ExecNodeBase
//{
//	public A(string name = "A") : base(name)
//	{

//	}


//	internal override void OnRegister(ExecManager execManager)
//	{
//		this._execCriteria._updateAfterNodeNames.Add("B");
//	}

//	internal override void OnUnregister(ExecManager execManager)
//	{
//	}

//	internal override void Update(ExecState state)
//	{
//		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})  FPS={state._avgFps} ({state._minFps}/{state._maxFps})");
//	}
//}

//public class B : ExecNodeBase
//{
//	public B(string name = "B") : base(name)
//	{

//	}


//	internal override void OnRegister(ExecManager execManager)
//	{
//		this._execCriteria._updateAfterNodeNames.Add("C");
//	}

//	internal override void OnUnregister(ExecManager execManager)
//	{
//	}

//	internal override void Update(ExecState state)
//	{
//		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})");
//	}
//}

//public class C : ExecNodeBase
//{
//	public C(string name = "C") : base(name)
//	{

//	}


//	internal override void OnRegister(ExecManager execManager)
//	{
//	}

//	internal override void OnUnregister(ExecManager execManager)
//	{
//	}

//	internal override void Update(ExecState state)
//	{
//		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})");
//	}
//}
