global using NotNot;
global using System;
global using System.Collections;
using NotNot.Bcl;
using NotNot.Ecs;
using NotNot.Rendering;
using Raylib_cs;
using System.Threading.Tasks;


//create basic engine
var engine = new Engine();
engine.Initialize();
//add renderer
engine.Rendering.AddChild(new NotNot.Rendering.RenderReferenceImplementationSystem());
//add system to apply movement (to worldXform)
engine.DefaultWorld.Phase2_Simulation.AddChild(new MoveSystem());
//add a system to simulate input (provide movement)
engine.DefaultWorld.Phase2_Simulation.AddChild(new TestInputSystem());
//add system to generate render packets
engine.DefaultWorld.Phase2_Simulation.AddChild(new RenderPacketGenerationSystem());

//start
engine.Updater.Start();


//create a box mesh+material used for rendering
var boxModel = new BatchedModelTechnique();
//init gfx via a callback.  this is needed because opengl only runs single threaded
boxModel.OnInitialize=(_this)=>
{
	_this.mesh = Raylib.GenMeshCube(1, 1, 1);
	_this.material = Raylib.LoadMaterialDefault();
	unsafe
	{
		boxModel.material.maps._As<MaterialMap>()[(int)MaterialMapIndex.MATERIAL_MAP_DIFFUSE].color = Color.LIME;
	}
};
var renderDescription = new RenderDescription() { techniques = { boxModel } };

//create an archetype
var em = engine.DefaultWorld.entityManager;
var archetype = em.GetOrCreateArchetype(new()
{
	typeof(WorldXform),
	typeof(IsVisible),
	typeof(Move),
	typeof(TestInput),
});

//create sharedComponents used used to bucket entities with
var sharedComponents = SharedComponentGroup.GetOrCreate(renderDescription);

//create entities using the sharedComponent
em.EnqueueCreateEntity(100, archetype, sharedComponents, (args) =>
{
	var (accessTokens, entityHandles, archetype) = args;
	foreach (var token in accessTokens)
	{
		ref var xform = ref token.GetComponentWriteRef<WorldXform>();
		xform = new WorldXform(){Position=__.Rand._NextVector3()*10};
	}
});



await Task.WhenAny(engine.RunningTask, Task.Delay(100000000));
await engine.Updater.Stop();





//asdfasd
//TODO:  modify EnqueueCreateEntity to take a generic TPartition where TPartition:class  and modify PartitionGroup to take normal objects (del interface)

