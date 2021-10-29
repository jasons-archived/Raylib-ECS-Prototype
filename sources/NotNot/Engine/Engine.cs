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
using NotNot.Engine.Ecs;
using NotNot.Engine.Internal.SimPipeline;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotNot.Engine;

public class Engine : DisposeGuard
{
	private SimManager _simManager;

	public RootNode RootNode { get => _simManager.root; }

	public Phase0StateSync StateSync { get; set; } = new() { Name = "!!_StateSync" };

	public World DefaultWorld { get; set; } = new() { Name= "!!_DefaultWorld" };

	public ContainerNode Rendering { get; } = new() { Name = "!!_Rendering", _updateAfter = { "!!_StateSync" } };
	public ContainerNode Worlds { get; } = new() { Name = "!!_Worlds", _updateAfter = { "!!_StateSync" } };

	public IUpdatePump Updater;

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
		__ERROR.Throw(DefaultWorld.IsDisposed == true,"disposing simManager should have disposed all nodes inside");
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
			while (ShouldStop==false)
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
		ShouldStop=true;
		await MainLoop;
		//await runningLock.WaitAsync();
		//IsRunning= false;
		//runningLock.Release();
	}
}



public class Phase0StateSync : SystemBase
{

	/// <summary>
	/// the current simulation frame writes packets here
	/// </summary>
	private ConcurrentQueue<IRenderPacket> _renderPackets = new();
	/// <summary>
	/// render packets for frame n-1.  these are ready to be picked up (swapped out) by the rendering system
	/// </summary>
	private ConcurrentQueue<IRenderPacket> _renderPacketsPrior = new();


	private HashSet<IRenderPacket> _CHECKED_renderPackets = new();

	public void EnqueueRenderPacket(IRenderPacket renderPacket)
	{
		__DEBUG.Throw(_updateLock.CurrentCount == 0, "update occuring.  no other systems should be enqueing");
		__CHECKED.Throw(_CHECKED_renderPackets.Add(renderPacket), "the same render packet is already added.  why?");
		_renderPackets.Enqueue(renderPacket);
	}

	/// <summary>
	/// For use by rendering system.   obtain last frame's render packets by swapping out the queue with another blank one.
	/// </summary>
	public void RenderPacketsSwapPrior(ConcurrentQueue<IRenderPacket> toReturn, out ConcurrentQueue<IRenderPacket> prior)
	{
		__DEBUG.Throw(toReturn.Count == 0, "should be empty");
		__DEBUG.Throw(_updateLock.CurrentCount == 0, "update occuring.  no other systems should be swapping/doing work");
		prior = _renderPacketsPrior;
		_renderPacketsPrior = toReturn;
	}



	private SemaphoreSlim _updateLock = new SemaphoreSlim(1, 1);

	protected override async Task OnUpdate(Frame frame)
	{
		var currentCount = _renderPackets.Count;

		await _updateLock.WaitAsync();

		//clear and swap
		_renderPacketsPrior.Clear();
		var temp = _renderPacketsPrior;
		_renderPacketsPrior = _renderPackets;
		_renderPackets = temp;

#if CHECKED
		_CHECKED_renderPackets.Clear();
#endif
		_updateLock.Release();
		__DEBUG.Throw(_renderPacketsPrior.Count == currentCount, "race condition.  something writing to render packets during swap");
	}
}

public interface IRenderPacket : IComparable<IRenderPacket>, IEnumerable<IRenderPacket>
{
	/// <summary>
	/// lower numbers get rendered first
	/// </summary>
	public int RenderLayer { get; }
	public void DoDraw();
}
