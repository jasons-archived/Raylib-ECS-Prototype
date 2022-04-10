// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using Nito.AsyncEx;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Bcl.Threading;
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
using NotNot.Bcl.Threading.Advanced;

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

	public bool singleThreaded = false;
	public event Func<TimeSpan, ValueTask> OnUpdate;


	public Task MainLoop { get; private set; }


	public void Start()
	{
		if (singleThreaded)
		{
			DebuggableTaskFactory.singleThreaded = true;
		}
		MainLoop = DebuggableTaskFactory.Run(_MainLoop);


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


	public Task Stop()
	{
		ShouldStop = true;
		return MainLoop;
		//await runningLock.WaitAsync();
		//IsRunning= false;
		//runningLock.Release();
	}

	protected override void OnDispose()
	{
		ShouldStop = true;
		base.OnDispose();
	}
}



/// <summary>
/// This system runs at the very start of every frame, in exclusive mode (no other systems running yet).   
/// </summary>
public class Phase0_StateSync : SystemBase
{

	//public FrameDataChannel<IRenderPacketNew> renderPackets = new(1);


	public FrameDataChannelSlim<RenderFrame> renderChannel = new(1);

	protected override void OnInitialize()
	{
		AddField(renderChannel);

		base.OnInitialize();

	}

	protected override async Task OnUpdate(Frame frame)
	{
		var i = 0;
		i++;

		//renderPackets.EndFrameAndEnqueue();

		//__DEBUG.Assert(renderPackets.CurrentFramePacketDataCount == 0,
		//	"should not race with main thread to write packets");
		await base.OnUpdate(frame);
		//return Task.CompletedTask;
	}
}
