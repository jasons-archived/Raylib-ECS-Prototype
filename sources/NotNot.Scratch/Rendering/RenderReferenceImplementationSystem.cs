using System.Collections.Concurrent;
using System.Numerics;
using NotNot.Bcl.Diagnostics;
using NotNot.Ecs;
using NotNot.SimPipeline;
using Raylib_cs;

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
		target = new Vector3(0.0f, 0.0f, 0.0f),      // Camera3D looking at point
		up = new Vector3(0.0f, 1.0f, 0.0f),          // Camera3D up vector (rotation towards target)
		fovy = 45.0f,                             // Camera3D field-of-view Y
		projection = CameraProjection.CAMERA_PERSPECTIVE,                   // Camera3D mode type
	};

	//private CustomTaskScheduler renderScheduler = new CustomTaskScheduler(1);

	protected override void OnRegister()
	{
		base.OnRegister();

		//start rendering on it's own thread
		_renderTask = new Task(_RenderThread, TaskCreationOptions.LongRunning); //runs for entire app duration
		_renderTask.Start();

		//_renderTask = new Task(() => StartRender2());
		//_renderTask.Start(renderScheduler);

		//renderScheduler
		//_renderTask = Task.Factory.StartNew(_RenderThread,CancellationToken.None,TaskCreationOptions.LongRunning,renderScheduler);
		//new Task(_RenderThread,TaskCreationOptions.LongRunning)

		//TaskScheduler.con
		//var x = new Thread()
		//Task.Factory.
	}

	private void _RenderThread()
	{
		//If Raylib or any app executes OpenGl instructions from multiple threads, it will crash with an AccessViolationException.
		//This is a problem not only for doing multithreading, but also using async/await.
		//If you use async/await in your render loop, you might be running on a different thread if you ever await, such as await Task.Delay(1);
		//The simplist solution is to start your render task via Nito.AsyncEx.AsyncContext.Run(_RenderThread);
		//which will force the async continuations to run on the same managed thread.
		//alternately creating a custom task scheduler works, but is a lot more effort.  see the edu.md "Threading" section for notes, or the NotNot.Bcl.Threading.CustomTaskScheduler
		Nito.AsyncEx.AsyncContext.Run(_RenderThread_Worker);


		Console.WriteLine("DONE");
	}
	private Task StartRender()
	{
		try
		{
			return _RenderThread_Worker();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
		finally
		{
			Console.WriteLine("DONE");
		}
		return Task.CompletedTask;

	}

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
	List<IRenderPacket> _thread_renderPackets = new();


	/// <summary>
	/// render packets obtained from the Phase0_SyncState system
	/// </summary>
	private ConcurrentQueue<IRenderPacket> _nextRenderPackets = new();


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

		await _updateSyncCriticalSectionLock.WaitAsync();
		try
		{
			_nextRenderPackets = await manager.engine.StateSync.RenderPacketsSwapPrior_New(_nextRenderPackets);
			_renderLoopAutoResetEvent.Set();
		}
		finally
		{
			_updateSyncCriticalSectionLock.Release();
		}
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


	}

	private async Task _RenderThread_Worker()
	{
		ConcurrentQueue<IRenderPacket> packetsCurrent = new();

		//var x = new SynchronizationContext();
		Thread.BeginThreadAffinity();
		//Console.WriteLine($"_RenderThread_Worker AFINITY.  cpuId={Thread.GetCurrentProcessorId()}, mtId={Thread.CurrentThread.ManagedThreadId}");
		try
		{
			Raylib.InitWindow(screenSize.Width, screenSize.Height, windowTitle);


			while (!Raylib.WindowShouldClose() && IsRegistered && IsDisposed == false)
			{


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
						await Task.Delay(1);
					}

					continue;
				}

				//wait until released from main system.update method
				//var isSuccess = await _renderLoopAutoResetEvent.WaitAsync(TimeSpan.FromMilliseconds(1));
				//if (!isSuccess)
				//{
				//	//our main system update isn't done yet, so delay rendering until it is ready (when we have new render data)
				//	continue;
				//}

				//critical section that must not be raced by the main simulation's render update task
				{
					await _renderLoopAutoResetEvent.WaitAsync();
					await _updateSyncCriticalSectionLock.WaitAsync();
					try
					{
						packetsCurrent = Interlocked.Exchange(ref _nextRenderPackets, packetsCurrent);
						_renderLoopAutoResetEvent.Reset();
					}
					finally
					{
						_updateSyncCriticalSectionLock.Release();
					}
				}
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





				Raylib.BeginDrawing();

				Raylib.ClearBackground(Color.RAYWHITE);
				Raylib.BeginMode3D(camera);
				//Matrix4x4[] transforms = new[]{ Matrix4x4.Identity };
				//DrawMeshInstanced(cube, material,new[]{ Matrix4x4.Identity }, 1);
				//DrawMesh(cube, material, Matrix4x4.Identity);

				//draw renderpackets
				{
					//#if CHECKED
					if (packetsCurrent.Count == 0)
					{
						Console.WriteLine("NO PACKETS");
					}
					//#endif
					__DEBUG.Throw(_thread_renderPackets.Count == 0);
					while (packetsCurrent.TryDequeue(out var renderPacket))
					{
						_thread_renderPackets.Add(renderPacket);
					}
					_thread_renderPackets.Sort();
					foreach (var renderPacket in _thread_renderPackets)
					{
						renderPacket.DoDraw();
					}
					_thread_renderPackets.Clear();
				}

				Raylib.DrawGrid(100, 1.0f);
				Raylib.EndMode3D();
				Raylib.DrawText("Reference Rendering", 10, 40, 20, Color.DARKGRAY);
				Raylib.DrawFPS(10, 10);


				Raylib.EndDrawing();

			}
			Raylib.CloseWindow();
		}
		finally
		{
			Thread.EndThreadAffinity();
		}
		_ = manager.engine.Updater.Stop();
	}

}
