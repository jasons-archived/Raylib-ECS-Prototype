using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Nito.AsyncEx;
using NotNot.Bcl.Diagnostics;
using NotNot.Bcl.Threading;
using NotNot.Ecs;
using NotNot.SimPipeline;
using Raylib_CsLo;

namespace NotNot.Rendering;

/// <summary>
/// An example Rendering System you should use for reference when building a better rendering system.
/// <para>This Rendering System utilizes RayLib, which is a great "toy renderer".
/// Raylib is very easy to use but is not designed for the high demands of a 3d game engine.
/// </para>
/// <para>This Reference Renderer shows how to coordinate the rendering work with the rest of the engine, especially showing how the engine/game can schedule
/// "RenderPackets" to be drawn by the render loop thread.
/// </para>
/// <para>Raylib was chosen because it's the easiest for jason to get working, without needing to know a lot of low-level rendering knowledge.</para>
/// </summary>
public class RenderReferenceImplementationSystem : SystemBase
{
	System.Drawing.Size screenSize = new(1920, 1080);
	string windowTitle;

	public static Camera3D camera = new()
	{
		position = new Vector3(0.0f, 10.0f, 10.0f), // Camera3D position
		target = new Vector3(0.0f, 0.0f, 0.0f), // Camera3D looking at point
		up = new Vector3(0.0f, 1.0f, 0.0f), // Camera3D up vector (rotation towards target)
		fovy = 45.0f, // Camera3D field-of-view Y
		projection_ = CameraProjection.CAMERA_PERSPECTIVE, // Camera3D mode type
	};

	//private CustomTaskScheduler renderScheduler = new CustomTaskScheduler(1);

	protected override void OnRegister()
	{
		base.OnRegister();



		//If Raylib or any app executes OpenGl instructions from multiple threads, it will crash with an AccessViolationException.
		//This is a problem not only for doing multithreading, but also using async/await.
		//If you use async/await in your render loop, you might be running on a different thread if you ever await, such as await Task.Delay(1);
		//The simplist solution is to start your render task via Nito.AsyncEx.AsyncContext.Run(_RenderThread);
		//which will force the async continuations to run on the same managed thread.
		//alternately creating a custom task scheduler works, but is a lot more effort.  see the edu.md "Threading" section for notes, or the NotNot.Bcl.Threading.CustomTaskScheduler



		//start rendering on it's own thread
		//_renderTask = new Task(_RenderThread, TaskCreationOptions.LongRunning); //runs for entire app duration
		//_renderTask.Start();


		_renderThread = new Nito.AsyncEx.AsyncContextThread();
		_renderTask = _renderThread.Factory.Run(_RenderThread_Worker);




		//////_renderTask = new Task(() => StartRender2());
		//////_renderTask.Start(renderScheduler);

		//////renderScheduler
		//////_renderTask = Task.Factory.StartNew(_RenderThread,CancellationToken.None,TaskCreationOptions.LongRunning,renderScheduler);
		//////new Task(_RenderThread,TaskCreationOptions.LongRunning)

		//////TaskScheduler.con
		//////var x = new Thread()
		//////Task.Factory.
	}

	//private void _RenderThread()
	//{
	//	Nito.AsyncEx.AsyncContext.Run(_RenderThread_Worker);


	//	Console.WriteLine("DONE");
	//}
	//private Task StartRender()
	//{
	//	try
	//	{
	//		return _RenderThread_Worker();
	//	}
	//	catch (Exception ex)
	//	{
	//		Console.WriteLine(ex.Message);
	//	}
	//	finally
	//	{
	//		Console.WriteLine("DONE");
	//	}
	//	return Task.CompletedTask;

	//}
	private Nito.AsyncEx.AsyncContextThread _renderThread;
	private Task _renderTask;

	//private SemaphoreSlim _swapPacketsLock = new(1, 1);

	///// <summary>
	///// packets obtained from the Engine.SyncState  (packets from frame N-1)
	///// <para>this is swapped back-and-forth with <see cref="_packetsPending"/></para>
	///// </summary>
	//private ConcurrentQueue<IRenderPacket> _packetsPending = new();
	///// <summary>
	///// the packets used by the render thread loop   (consumed by each pass of that loop)
	///// <para>this is swapped back-and-forth with <see cref="_packetsPending"/></para>
	///// </summary>
	//private ConcurrentQueue<IRenderPacket> _packetsCurrent = new();

	/// <summary>
	/// sync point that prevents the render thread from running faster than the simulation frame rate
	/// <para>render thread can run slower without impacting performance of the simulation</para>
	/// </summary>
	//private Nito.AsyncEx.AsyncAutoResetEvent _renderLoopAutoResetEvent = new(false);
	private DotNext.Threading.AsyncAutoResetEvent _renderLoopAutoResetEvent = new(false);

	/// <summary>
	/// temp collection used in the render thread, used to sort render packets before drawing
	/// </summary>
	List<IRenderPacketNew> _thread_renderPackets = new();


	/// <summary>
	/// render packets obtained from the Phase0_SyncState system
	/// </summary>
	private ConcurrentQueue<IRenderPacketNew> _tempRenderPackets = new();


	/// <summary>
	/// used to synchronize the critical section that must not be raced by the main simulation's render update task and the render loop
	/// <para>The protected section obtains the frame N-1 render packets and allows the render loop to run once.</para>
	/// </summary>
	/// <remarks>
	/// <para>The original problem was that there was flickering every so often with the packet rendering.
	/// More analysis showed that maybe 1/10000 loops the renderLoopThread did not have any packets to render, causing a blank frame to be shown.
	/// A lazy solution would have been to skip rendering that frame, but that would lead to jitters and so I needed to solve the root cause.
	/// It turns out that I was having system A (rendering loop) synchronizing with system B (rendering system) but reading data from system C (frame start cache of render packets).
	/// This meant that occasionally, the render thread gets behind, then catches up, effectively running twice in a frame.
	/// In more detail:
	/// The frame would start, provide renderPackets(Frame N-1).
	/// The renderLoopThread would aquire those renderPackets(N-1) and render.
	/// The renderSystem would run, unblocking renderLoopThread to run.
	/// That same frame, renderLoopThread would loop again, aquiring renderPackets again.
	/// But since the next frame didn't start, there would be no new renderPackets.  so no work to do, resulting in the blank frame.
	/// </para>
	/// <para> The solution was to have A read and synchronize from B, and B reads from C.
	/// Since RenderSystem is part of the SimPipeline, it's order is gurenteed to run once per frame, after the Phase0StateSync.
	/// RenderSystem reads the renderPackets(N-1), then allows RenderLoopThread to run once.
	/// RenderLoopThread aquires renderPackets(N-1) then blocks itself from running until next RenderSystem update unblocks it.
	/// This last statement (blocks itself) is very important to avoid the race condition that is the original problem.</para>
	/// <para>Basically this is just a long-winded way of me saying it took 3 days to get a cube rendering without flickering</para>
	/// </remarks>
	private SemaphoreSlim _updateSyncCriticalSectionLock = new(1, 1);

	protected override async Task OnUpdate(Frame frame)
	{

		//////await _updateSyncCriticalSectionLock.WaitAsync();
		//////try
		//////{
		//////	_tempRenderPackets = await manager.engine.StateSync.RenderPacketsSwapPrior_New(_tempRenderPackets);
		//////	_renderLoopAutoResetEvent.Set();
		//////	_tempRenderPackets = renderThreadInput.WriteAndSwap(_tempRenderPackets);
		//////}
		//////finally
		//////{
		//////	_updateSyncCriticalSectionLock.Release();
		//////}

		//get our render packets from prior frame and pass it to the render thread
		_tempRenderPackets = await manager.engine.StateSync.RenderPacketsSwapPrior_New(_tempRenderPackets);
		_tempRenderPackets = renderThreadInput.WriteAndSwap(_tempRenderPackets);
	}





	protected override void OnDispose()
	{

		//Raylib.CloseWindow();


		base.OnDispose();

		if (_renderTask != null)
		{
			_renderLoopAutoResetEvent.Set();
			_renderTask.Wait();
		}

		_renderTask = null;
		_renderThread.Dispose();
		_renderThread = null;


	}

	public RecycleChannel<ConcurrentQueue<IRenderPacketNew>> renderThreadInput = new(
		1,
		() => new(),
		(toClean) =>
		{
			toClean.Clear();
			return toClean;
		}
	);

	public static int mtId;
	private async Task _RenderThread_Worker()
	{
		//the render packets currently being consumed by this render thread.
		//note that this gets swapped out for the queue stored in Engine.Phase0_StateSync every loop via some complex swap logic below.
		ConcurrentQueue<IRenderPacketNew> packetsCurrent = new();
		var pswRenderLoop = new PerfSpikeWatch("RenderLoop");
		var pswRenderPacketSync = new PerfSpikeWatch("RenderPacketSync");
		var pswRaylibDraw = new PerfSpikeWatch("RaylibDraw");
		var pswDrawRenderPacket = new PerfSpikeWatch("DrawRenderPacket");
		var pswDoDraw = new PerfSpikeWatch("DoDraw");
		var pswRaylibSetup = new PerfSpikeWatch("RaylibSetup");

		var pswRaylibTeardown = new PerfSpikeWatch("RaylibTeardown");
		var pswDrawGrid = new PerfSpikeWatch("DrawGrid");
		var pswEnd3d = new PerfSpikeWatch("DrawEnd3d");
		var pswDrawText = new PerfSpikeWatch("DrawText");
		var pswDrawFps = new PerfSpikeWatch("DrawFps");
		var pswEndDrawing = new PerfSpikeWatch("EndDrawing");

		//var x = new SynchronizationContext();
		//Thread.BeginThreadAffinity();
		mtId = Thread.CurrentThread.ManagedThreadId;
		//Console.WriteLine($"_RenderThread_Worker AFINITY.  cpuId={Thread.GetCurrentProcessorId()}, mtId={Thread.CurrentThread.ManagedThreadId}");
		try
		{
			Raylib.InitWindow(screenSize.Width, screenSize.Height, windowTitle);
			Raylib.SetTargetFPS(60);
			var swElapsed = Stopwatch.StartNew();
			var swTotal = Stopwatch.StartNew();


			while (!Raylib.WindowShouldClose() && IsRegistered && IsDisposed == false)
			{
				pswRenderLoop.Lap();
				var elapsed = (float)swElapsed.Elapsed.TotalSeconds;
				swElapsed.Restart();
				var totalTime = (float)swTotal.Elapsed.TotalSeconds;

				//if disabled, wait until we are not disabled
				if (IsDisabled)
				{
					var updateTask = _lastUpdateState.UpdateTask;
					if (updateTask != null)
					{
						await _lastUpdateState.UpdateTask;
					}
					else
					{
						//due to timer resolution, this could wait up to 16ms.
						//that is okay because we are "disabled"
						await Task.Delay(1);
					}

					continue;
				}

				#region TO_DELETE

				//wait until released from main system.update method
				//var isSuccess = await _renderLoopAutoResetEvent.WaitAsync(TimeSpan.FromMilliseconds(1));
				//if (!isSuccess)
				//{
				//	//our main system update isn't done yet, so delay rendering until it is ready (when we have new render data)
				//	continue;
				//}

				#endregion

				//critical section that must not be raced by the main simulation's render update task
				//this section swaps out our renderPacket queue with a "fresh" one from the engine threads
				pswRenderPacketSync.Start();
				//{
				//	await _renderLoopAutoResetEvent.WaitAsync();
				//	await _updateSyncCriticalSectionLock.WaitAsync();
				//	try
				//	{
				//		packetsCurrent = Interlocked.Exchange(ref _tempRenderPackets, packetsCurrent);
				//		_renderLoopAutoResetEvent.Reset();
				//	}
				//	finally
				//	{
				//		_updateSyncCriticalSectionLock.Release();
				//	}
				//}
				packetsCurrent = await renderThreadInput.ReadAndSwap(packetsCurrent);

				pswRenderPacketSync.LapAndReset();

				#region TO_DELETE
				//Thread.CurrentThread.Priority = ThreadPriority.Highest;
				//Thread.BeginCriticalRegion();
				//Thread.BeginThreadAffinity();
				//////obtain render packets for the most recent frame (N-1) in a locked fashion
				////await _swapPacketsLock.WaitAsync();
				////try
				////{
				////	//__DEBUG.Throw(_packetsCurrent.Count == 0);
				////	var temp = _packetsCurrent;
				////	_packetsPending = _packetsCurrent;
				////	_packetsCurrent = temp;
				////}
				////finally
				////{
				////	_swapPacketsLock.Release();
				////}

				#endregion

				//await Task.Delay(10);

				pswRaylibDraw.Start();
				pswRaylibSetup.Start();
				Raylib.BeginDrawing();
				
				Raylib.ClearBackground(Raylib.RAYWHITE);
				Raylib.BeginMode3D(camera);
				pswRaylibSetup.LapAndReset();

				//draw renderpackets
				pswDrawRenderPacket.Start();
				{
					//debug logic
					{
						//#if CHECKED
						if (packetsCurrent.Count == 0)
						{
							Console.WriteLine("NO PACKETS");
						}

						//#endif
						__DEBUG.Throw(_thread_renderPackets.Count == 0);
					}

					//sort render packets according to their internal priority
					{
						while (packetsCurrent.TryDequeue(out var renderPacket))
						{
							_thread_renderPackets.Add(renderPacket);
						}

						_thread_renderPackets.Sort();
					}

					//draw them
					pswDoDraw.Start();
					foreach (var renderPacket in _thread_renderPackets)
					{
						//__TEST_JITTER_RENDER_XFORM(elapsed, totalTime, renderPacket);

						renderPacket.DoDraw();
					}

					pswDoDraw.Stop();
					_thread_renderPackets.Clear();

				}
				pswDoDraw.LapAndReset();
				pswDrawRenderPacket.LapAndReset();

				pswRaylibTeardown.Start();
				{
					pswDrawGrid.Start();
					Raylib.DrawGrid(100, 1.0f);
					pswDrawGrid.LapAndReset();
					pswEnd3d.Start();
					Raylib.EndMode3D();
					pswEnd3d.LapAndReset();
					pswDrawText.Start();
					Raylib.DrawText($"Reference Rendering {this.renderThreadInput._reader.Count}", 10, 40, 20, Raylib.DARKGRAY);
					pswDrawText.LapAndReset();
					pswDrawFps.Start();
					Raylib.DrawFPS(10, 10);
					pswDrawFps.LapAndReset();
					pswEndDrawing.Start();
					Raylib.EndDrawing();
					pswEndDrawing.LapAndReset();
				}
				pswRaylibTeardown.LapAndReset();

				pswRaylibDraw.LapAndReset();

				Thread.EndCriticalRegion();
				Thread.EndThreadAffinity();
			}
			Raylib.CloseWindow();
		}
		finally
		{
			//Thread.EndThreadAffinity();
		}
		//if/when our render thread terminates, stop the engine.
		//this can happen when the user closes the render window.
		_ = manager.engine.Updater.Stop();
	}

	private static void __TEST_JITTER_RENDER_XFORM(float elapsed, float totalTime, IRenderPacketNew renderPacket)
	{
		var rp3d = renderPacket as RenderPacket3d;
		var span = rp3d.instances.AsWriteSpan();
		for (var i = 0; i < span.Length; i++)
		{
			ref var currXform = ref span[i];
			var currentPos = currXform.Translation;
			currXform = Matrix4x4.CreateFromYawPitchRoll(0, 0, i + totalTime);
			currXform.Translation = currentPos + new Vector3(MathF.Sin(totalTime), 0f, MathF.Cos(totalTime)) * elapsed * 3;
		}
	}
}
