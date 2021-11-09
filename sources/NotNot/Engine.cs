// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Ecs;
using NotNot.Rendering;
using NotNot.SimPipeline;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotNot;

public class Engine : DisposeGuard
{
	private SimManager _simManager;

	public RootNode RootNode { get => _simManager.root; }

	public Phase0_StateSync StateSync { get; set; } = new() { Name = "!!_StateSync" };

	public World DefaultWorld { get; set; } = new() { Name = "!!_DefaultWorld" };

	public ContainerNode Rendering { get; } = new() { Name = "!!_Rendering", _updateAfter = { "!!_StateSync" } };
	public ContainerNode Worlds { get; } = new() { Name = "!!_Worlds", _updateAfter = { "!!_Rendering" } };

	public IUpdatePump Updater = new HeadlessUpdater();

	public Task RunningTask { get => Updater.MainLoop; }

	public void Initialize()
	{
		__ERROR.Throw(Updater != null, "you must set the Updater property before calling Initialize()");

		_simManager = new(this);

		RootNode.AddChild(StateSync);
		RootNode.AddChild(Rendering);
		RootNode.AddChild(Worlds);

		if (DefaultWorld != null)
		{
			DefaultWorld.Initialize();
			Worlds.AddChild(DefaultWorld);
		}

		Updater.OnUpdate += OnUpdate;

	}

	protected async ValueTask OnUpdate(TimeSpan elapsed)
	{
		await _simManager.Update(elapsed);

		//return ValueTask.CompletedTask;
	}



	protected override void OnDispose()
	{
		Updater.Dispose();
		Updater = null;
		_simManager.Dispose();
		__ERROR.Throw(DefaultWorld.IsDisposed == true, "disposing simManager should have disposed all nodes inside");
		DefaultWorld = null;
		_simManager = null;

		base.OnDispose();
	}

}

public interface IUpdatePump : IDisposable
{
	public event Func<TimeSpan, ValueTask> OnUpdate;
	public Task Stop();
	public void Start();
	public Task MainLoop { get; }
	public bool ShouldStop { get; set; }




}

public class HeadlessUpdater : DisposeGuard, IUpdatePump
{
	//System.Threading.SemaphoreSlim runningLock = new(1);


	public event Func<TimeSpan, ValueTask> OnUpdate;


	public Task MainLoop { get; private set; }


	public void Start()
	{
		MainLoop = _MainLoop();

		async Task _MainLoop()
		{
			var start = Stopwatch.GetTimestamp();
			long lastElapsed = 0;

			var loop = 0;
			//await runningLock.WaitAsync();
			while (ShouldStop == false)
			{
				loop++;
				lastElapsed = Stopwatch.GetTimestamp() - start;
				start = Stopwatch.GetTimestamp();
				//Console.WriteLine($" ======================== {loop} ({Math.Round(TimeSpan.FromTicks(lastElapsed).TotalMilliseconds,1)}ms) ============================================== ");
				await OnUpdate(TimeSpan.FromTicks(lastElapsed));
				//Console.WriteLine($"last Elapsed = {lastElapsed}");
				//runningLock.Release();
				//await runningLock.WaitAsync();
			}
			//runningLock.Release();
		}
	}
	public bool ShouldStop { get; set; }


	public async Task Stop()
	{
		ShouldStop = true;
		await MainLoop;
		//await runningLock.WaitAsync();
		//IsRunning= false;
		//runningLock.Release();
	}
}



/// <summary>
/// This system runs at the very start of every frame, in exclusive mode (no other systems running yet).   
/// </summary>
public class Phase0_StateSync : SystemBase
{

	/// <summary>
	/// the current simulation frame writes packets here
	/// </summary>
	private ConcurrentQueue<IRenderPacketNew> _renderPackets = new();
	/// <summary>
	/// render packets for frame n-1.  these are ready to be picked up (swapped out) by the rendering system
	/// </summary>
	private ConcurrentQueue<IRenderPacketNew> _renderPacketsPrior = new();


	private HashSet<IRenderPacketNew> _CHECKED_renderPackets = new();

	public void EnqueueRenderPacket(IRenderPacketNew renderPacket)
	{
		var tempCheck = _renderPackets;
		//__DEBUG.Throw(_updateLock.CurrentCount != 0, "update occuring.  no other systems should be enqueing");
		__CHECKED.Throw(_CHECKED_renderPackets.Add(renderPacket), "the same render packet is already added.  why?");
		_renderPackets.Enqueue(renderPacket);
		__DEBUG.Throw(tempCheck == _renderPackets, "all simPipeline systems should run after the phase0SyncState.  something is serious wrong if this occurs!");

	}

	/// <summary>
	/// For use by rendering system.   obtain last frame's render packets by swapping out the queue with another blank one.
	/// </summary>
	public async ValueTask<ConcurrentQueue<IRenderPacketNew>> RenderPacketsSwapPrior_New(ConcurrentQueue<IRenderPacketNew> finishedPackets)
	{
		//__DEBUG.Throw(toReturn.Count == 0, "should be empty");
		__DEBUG.Throw(_updateLock.CurrentCount != 0, "update occuring.  no other systems should be swapping/doing work");

		//await _updateLock.WaitAsync();
		__DEBUG.WriteLine(_DEBUG_PRINT_TRACE != true, " ----- RENDER -----> RenderPacketsSwapPrior");
		__DEBUG.Throw(finishedPackets != _renderPackets && finishedPackets != _renderPacketsPrior);
		__DEBUG.Throw(_DEBUG_lastRenderPacketsPriorReturned != _renderPacketsPrior, "render thread is grabbing render packets twice in a row without the StateSync.OnUpdate() swapping out a fresh package.  that should not happen!");


		finishedPackets.Clear();
		var toReturn = Interlocked.Exchange(ref _renderPacketsPrior, finishedPackets);

		__DEBUG.Throw(finishedPackets.Count == 0);
		//var toReturn = _renderPacketsPrior;
		//finishedPackets.Clear();
		//_renderPacketsPrior = finishedPackets;
		//_DEBUG_lastRenderPacketsPriorReturned = finishedPackets;

		//_updateLock.Release();
		return toReturn;
	}
	private ConcurrentQueue<IRenderPacketNew> _DEBUG_lastRenderPacketsPriorReturned;



	private SemaphoreSlim _updateLock = new SemaphoreSlim(1, 1);

	protected override async Task OnUpdate(Frame frame)
	{
		await _updateLock.WaitAsync();
		var currentCount = _renderPackets.Count;

		//clear and swap
		_renderPacketsPrior.Clear(); //clear in case rendering is running slower than sim
		var temp = _renderPacketsPrior;
		_renderPacketsPrior = _renderPackets;
		_renderPackets = temp;
		_DEBUG_lastRenderPacketsPriorReturned = null;

#if CHECKED
		_CHECKED_renderPackets.Clear();
#endif
		__DEBUG.Throw(_renderPacketsPrior.Count == currentCount, "race condition.  something writing to render packets during swap.  ensure the node.updateAfter is properly set");
		_updateLock.Release();
	}
}
