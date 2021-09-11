using DumDum.Bcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DumDum.Bcl.Diagnostics;
using DumDum.Engine._internal;

namespace DumDum.Engine;



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

	private HashSet<string> _tempFinished = new();
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
			&& _tempFinished.Count == 0
			&& _tempNowUpdating.Count == 0
			, "algo error: below should clear these before exiting fcn"
		);

		foreach (var item in _nodes)
		{
			_tempToUpdateThisTick.Add(item);
		}
		//_tempToUpdateThisTick.UnionWith(_nodes);

		_execState.Update(elapsed);



		//randomize execution order of those nodes that can execute now (good for debugging dependency problems)
		while (this._tempToUpdateThisTick.Count > 0)
		{

			foreach (var node in this._tempToUpdateThisTick)
			{
				//if the current node has no blocks or all its blocks have already finished
				if (!node._updateAfter.Any() || _tempFinished.IsSupersetOf(node._updateAfter))
				{
					if (_tempUnblockedAndReadyForUpdate_Lookup.Add(node))
					{
						_tempUnblockedAndReadyForUpdate.Add(node);
					}
				}
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
				_tempFinished.Add(node.Name);
				loopExecCount++;
			}


			

			__DEBUG.Throw(loopExecCount > 0, $"No nodes executed.  We are in a deadlock state.  There are {_tempToUpdateThisTick.Count} nodes still remaining.  " +
			                                 $"At least one node should have executed per WHILE loop, or we will be stuck in an infinite loop." +
			                                 $" Possible cause is a node that has an invalid .UpdateAfter() dependency");
		}

		__DEBUG.Assert(_tempUnblockedAndReadyForUpdate.Count == 0, "above should have cleared as part of algo");
		_tempUnblockedAndReadyForUpdate_Lookup.Clear();
		this._tempToUpdateThisTick.Clear();
		_tempFinished.Clear();
	}



}



public abstract class ExecNodeBase
{
	private static HashSet<string> _names = new();
//	private string _name;

	public string Name
	{
		get;init;
	}

	public ExecNodeBase(string name)
	{
		Name = name;
		lock (_names)
		{
			_names.Add(Name);
		}
	}
	~ExecNodeBase()
	{
		lock (_names)
		{
			_names.Remove(Name);
		}
	}

	/// <summary>
	/// names of nodes this will update after
	/// </summary>
	internal List<string> _updateAfter = new();
	protected void UpdateAfter(string nodeName)
	{
		_updateAfter.Add(nodeName);
	}
	protected void UpdateAfter(ExecNodeBase node)
	{
		_updateAfter.Add(node.Name);
	}

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
		this.UpdateAfter("B");
	}

	internal override void OnUnregister(ExecManager execManager)
	{
	}

	internal override void Update(ExecState state)
	{
		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})");
	}
}

public class B : ExecNodeBase
{
	public B(string name = "B") : base(name)
	{

	}


	internal override void OnRegister(ExecManager execManager)
	{
		this.UpdateAfter("C");
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
		//this.UpdateAfter("C");
	}

	internal override void OnUnregister(ExecManager execManager)
	{
	}

	internal override void Update(ExecState state)
	{
		Console.WriteLine($"{Name} @{state._totalFrames} ({state._totalElapsed})");
	}
}
