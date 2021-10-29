using System.Numerics;
using BepuPhysics;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine.Ecs;
using NotNot.Engine.Ecs.Allocation;
using NotNot.Engine.Internal.SimPipeline;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.ConfigFlags;
using static Raylib_cs.Color;
using static Raylib_cs.CameraProjection;
using static Raylib_cs.ShaderLocationIndex;
using static Raylib_cs.ShaderUniformDataType;
using static Raylib_cs.MaterialMapIndex;
using static Raylib_cs.CameraMode;
using static Raylib_cs.KeyboardKey;
using System.Collections.Concurrent;
using NotNot.Engine;

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




public class RaylibRendering : SystemBase
{
	System.Drawing.Size screenSize = new(1920, 1080);
	string windowTitle;

	Camera3D camera = new()
	{
		position = new Vector3(0.0f, 10.0f, 10.0f), // Camera3D position
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

	private SemaphoreSlim _swapPacketsLock = new(1, 1);

	/// <summary>
	/// packets obtained from the Engine.SyncState  (packets from frame N-1)
	/// <para>this is swapped back-and-forth with <see cref="_packetsPending"/></para>
	/// </summary>
	private ConcurrentQueue<IRenderPacket> _packetsPending = new();
	/// <summary>
	/// the packets used by the render thread loop   (consumed by each pass of that loop)
	/// <para>this is swapped back-and-forth with <see cref="_packetsPending"/></para>
	/// </summary>
	private ConcurrentQueue<IRenderPacket> _packetsCurrent = new();

	/// <summary>
	/// sync point that prevents the render thread from running faster than the simulation frame rate
	/// <para>render thread can run slower without impacting performance of the simulation</para>
	/// </summary>
	private DotNext.Threading.AsyncAutoResetEvent _renderLoopAutoResetEvent = new(false);

	/// <summary>
	/// temp collection used in the render thread, used to sort render packets before drawing
	/// </summary>
	List<IRenderPacket> _thread_renderPackets = new();

	protected override async Task OnUpdate(Frame frame)
	{

	
		await _swapPacketsLock.WaitAsync();
		try
		{
			this.manager.engine.StateSync.RenderPacketsSwapPrior(_packetsPending,out _packetsPending);
			_renderLoopAutoResetEvent.Set();

		}
		finally
		{
			_swapPacketsLock.Release();
		}

		//DotNext.Threading.AsyncAutoResetEvent autoResetEvent = new(false);



		//return Task.CompletedTask;
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

		Mesh cube = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);
		Shader shader = Raylib.LoadShader("resources/shaders/glsl330/base_lighting.vs", "resources/shaders/glsl330/lighting.fs");
		// Get some shader loactions
		unsafe
		{
			int* locs = (int*)shader.locs;
			locs[(int)SHADER_LOC_MATRIX_MVP] = GetShaderLocation(shader, "mvp");
			locs[(int)SHADER_LOC_VECTOR_VIEW] = GetShaderLocation(shader, "viewPos");
			locs[(int)SHADER_LOC_MATRIX_MODEL] = GetShaderLocationAttrib(shader, "instanceTransform");
		}
		// Ambient light level
		int ambientLoc = GetShaderLocation(shader, "ambient");
		Utils.SetShaderValue(shader, ambientLoc, new float[] { 0.2f, 0.2f, 0.2f, 1.0f }, SHADER_UNIFORM_VEC4);

		Rlights.CreateLight(0, LightType.LIGHT_DIRECTIONAL, new Vector3(50, 50, 0), Vector3.Zero, WHITE, shader);

		Material material = LoadMaterialDefault();
		material.
		
		unsafe
		{
			MaterialMap* maps = (MaterialMap*)material.maps.ToPointer();
			maps[(int)MATERIAL_MAP_DIFFUSE].color = RED;
		}

		
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
			var isSuccess = await _renderLoopAutoResetEvent.WaitAsync(TimeSpan.FromMilliseconds(1));
			if (!isSuccess)
			{
				//our main system update isn't done yet, so delay rendering until it is ready (when we have new render data)
				continue;
			}

			//obtain our render packets in a locked fashion
			await _swapPacketsLock.WaitAsync();
			try
			{
				__DEBUG.Throw(_packetsCurrent.Count == 0);
				var temp = _packetsCurrent;
				_packetsPending = _packetsCurrent;
				_packetsCurrent = temp;
			}
			finally
			{
				_swapPacketsLock.Release();
			}


			

			//Utils.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, new Vector3[] { camera.position }, SHADER_UNIFORM_VEC3);
			Utils.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, camera.position, SHADER_UNIFORM_VEC3);

			Raylib.BeginDrawing();

			Raylib.ClearBackground(Color.RAYWHITE);
			Raylib.BeginMode3D(camera);
			//Matrix4x4[] transforms = new[]{ Matrix4x4.Identity };
			//DrawMeshInstanced(cube, material,new[]{ Matrix4x4.Identity }, 1);
			//DrawMesh(cube, material, Matrix4x4.Identity);

			//draw renderpackets
			{
				__DEBUG.Throw(_thread_renderPackets.Count == 0);
				while (_packetsCurrent.TryDequeue(out var renderPacket))
				{
					_thread_renderPackets.Add(renderPacket);
				}
				_thread_renderPackets.Sort((first, second) => first.RenderLayer.CompareTo(second.RenderLayer));
				foreach (var renderPacket in _thread_renderPackets)
				{
					renderPacket.DoDraw();
				}
				_thread_renderPackets.Clear();
			}

			Raylib.DrawGrid(10, 1.0f);
			Raylib.EndMode3D();
			Raylib.DrawText("Reference Rendering", 10, 40, 20, Color.DARKGRAY);
			Raylib.DrawFPS(10, 10);


			Raylib.EndDrawing();

		}

		Raylib.CloseWindow();
		_ = manager.engine.Updater.Stop();
	}

}

public class BatchedRenderMesh : IRenderPacket
{
	public int RenderLayer { get; } = 0;

	public RenderMesh renderMesh;
	public Mem<Matrix4x4> instances;

	public void DoDraw()
	{
		var xforms = instances.Span;
		var mesh = renderMesh.mesh;
		var material = renderMesh.material;
		//TODO: when raylib 4 is released change to use instanced based.   right now (3.7.x) there's a bug where it doesn't render instances=1
		for (var i = 0; i < xforms.Length; i++)
		{
			Raylib.DrawMesh(mesh, material, xforms[i]);
		}
	}
}



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



public record struct Transform
{
	private Vector3 _pos;
	public Vector3 Pos { get => _pos; set { _pos = value; version++; } }
	public Vector3 _scale;
	public Vector3 Scale { get => _scale; set { _scale = value; version++; } }
	public Quaternion _rotation;
	public Quaternion Rotation { get => _rotation; set { _rotation = value; version++; } }
	public short version;
	//public Matrix4x4 _xform;
}

public record struct MeshRenderInfo
{
	public int meshId;
	public int materialId;
	public int renderSlot;
	public short lastTransformVersion;
}
public record struct Scale
{
	public Vector3 value;
}
public record struct Rotation
{
	public Quaternion value;
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


public class RenderMesh
{
	public Raylib_cs.Mesh mesh;
	public Raylib_cs.Material material;
}



public class PlayerInputSystem : NotNot.Engine.Ecs.System
{
	EntityQuery playerMoveQuery;
	protected override void OnInitialize()
	{
		base.OnInitialize();


		playerMoveQuery = entityManager.Query(new() { all = { typeof(PlayerInput), typeof(Move) } });

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
		moveQuery = entityManager.Query(new() { all = { typeof(Move), typeof(Translation) } });

		//notify our need for read/write access so systems can be multithreaded safely
		RegisterWriteLock<Translation>();
		RegisterReadLock<Move>();

	}

	protected override async Task OnUpdate(Frame frame)
	{
		//Console.WriteLine($"-------------------- {frame._stats._frameId}");

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
				//Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}, move={moves[i].value}");
			}

		});
	}
}

public class VisibilitySystem : NotNot.Engine.Ecs.System
{
	EntityQuery positionQuery;
	protected override void OnInitialize()
	{
		base.OnInitialize();
		//create a query that selects all entities that have a Move and Translation component
		//for performance reasons this should be cached as a class member,
		positionQuery = entityManager.Query(new() { all = { typeof(Translation) } });

		//notify our need for read/write access so systems can be multithreaded safely
		RegisterReadLock<Translation>();
	}
	protected override Task OnUpdate(Frame frame)
	{
		positionQuery.Run((
		//metadata about the entity
		ReadMem<EntityMetadata> meta,
		//write access to Translation component
		ReadMem<Translation> translations
		) =>
		{
			

			var instances = Mem<Matrix4x4>.Allocate(meta.length,false);
			for (var i = 0; i < meta.length; i++)
			{				
				//Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}, move={moves[i].value}");
				instances[i] = Matrix4x4.CreateTranslation(translations[i].value);
			}
			var renderPacket = new BatchedRenderMesh();			
			renderPacket.instances = instances;

			this.manager.engine.StateSync.EnqueueRenderPacket(renderPacket);


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

