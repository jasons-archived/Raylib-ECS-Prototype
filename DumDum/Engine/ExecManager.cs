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
	private List<ExecNodeBase> _tempAboutToUpdate = new();
	private HashSet<ExecNodeBase> _tempNowUpdating = new();
	private ExecState _execState = new();
	private Random _rand = new();
	public void Update(TimeSpan elapsed)
	{
		//algo:  loop through all our nodes, executing those not blocked by anything (either no blocks or blocks are finished), and moving those into the _tempFinished set.


		__DEBUG.Assert(
			_tempAboutToUpdate.Count == 0
			&& _tempToUpdateThisTick.Count == 0
			&& _tempAboutToUpdate.Count == 0
			&& _tempNowUpdating.Count == 0
			&& _tempNowUpdating.Count == 0
			, "algo error: below should clear these before exiting fcn"
		);

		_execState.Update(elapsed);

		this._tempToUpdateThisTick.UnionWith(_nodes);

		//__DEBUG.WriteLine(gameTime.FrameCount.ToString());


		while (this._tempToUpdateThisTick.Count > 0)
		{

			foreach (var node in this._tempToUpdateThisTick)
			{
				//if the current node has no blocks or all its blocks have already finished
				if (!node._updateAfter.Any() || _tempFinished.IsSupersetOf(node._updateAfter))
				{
					_tempAboutToUpdate.Add(node);
				}
			}

			var loopExecCount = 0;

			//randomize execution order of those nodes that can execute now (good for debugging dependency problems)
			while (_tempAboutToUpdate.Count > 0)
			{
				//get a random node
				var index = _rand.Next(0, _tempAboutToUpdate.Count);
				var node = _tempAboutToUpdate[index];
				_tempAboutToUpdate.RemoveAt(index);
				this._tempToUpdateThisTick.Remove(node);

				//execute it
				_tempNowUpdating.Add(node);
				node.Update(_execState);
				_tempNowUpdating.Remove(node);


				//mark it as finished
				_tempFinished.Add(node.Name);
				loopExecCount++;
			}


			__DEBUG.Assert(_tempAboutToUpdate.Count == 0, "above should have cleared as part of algo");
			__DEBUG.Throw(loopExecCount > 0, "at least one node should have executed, or we will be stuck in an infinite loop");
		}

		this._tempToUpdateThisTick.Clear();
		_tempFinished.Clear();
	}



}



public abstract class ExecNodeBase
{
	public string Name{ get; init; }
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

//public class A : SystemBase
//{
//	protected override void OnAdd(ExecManager execManager)
//	{
//		this._updateAfter.Add(typeof(B))
//	}
//}
//public class B : SystemBase
//{
//	protected override void OnAdd(ExecManager execManager)
//	{
//		this._updateAfter.Add()
//	}
//}
//public class A : SystemBase
//{
//	protected override void OnAdd(ExecManager execManager)
//	{
//		this._updateAfter.Add()
//	}
//}