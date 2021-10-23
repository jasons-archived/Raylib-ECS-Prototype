using System.Numerics;
using BepuPhysics;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine.Ecs;
using NotNot.Engine.Ecs.Allocation;
using NotNot.Engine.Internal.SimPipeline;
using Raylib_cs;

namespace NotNot;



//public struct Transform
//{
//    public Vector3 position;
//    public Quaternion rotation;
//	public Vector3 scale;


//    public Matrix4x4 transform;
//}




//public class PhysicsSystem : NotNot.Engine.Ecs.SystemBase
//{
//    protected override Task Update()
//    {
//        throw new NotImplementedException();
//    }
//}







//public struct PhysicsInfo
//{
//    BodyReference bodyRef;
//}


//when explode occurs, do the following


//system nodes

public record struct RenderPrimitive
{
	bool showWireframe;
	bool showSolid;
	Primitive primitive;
}

public enum Primitive
{

	Box,
}

public class RaylibRendering : SystemBase
{
	System.Drawing.Size screenSize = new(1920, 1080);
	string windowTitle;

	Camera3D camera = new()
	{
		position = new Vector3(10.0f, 10.0f, 10.0f), // Camera3D position
		target = new Vector3(0.0f, 0.0f, 0.0f),      // Camera3D looking at point
		up = new Vector3(0.0f, 1.0f, 0.0f),          // Camera3D up vector (rotation towards target)
		fovy = 45.0f,                             // Camera3D field-of-view Y
		projection = CameraProjection.CAMERA_PERSPECTIVE,                   // Camera3D mode type
	};

	protected override void OnRegister()
	{
		base.OnRegister();
		_renderTask = Task.Run(_RenderThread);
	}
	private Task _renderTask;
	protected override Task OnUpdate(Frame frame)
	{
		//throw new NotImplementedException();
		return Task.CompletedTask;
	}
	protected override void OnDispose()
	{

		//Raylib.CloseWindow();


		base.OnDispose();

		if (_renderTask != null)
		{
			_renderTask.Wait();
		}
		_renderTask = null;


	}

	private async Task _RenderThread()
	{
		Raylib.InitWindow(screenSize.Width, screenSize.Height, windowTitle);

		while (!Raylib.WindowShouldClose() && IsRegistered && IsDisposed == false)
		{
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
			Raylib.BeginDrawing();

			Raylib.ClearBackground(Color.SKYBLUE);
			Raylib.BeginMode3D(camera);
			Raylib.DrawGrid(10, 10f);
			Raylib.EndMode3D();
			Raylib.DrawText("RaylibRendering System", 10, 40, 20, Color.DARKGRAY);
			Raylib.DrawFPS(10, 10);


			Raylib.EndDrawing();

		}

		Raylib.CloseWindow();
		_ = manager.engine.Updater.Stop();
	}

}



public record struct Translation
{
	public Vector3 value;
}

public record struct PlayerInput
{
}

public record struct Move
{
	public Vector3 value;
}




public class PlayerInputSystem : NotNot.Engine.Ecs.System
{
	EntityQuery playerMoveQuery;
	protected override void OnInitialize()
	{
		base.OnInitialize();


		playerMoveQuery = entityManager.Query(new() { All = { typeof(PlayerInput), typeof(Move) } });

		RegisterWriteLock<Move>();
		RegisterReadLock<PlayerInput>();

	}




	protected override async Task OnUpdate(Frame frame)
	{
		playerMoveQuery.Run((ReadMem<EntityMetadata> meta, Mem<Move> moves, ReadMem<PlayerInput> players) =>
		{

			var metaSpan = meta.Span;
			for (var i = 0; i < meta.length; i++)
			{
				__ERROR.Throw(metaSpan[i].IsAlive, "why dead being enumerated?  we are forcing autopack");
				//moves[i].value =Vector3.Normalize(__.Rand._NextVector3());
				var vec = __.Rand._NextVector3();
				var norm = Vector3.Normalize(vec);
				moves[i].value = norm;

			}

		});
	}
}
/// <summary>
/// This example simply takes a "move component" vector3 and adds it to the position "translation component" vector3
/// </summary>
public class MoveSystem : NotNot.Engine.Ecs.System
{
	EntityQuery moveQuery;
	protected override void OnInitialize()
	{
		base.OnInitialize();
		//create a query that selects all entities that have a Move and Translation component
		//for performance reasons this should be cached as a class member,
		moveQuery = entityManager.Query(new() { All = { typeof(Move), typeof(Translation) } });

		//notify our need for read/write access so systems can be multithreaded safely
		RegisterWriteLock<Translation>();
		RegisterReadLock<Move>();

	}

	protected override async Task OnUpdate(Frame frame)
	{
		Console.WriteLine($"-------------------- {frame._stats._frameId}");

		//run the query, selecting all entities and doing work on them.
		moveQuery.Run((
			//metadata about the entity
			ReadMem<EntityMetadata> meta,
			//write access to Translation component
			Mem<Translation> translations,
			//read access to Move component
			ReadMem<Move> moves
			) =>
		{

			for (var i = 0; i < meta.length; i++)
			{
				//apply move vector onto translation vector
				translations[i].value += moves[i].value;
				Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}, move={moves[i].value}");
			}

		});
	}
}


//public class TestGame
//{
//	public void Run()
//	{
//		var engine = new Engine.Engine();
//		engine.Updater = new NotNot.Engine.HeadlessUpdater();
//		engine.Initialize();
//		//engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });

//		engine.DefaultWorld.AddChild(new MoveSystem());
//		engine.DefaultWorld.AddChild(new PlayerInputSystem());




//		engine.Updater.Start();

//		var em = engine.DefaultWorld.entityManager;

//		var archetype = em.GetOrCreateArchetype(new() { typeof(PlayerInput), typeof(Translation), typeof(Move) });

//		em.EnqueueCreateEntity(1, archetype, (args) => {
//			var (accessTokens, entityHandles, archetype) = args;
//			foreach (var accessToken in accessTokens)
//			{
//				ref var translation = ref accessToken.GetComponentWriteRef<Translation>();
//				ref var move = ref accessToken.GetComponentWriteRef<Move>();
//				translation = new Translation() { value = Vector3.One };
//				move = new() { value = Vector3.Zero };
//			}
//		});



//	}

//}

