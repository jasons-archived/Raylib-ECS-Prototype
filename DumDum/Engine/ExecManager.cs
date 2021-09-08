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
	private List<IExecNode> _nodes = new();
	public IEnumerable<IExecNode> Nodes => _nodes;


	/// <summary>
	/// registers your node for execution in the proper order
	/// </summary>
	/// <param name="node"></param>
	public void Register(IExecNode node)
	{
		_nodes.Add(node);
		node.OnAdd(this);
	}

	public void Unregister(IExecNode node)
	{
		var result = _nodes.Remove(node);
		__DEBUG.Assert(result);
	}

	public void BeginRun()
	{


	}

	private HashSet<IExecNode> _tempFinished = new();
	private HashSet<IExecNode> _tempToUpdateThisTick = new();
	private List<IExecNode> _tempAboutToUpdate = new();
	private HashSet<IExecNode> _tempNowUpdating = new();
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
				if (!node.UpdateAfter.Any() || _tempFinished.IsSupersetOf(node.UpdateAfter))
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
				node.ExecUpdate(_execState);
				_tempNowUpdating.Remove(node);


				//mark it as finished
				_tempFinished.Add(node);
				loopExecCount++;
			}


			__DEBUG.Assert(_tempAboutToUpdate.Count == 0, "above should have cleared as part of algo");
			__DEBUG.Throw(loopExecCount > 0, "at least one node should have executed, or we will be stuck in an infinite loop");
		}

		this._tempToUpdateThisTick.Clear();
		_tempFinished.Clear();
	}



}


public abstract class SystemBase : IExecNode
{
	IEnumerable<IExecNode> IExecNode.UpdateAfter => _updateAfter;
	//private ExecManager _execManager;

	protected List<SystemBase> _updateAfter = new();

	void IExecNode.OnAdd(ExecManager execManager)
	{
		this.OnAdd(execManager);
	}

	protected abstract void OnAdd(ExecManager execManager);

	void IExecNode.ExecUpdate(ExecState state)
	{
		this.Update(state);
	}

	protected abstract void Update(ExecState state);

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