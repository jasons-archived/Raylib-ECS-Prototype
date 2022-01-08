using System.Numerics;
using BepuPhysics;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Ecs;
using NotNot.Ecs.Allocation;
using NotNot.SimPipeline;
using Raylib_CsLo;

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
	public Raylib_CsLo.Mesh mesh;
	public Raylib_CsLo.Material material;

	public CombinedHash combinedHash;

	public RenderMesh(Mesh mesh, Material material)
	{
		this.mesh = mesh;
		this.material = material;
	}
}

public class RenderInfo 
{

	Dictionary<CombinedHash, object> test;
}


public class TestInputSystem : NotNot.Ecs.System
{
	EntityQuery playerMoveQuery;
	protected override async void OnInitialize()
	{
		base.OnInitialize();


		playerMoveQuery = entityManager.Query(new() { all = { typeof(TestInput), typeof(Move) } });

		RegisterWriteLock<Move>();
		RegisterReadLock<TestInput>();

	}




	protected override async Task OnUpdate(Frame frame)
	{
#if DEBUG
		__ERROR.Throw(Thread.CurrentThread.ManagedThreadId != RenderReferenceImplementationSystem.mtId);
		if (Thread.CurrentThread.ManagedThreadId == RenderReferenceImplementationSystem.mtId)
		{
			__ERROR.WriteLine("whut");
		}
#endif
		playerMoveQuery.Run((ReadMem<EntityMetadata> meta, Mem<Move> moves, ReadMem<TestInput> players) =>
		{
			var metaSpan = meta.Span;
			var totalTime = (float)frame._stats._wallTime.TotalSeconds * 2;


			for (var i = 0; i < meta.length; i++)
			{
				__ERROR.Throw(metaSpan[i].IsAlive, "why dead being enumerated?  we are forcing autopack");


				var norm = new Vector3(MathF.Sin(totalTime), 0f, MathF.Cos(totalTime));
				moves[i].pos = norm * (float)frame._stats._frameElapsed.TotalSeconds * 3;
				moves[i].rot = Quaternion.CreateFromYawPitchRoll(0, 0, totalTime);

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
				transforms[i].Rotation = moves[i].rot;
				//transforms[i].Position = new Vector3(MathF.Sin(elapsed), 0f, MathF.Cos(elapsed));
				//transforms[i].Rotation = Quaternion.CreateFromYawPitchRoll(0, 0, elapsed);
				//transforms[i].Scale = Vector3.One * MathF.Cos(elapsed);
				//Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}, move={moves[i].value}");
			}

		});
	}
}
public struct IsVisible : IEcsComponent
{

}

/// <summary>
/// finds all entities that are visible and have a RenderDescription, and generates render packets for them every frame.
/// </summary>
public class RenderPacketGenerationSystem : NotNot.Ecs.System
{
	EntityQuery positionQuery;
	StaticModelTechnique asset = new();
	protected override void OnInitialize()
	{
		base.OnInitialize();
		//create a query that selects all entities that have a Move and Translation component
		//for performance reasons this should be cached as a class member,
		positionQuery = entityManager.Query(new() { all = { typeof(WorldXform), typeof(IsVisible) },
			//only itterate entities that have a RenderDescription
			sharedComponentTypes = {typeof(RenderDescription) } });

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
			//get the shared components for this chunk
			var sharedComponents = meta[0].SharedComponents;
			//we already filtered the query to require a RenderDescription (above), so get it now
			var renderDescription = sharedComponents.Get<RenderDescription>();

			
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


			//loop through the render techniques and generate render packets for them.
			foreach (var iTechnique in renderDescription.techniques)
			{
				var renderPacket = new RenderPacket3d(iTechnique);
				renderPacket.instances = instances.AsReadMem();
				renderPacket.entityMetadata = meta;
				this.manager.engine.StateSync.EnqueueRenderPacket(renderPacket);
			}

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

