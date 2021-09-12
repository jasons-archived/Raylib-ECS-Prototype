using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DumDum.Bcl.Diagnostics;

namespace DumDum.Engine.Ecs;


public class SimManager
{
	public Dictionary<string, SimNode> _nodeRegistry = new();
	public RootNode _root;

	SimManager()
	{
		_root = new RootNode { Name = "root", _manager = this };
	}

	public void Register(SimNode node)
	{
		SimNode parent;
		lock (_nodeRegistry)
		{
			__ERROR.Throw(_nodeRegistry.TryAdd(node.Name, node));
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

	private TimeStats _stats;
	private Task _priorFrameTask;
	private Frame _frame;
	public void Update(TimeSpan elapsed)
	{
		_stats.Update(elapsed);
		_frame = Frame.FromPool(_stats,_frame);

		_root.OnFrameInitialize(_frame);

		var task = _root.StartUpdate(_frame);

		//let prior frame run late, as inter-frame coordination is taken care of in the frame object
		if (_priorFrameTask != null)
		{
			while (_priorFrameTask.IsCompleted == false)
			{
				_priorFrameTask.Wait(TimeSpan.FromMilliseconds(10));
			}
			_priorFrameTask.Dispose();
		}
		_priorFrameTask = task;

		

	}


}

public class RootNode : SimNode
{

}

public abstract partial class SimNode
{
	public string Name { get; init; }
	public string ParentName { get; init; }
	public SimNode _parent;
	public SimManager _manager;

	public UpdateCriteria _updateCriteria = new();

	public List<SimNode> _children = new();

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
		_manager =null;
	}

	internal void OnFrameInitialize(Frame frame)
	{
		_updateCriteria.OnFrameInitialize(frame);
		foreach (var child in _children)
		{
			child.OnFrameInitialize(frame);
		}

		if (_updateCriteria.ShouldSkipFrame(frame))
		{
			SkipFrame(frame);
		}
	}

}

public partial class SimNode //update logic
{

	/// <summary>
	/// starts performing an update.
	/// <para>this frame delays start until <see cref="_updateCriteria"/> says not blocked</para>
	/// is considered done when itself and it's children finish.
	/// </summary>
	/// <param name="frame"></param>
	/// <returns></returns>
	internal async Task StartUpdate(Frame frame)
	{
	}

	private void SkipFrame(Frame frame)
	{
		foreach (var child in _children)
		{
			child.SkipFrame(frame);
		}
		//move this frame's dependencies to complete
		throw new NotImplementedException();

	}
}


public class UpdateCriteria
{
	public List<string> _updateBefore = new();
	public List<string> _updateAfter = new();

	public SimNode _owner{ get; init; }

	public void OnFrameInitialize(Frame frame)
	{asdfasdfasdfasdfa  
			//TODO: start here with constructing the blocking criterias
		//populate frame with blocking criteria
		throw new NotImplementedException();
	}

	public bool IsBlocked(Frame frame)
	{
		throw new NotImplementedException();
	}

	internal bool ShouldSkipFrame(Frame frame)
	{
		throw new NotImplementedException();
	}
}

public class Frame
{
	public TimeStats _stats;
	//public SimNode _root;

	public Dictionary<SimNode, ExecStatus> _nodeStatus = new();



	/// <summary>
	/// key should not update yet, because it's being stopped by value, which wants to complete first.
	/// <para>records added by VALUE</para>
	/// </summary>
	public Dictionary<string, Node> _currentFrameUpdateBeforeBlocks = new();
	/// <summary>
	/// key should not update yet, becasue it wants to wait until VALUE has completed.
	/// <para>records added by KEY</para>
	/// </summary>
	public Dictionary<Node, string> _currentFrameUpdateAfterBlocks = new();

	internal static Frame FromPool(TimeStats stats, Frame priorFrame)
	{
		//make chain, recycle old stuff every update
		throw new NotImplementedException();
		throw new NotImplementedException();
	}
}

