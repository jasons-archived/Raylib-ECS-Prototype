using System.Numerics;
using BepuPhysics;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Ecs;
using NotNot.Ecs.Allocation;
using NotNot.SimPipeline;
using Raylib_cs;

using NotNot.Rendering;

namespace NotNot;



//public struct WorldXform
//{
//    public Vector3 position;
//    public Quaternion rotation;
//	public Vector3 scale;


//    public Matrix4x4 transform;
//}




//public class PhysicsSystem : NotNot.Ecs.SystemBase
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

public class BatchedRenderMesh : IRenderPacket
{

	public Batched3dModel asset;
	public int RenderLayer { get; } = 0;

	public BatchedRenderMesh(Batched3dModel asset)
	{
		this.asset = asset;
	}

	public bool IsInitialized { get; set; }
	public void Initialize()
	{
		IsInitialized = true;

		if (asset.IsInitialized == false)
		{
			asset.Initialize();
		}


	}

	public Mem<Matrix4x4> instances;

	public void DoDraw()
	{

		asset.DoDraw(this);

		//if (asset.IsInitialized == false)
		//{
		//	asset.Initialize();
		//}
		////__DEBUG.Throw(IsInitialized);

		////Utils.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, new Vector3[] { camera.position }, SHADER_UNIFORM_VEC3);
		////Utils.SetShaderValue(asset.shader, (int)SHADER_LOC_VECTOR_VIEW, RaylibRendering.camera.position, SHADER_UNIFORM_VEC3);

		//var xforms = instances.Span;
		////var mesh = renderMesh.mesh;
		////var material = renderMesh.material;
		////TODO: when raylib 4 is released change to use instanced based.   right now (3.7.x) there's a bug where it doesn't render instances=1
		//for (var i = 0; i < xforms.Length; i++)
		//{
		//	Raylib.DrawMesh(asset.mesh, asset.material, Matrix4x4.Transpose(xforms[i])); //IMPORTANT: raylib is row-major.   need to transpose dotnet (column major) to match
		//}
		//if (xforms.Length == 0)
		//{
		//	Console.WriteLine("Packet is EMPTY!!>?!?!");
		//}

		//Console.WriteLine($"cpuId={Thread.GetCurrentProcessorId()}, mtId={Thread.CurrentThread.ManagedThreadId}");
	}

	public int CompareTo(IRenderPacket? other)
	{
		return 0;
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



public record struct WorldXform
{
	public short version;
	private Vector3 _position = Vector3.Zero;
	public Vector3 Position
	{
		get => _position; set
		{
			_position = value;
			version++;
			xformMatrix.Translation = value;
		}
	}
	private Vector3 _scale = Vector3.One;
	public Vector3 Scale
	{
		get => _scale; set
		{

			_scale = value;
			version++;
			_RecalculateMatrix();

		}
	}
	private Quaternion _rotation = Quaternion.Identity;
	public Quaternion Rotation
	{
		get => _rotation; set
		{
			_rotation = value;
			version++;
			_RecalculateMatrix();
		}
	}

	private void _RecalculateMatrix()
	{
		xformMatrix = Matrix4x4.CreateScale(_scale) * Matrix4x4.CreateFromQuaternion(_rotation);
		xformMatrix.Translation = _position;
	}
	public Matrix4x4 xformMatrix = Matrix4x4.Identity;

	/// <summary>
	/// set this xform from a matrix.   the matrix needs to be decomposable so we can obtain the position/rotation/scale components
	/// </summary>
	/// <param name="toDecompose"></param>
	/// <returns></returns>
	public bool FromMatrix(ref Matrix4x4 toDecompose)
	{
		var result = Matrix4x4.Decompose(toDecompose, out var scale, out var rotation, out var translation);
		if (!result)
		{
			return false;
		}
		_scale = scale;
		_position = translation;
		_rotation = rotation;
		xformMatrix = toDecompose;
		version++;
		return true;
	}


}

public record struct MeshRenderInfo
{
	public int meshId;
	public int materialId;
	public int renderSlot;
	public short lastTransformVersion;
}
//public record struct Scale
//{
//	public Vector3 value;
//}
//public record struct Rotation
//{
//	public Quaternion value;
//}
//public record struct Translation
//{
//	public Vector3 value;
//}
public record struct TestInput
{
}

public record struct Move
{
	public Vector3 pos;
	public Quaternion rot;
}


public class RenderMesh
{
	public Raylib_cs.Mesh mesh;
	public Raylib_cs.Material material;

	public CombinedHash combinedHash;

	public RenderMesh(Mesh mesh, Material material)
	{
		this.mesh = mesh;
		this.material = material;
	}
}

public class RenderInfo : IPartitionComponent
{
	public bool Equals(IPartitionComponent? other)
	{
		throw new NotImplementedException();
	}

	Dictionary<CombinedHash, object> test;
}


public class PlayerInputSystem : NotNot.Ecs.System
{
	EntityQuery playerMoveQuery;
	protected override void OnInitialize()
	{
		base.OnInitialize();


		playerMoveQuery = entityManager.Query(new() { all = { typeof(TestInput), typeof(Move) } });

		RegisterWriteLock<Move>();
		RegisterReadLock<TestInput>();

	}




	protected override async Task OnUpdate(Frame frame)
	{
		playerMoveQuery.Run((ReadMem<EntityMetadata> meta, Mem<Move> moves, ReadMem<TestInput> players) =>
		{
			var metaSpan = meta.Span;
			var totalSeconds = (float)frame._stats._wallTime.TotalSeconds;


			for (var i = 0; i < meta.length; i++)
			{
				__ERROR.Throw(metaSpan[i].IsAlive, "why dead being enumerated?  we are forcing autopack");


				var norm = new Vector3(MathF.Sin(totalSeconds), 0f, MathF.Cos(totalSeconds));
				moves[i].pos = norm * (float)frame._stats._frameElapsed.TotalSeconds * 3;
				//moves[i].

			}

		});
	}
}
/// <summary>
/// This example simply takes a "move component" vector3 and adds it to the position "translation component" vector3
/// </summary>
public class MoveSystem : NotNot.Ecs.System
{
	EntityQuery moveQuery;
	protected override void OnInitialize()
	{
		base.OnInitialize();
		//create a query that selects all entities that have a Move and Translation component
		//for performance reasons this should be cached as a class member,
		moveQuery = entityManager.Query(new() { all = { typeof(Move), typeof(WorldXform) } });

		//notify our need for read/write access so systems can be multithreaded safely
		RegisterWriteLock<WorldXform>();
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
			Mem<WorldXform> transforms,
			//read access to Move component
			ReadMem<Move> moves
			) =>
		{

			var elapsed = (float)frame._stats._wallTime.TotalSeconds * 2;

			for (var i = 0; i < meta.length; i++)
			{
				//apply move vector onto translation vector
				transforms[i].Position += moves[i].pos;
				transforms[i].Rotation = Quaternion.CreateFromYawPitchRoll(0, 0, elapsed);
				transforms[i].Scale = Vector3.One * MathF.Cos(elapsed);
				//Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}, move={moves[i].value}");
			}

		});
	}
}

public class VisibilitySystem : NotNot.Ecs.System
{
	EntityQuery positionQuery;
	Batched3dModel asset = new();
	protected override void OnInitialize()
	{
		base.OnInitialize();
		//create a query that selects all entities that have a Move and Translation component
		//for performance reasons this should be cached as a class member,
		positionQuery = entityManager.Query(new() { all = { typeof(WorldXform) } });

		//notify our need for read/write access so systems can be multithreaded safely
		RegisterReadLock<WorldXform>();
	}
	protected override Task OnUpdate(Frame frame)
	{
		positionQuery.Run((
		//metadata about the entity
		ReadMem<EntityMetadata> meta,
		//write access to Translation component
		ReadMem<WorldXform> transforms
		) =>
		{


			var instances = Mem<Matrix4x4>.Allocate(meta.length, false);
			for (var i = 0; i < meta.length; i++)
			{
				//instances[i] = Matrix4x4.Identity;
				//Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}, move={moves[i].value}");
				//Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}");
				//instances[i] = Matrix4x4.Identity;// Matrix4x4.CreateTranslation(translations[i].value);
				instances[i] = transforms[i].xformMatrix;//Matrix4x4.CreateTranslation(transforms[i].xformMatrix);
														 //instances[i].Translation = translations[i].value;
														 //instances[i].Translation = new Vector3(0,0,1.1f);
														 //instances[i] = Raymath.MatrixTranslate(0, 0, 1);



			}
			var renderPacket = new BatchedRenderMesh(asset);
			renderPacket.instances = instances;

			this.manager.engine.StateSync.EnqueueRenderPacket(renderPacket);


		});

		return Task.CompletedTask;

	}
}


//public class TestGame
//{
//	public void Run()
//	{
//		var engine = new Engine.Engine();
//		engine.Updater = new NotNot.HeadlessUpdater();
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

