using System.Numerics;
using BepuPhysics;
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine.Ecs;
using NotNot.Engine.Ecs.Allocation;

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


		playerMoveQuery =entityManager.Query(new() { All = {typeof(PlayerInput), typeof(Move) } });

		RegisterWriteLock<Move>();

	}




	protected override async Task Update()
	{
		playerMoveQuery.Run((ReadMem<EntityMetadata> meta,Mem<Move> moves,ReadMem<PlayerInput> players) => {

			var metaSpan = meta.Span;
			for(var i = 0; i < meta.length; i++)
			{
				__ERROR.Throw(metaSpan[i].IsAlive, "why dead being enumerated?  we are forcing autopack");
				moves[i].value =Vector3.Normalize(__.Rand._NextVector3());
			}

		});
	}
}
/// <summary>
/// This example simply takes a "move component" vector3 and adds it to the position "translation component" vector3
/// </summary>
public class MoveSystem: NotNot.Engine.Ecs.System
{
	EntityQuery moveQuery;
	protected override void OnInitialize()
	{
		base.OnInitialize();
		//create a query that selects all entities that have a Move and Translation component
		//for performance reasons this should be cached as a class member,
		moveQuery = entityManager.Query(new() { All = { typeof(Move), typeof(Translation) } } );

		//notify our need for read/write access so systems can be multithreaded safely
		RegisterWriteLock<Translation>();
		RegisterReadLock<Move>();
		
	}

	protected override async Task Update()
	{
		//run the query, selecting all entities and doing work on them.
		moveQuery.Run((
			//metadata about the entity
			ReadMem<EntityMetadata> meta,
			//write access to Translation component
			Mem<Translation> translations,
			//read access to Move component
			ReadMem<Move> moves
			) => {

			for (var i = 0; i < meta.length; i++)
			{
				//apply move vector onto translation vector
				translations[i].value+=moves[i].value;
				Console.WriteLine($"entity={meta[i]}, pos={translations[i].value}, move={moves[i].value}");
			}

		});
	}
}


public class TestGame
{
	public void Run()
	{
		var engine = new Engine.Engine();
		engine.Updater = new NotNot.Engine.HeadlessUpdater();
		engine.Initialize();
		//engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });

		engine.DefaultWorld.AddChild(new MoveSystem());
		engine.DefaultWorld.AddChild(new PlayerInputSystem());




		engine.Updater.Start();

		var em = engine.DefaultWorld.entityManager;

		var archetype = em.GetOrCreateArchetype(new() { typeof(PlayerInput), typeof(Translation), typeof(Move) });

		em.EnqueueCreateEntity(1, archetype, (args) => {
			var (accessTokens, entityHandles, archetype) = args;
			foreach (var accessToken in accessTokens)
			{
				ref var translation = ref accessToken.GetComponentWriteRef<Translation>();
				ref var move = ref accessToken.GetComponentWriteRef<Move>();
				translation = new Translation() { value = Vector3.One };
				move = new() { value = Vector3.Zero };
			}
		});



	}

}

