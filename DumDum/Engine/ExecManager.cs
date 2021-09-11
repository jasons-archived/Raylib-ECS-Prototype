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
	public TimeSpan _elapsedSinceLastUpdate = TimeSpan.Zero;

	private ExecManager _execManager;



	internal override void OnRegister(ExecManager execManager)
	{
		_execManager = execManager;
	}

	internal override void OnUnregister(ExecManager execManager)
	{
		_execManager = null;
	}

	internal override void Update(ExecState state)
	{
		//determine if we should execute
		_elapsedSinceLastUpdate.Add(state._frameElapsed);
		if (_elapsedSinceLastUpdate < _targetUpdateInterval)
		{
			//not yet time
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
		foreach (var childNode in _children)
		{
			_execManager._ScheduleNodeForThisFrame(childNode);
		}
	}
}



/// <summary>
/// just a singlethreaded, simple execution manager
/// </summary>
public class ExecManager
{
	private List<ExecNodeBase> _nodes = new();
	public IEnumerable<ExecNodeBase> Nodes => _nodes;

	private HashSet<string> _registeredNodeNames = new();

	/// <summary>
	/// registers your node for execution in the proper order
	/// </summary>
	/// <param name="node"></param>
	public void Register(ExecNodeBase node)
	{
		__ERROR.Throw(_registeredNodeNames.Add(node.Name), $"Another node with name '{node.Name}' has already been added");
		_nodes.Add(node);
		node.OnRegister(this);
	}

	public void Unregister(ExecNodeBase node)
	{
		__ERROR.Throw(_registeredNodeNames.Remove(node.Name), $"Node with name '{node.Name}' was not found to be removed");
		var result = _nodes.Remove(node);
		__DEBUG.Assert(result);

		node.OnUnregister(this);
	}

	public void BeginRun()
	{

	}

	private HashSet<ExecNodeBase> _tempFinishedNodes = new();
	private HashSet<ExecNodeBase> _tempToUpdateThisTick = new();
	/// <summary>
	/// helper to track what nodes are ready for updating.  removed as soon as they are updated.
	/// </summary>
	private List<ExecNodeBase> _tempUnblockedAndReadyForUpdate = new();
	/// <summary>
	/// helper to track what nodes we have already marked as "ready to execute" this tick.
	/// </summary>
	private HashSet<ExecNodeBase> _tempUnblockedAndReadyForUpdate_Lookup = new();
	private HashSet<ExecNodeBase> _tempNowUpdating = new();
	private ExecState _execState = new();
	private Random _rand = new();
	public void Update(TimeSpan elapsed)
	{
		//algo:  loop through all our nodes, executing those not blocked by anything (either no blocks or blocks are finished), and moving those into the _tempFinished set.


		__DEBUG.Assert(
			_tempUnblockedAndReadyForUpdate.Count == 0
			&& _tempToUpdateThisTick.Count == 0
			&& _tempFinishedNodes.Count == 0
			&& _tempNowUpdating.Count == 0
			, "algo error: below should clear these before exiting fcn"
		);

		foreach (var item in _nodes)
		{
			_tempToUpdateThisTick.Add(item);
		}
		//_tempToUpdateThisTick.UnionWith(_nodes);

		_execState.Update(elapsed);


#if CHECKED

		foreach (var node in _nodes)
		{
			foreach (var updateAfterName in node._execCriteria._updateAfterNodeNames)
			{
				__CHECKED.AssertOnce(_registeredNodeNames.Contains(updateAfterName), $"The node {node.Name} is specified to update after node {updateAfterName} but that node is not registered with the ExecManager.  This dependency will be assumed to be fulfuilled (nothing to wait on)");
			}
		}
#endif


		//TODO:  ensure that all nodes that wait on named nodes actually have nodes of that name added.
		//throw new NotImplementedException();


		//randomize execution order of those nodes that can execute now (good for debugging dependency problems)
		while (this._tempToUpdateThisTick.Count > 0)
		{



			//try to add all unblocked nodes 
			foreach (var currentInspectedPending in this._tempToUpdateThisTick)
			{
				if (_tempUnblockedAndReadyForUpdate_Lookup.Contains(currentInspectedPending))
				{
					//already marked as unblocked so skip checking again
					continue;
				}

				var isBlocked = false;
				//if the current node has no blocks or all its blocks have already finished
				foreach (var otherPending in _tempToUpdateThisTick)
				{
					if (currentInspectedPending._execCriteria.IsUpdateAfterBlockedBy(otherPending))
					{
						isBlocked = true;
						break;
					}
				}

				if (isBlocked)
				{
					continue;
				}
				//not blocked
				_tempUnblockedAndReadyForUpdate_Lookup.Add(currentInspectedPending);
				_tempUnblockedAndReadyForUpdate.Add(currentInspectedPending);

				//if (!node._updateAfter.Any() || _tempFinished.IsSupersetOf(node._updateAfter))
				//{
				//	if (_tempUnblockedAndReadyForUpdate_Lookup.Add(node))
				//	{
				//		_tempUnblockedAndReadyForUpdate.Add(node);
				//	}
				//}
			}

			var loopExecCount = 0;

			//NOTE: this was a WHILE loop.
			//changing to an IF so that we can maximize randomness of node execution order.
			// Why this change to IF allows it:  only execute one node randomly from all "_tempUnblockedAndReadyForUpdate" choices,
			//then go back to the above WHILE to add any more just-unblocked nodes to our choices.
			if (_tempUnblockedAndReadyForUpdate.Count > 0)
			{
				//get a random node
				var index = _rand.Next(0, _tempUnblockedAndReadyForUpdate.Count);
				var node = _tempUnblockedAndReadyForUpdate[index];
				_tempUnblockedAndReadyForUpdate.RemoveAt(index);
				this._tempToUpdateThisTick.Remove(node);

				//execute it
				_tempNowUpdating.Add(node);
				node.Update(_execState);
				_tempNowUpdating.Remove(node);


				//mark it as finished
				var result = _tempFinishedNodes.Add(node);
				__DEBUG.Assert(result);
				loopExecCount++;
			}




			__DEBUG.Throw(loopExecCount > 0, $"No nodes executed.  We are in a deadlock state.  There are {_tempToUpdateThisTick.Count} nodes still remaining.  " +
											 $"At least one node should have executed per WHILE loop, or we will be stuck in an infinite loop." +
											 $" Possible cause is a node that has an invalid .UpdateAfter() dependency");
		}

		__DEBUG.Assert(_tempUnblockedAndReadyForUpdate.Count == 0, "above should have cleared as part of algo");
		_tempUnblockedAndReadyForUpdate_Lookup.Clear();
		this._tempToUpdateThisTick.Clear();
		_tempFinishedNodes.Clear();
	}

	internal void _ScheduleNodeForThisFrame(ExecNodeBase childNode)
	{
		throw new NotImplementedException();
	}
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
}


public abstract class ExecNodeBase
{
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
	internal abstract void OnRegister(ExecManager execManager);

	/// <summary>
	/// triggered when detached
	/// </summary>
	/// <param name="execManager"></param>
	internal abstract void OnUnregister(ExecManager execManager);


	internal abstract void Update(ExecState state);

}

public class A : ExecNodeBase
{
	public A(string name = "A") : base(name)
	{

	}


	internal override void OnRegister(ExecManager execManager)
	{
		this._execCriteria._updateAfterNodeNames.Add("B");
	}

	internal override void OnUnregister(ExecManager execManager)
	{
	}

	internal override void Update(ExecState state)
	{
		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})  FPS={state._avgFps} ({state._minFps}/{state._maxFps})");
	}
}

public class B : ExecNodeBase
{
	public B(string name = "B") : base(name)
	{

	}


	internal override void OnRegister(ExecManager execManager)
	{
		this._execCriteria._updateAfterNodeNames.Add("C");
	}

	internal override void OnUnregister(ExecManager execManager)
	{
	}

	internal override void Update(ExecState state)
	{
		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})");
	}
}

public class C : ExecNodeBase
{
	public C(string name = "C") : base(name)
	{

	}


	internal override void OnRegister(ExecManager execManager)
	{
	}

	internal override void OnUnregister(ExecManager execManager)
	{
	}

	internal override void Update(ExecState state)
	{
		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})");
	}
}
