using System;
using System.Collections.Generic;
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

}
public partial class SimManager //thread execution
{

	private TimeStats _stats;
	private Task _priorFrameTask;
	private Frame _frame;
	public async void Update(TimeSpan elapsed)
	{
		_stats.Update(elapsed);
		_frame = Frame.FromPool(_stats, _frame);


		//notify all nodes of our intent to execute a frame, and obtain a flat listing of all nodes
		var allNodesInFrame = new List<SimNode>();
		_root.FrameUpdateHiearchyStart(_frame, allNodesInFrame);

		//sort so at bottom are those that should be executed at highest priority
		allNodesInFrame.Sort();
		var allNodesToProcess = new List<SimNode>(allNodesInFrame);

		var maxThreads = Environment.ProcessorCount + 2;
		var currentTasks = new List<Task>();

		while (allNodesToProcess.Count > 0)
		{
			var startedThisPass = 0;

			//try to execute highest priority first
			for (var i = allNodesToProcess.Count - 1; i >= 0; i--)
			{
				var currentNode = allNodesToProcess[i];
				if (currentNode.FrameCanUpdateNow(_frame))
				{
					allNodesToProcess.RemoveAt(i);
					var updateTask = currentNode.FrameUpdateMain(_frame);
					currentTasks.Add(updateTask);
					startedThisPass++;
				}

				if (currentTasks.Count >= maxThreads)
				{
					//too many enqueued, stop trying to enqueue more
					break;
				}
			}

			//wait on at least one task
			await Task.WhenAny(currentTasks);
			//remove done
			for (var i = currentTasks.Count - 1; i >= 0; i--)
			{
				if (currentTasks[i].IsCompleted)
				{
					currentTasks.RemoveAt(i);
				}
			}

			__DEBUG.Assert(startedThisPass > 0 || currentTasks.Count > 0, "deadlock?");
		}

		//nothing else to process, wait on all remaining tasks
		await Task.WhenAll(currentTasks);









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
	/// <summary>
	/// stores per-frame state.  right now just a Task showing status of the Update() method (UpdateTask).
	/// </summary>
	private Dictionary<Frame, FrameState> _frameStates = new();

	internal void FrameUpdateHiearchyStart(Frame frame, ICollection<SimNode> allNodesToUpdateInFrame)
	{
		//TODO:  store last updateTime metric, use this+children to estimate node priority (costly things run first!)  add out TimeSpan hiearchyLastElapsed
		//TODO:  when doing that, maybe take the average of executions.   store in this._executionPriority
		allNodesToUpdateInFrame.Add(this);
		var frameState = new FrameState() { status = ExecStatus.SCHEDULED };
		_frameStates.Add(frame, frameState);

		foreach (var child in _children)
		{
			child.FrameUpdateHiearchyStart(frame, allNodesToUpdateInFrame, );
		}
		//TODO: node filtering logic here based on FPS limiting, etc


	}

	internal Task FrameUpdateMain(Frame frame)
	{
		//do own update
		var frameState = _frameStates[frame];

		frameState.status = ExecStatus.RUNNING;
		var updateTask = Update(frame).ContinueWith((task) =>
		{
			frameState.status = ExecStatus.FINISHED;
			frameState.updateTcs.SetFromTask(task);
		});
		return updateTask;
	}


	/// <summary>
	/// //IMPORTANT: a node.Update() will fire and complete BEFORE it's children are executed. 
	/// </summary>
	/// <param name="frame"></param>
	/// <returns></returns>
	public abstract Task Update(Frame frame);


	/// <summary>
	/// allows await of this and all children
	/// </summary>
	/// <param name="frame"></param>
	/// <returns></returns>
	internal async Task FrameUpdateHiearchyFinished(Frame frame)
	{
		await _frameStates[frame].UpdateTask;

		__DEBUG.Assert(_frameStates[frame].UpdateTask.IsCompletedSuccessfully);

		//unblock when self and all children are complete
		foreach (var child in _children)
		{
			await child.FrameUpdateHiearchyFinished(frame);
		}
		kjhkhlkhj //remove above FrameUpdateHiearchyFinished() function.  instead have a per-frame (framestate) counter for children remaining
		//that is allocated during the FrameUpdateHiearchyStart() method.  and decremented (down hiearchy) on every Update() complete.
		//when completed, logic in the FrameState should unblock a CountdownLockSlim
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

	public bool FrameCanUpdateNow(Frame frame)
	{
		throw new NotImplementedException();
	}
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

public class Frame
{
	public TimeStats _stats;


	///// <summary>
	///// key should not update yet, because it's being stopped by value, which wants to complete first.
	///// <para>records added by VALUE</para>
	///// </summary>
	//public Dictionary<string, Node> _currentFrameUpdateBeforeBlocks = new();
	///// <summary>
	///// key should not update yet, becasue it wants to wait until VALUE has completed.
	///// <para>records added by KEY</para>
	///// </summary>
	//public Dictionary<Node, string> _currentFrameUpdateAfterBlocks = new();

	internal static Frame FromPool(TimeStats stats, Frame priorFrame)
	{
		//make chain, recycle old stuff every update
		throw new NotImplementedException();
		throw new NotImplementedException();
	}

	//public Dictionary<SimNode, (ExecStatus status, int childBlocks)> nodeState = new();
}

public record class FrameState
{
	public TaskCompletionSource updateTcs { get; init; }
	public ExecStatus status { get; set; }
	public Task UpdateTask
	{
		get => updateTcs.Task;
	}

}