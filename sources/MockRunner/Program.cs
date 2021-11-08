global using NotNot;
global using System;
global using System.Collections;
using NotNot.Bcl;
using NotNot.Ecs;
using NotNot.Rendering;
using Raylib_cs;


//create basic engine
var engine = new Engine();
engine.Initialize();
//add renderer
engine.Rendering.AddChild(new NotNot.Rendering.RenderReferenceImplementationSystem());

//start
engine.Updater.Start();


//create a box mesh+material
var boxModel = new Batched3dModel()
{
	mesh=Raylib.GenMeshCube(1,1,1),
	material=Raylib.LoadMaterialDefault(),
};
unsafe
{
	boxModel.material.maps._As<MaterialMap>()[(int) MaterialMapIndex.MATERIAL_MAP_DIFFUSE].color=Color.LIME;
}


//create an archetype
var em = engine.DefaultWorld.entityManager;
var archetype = em.GetOrCreateArchetype(new()
{
	typeof(WorldXform),
	typeof(IsRenderable),
	typeof(Move),
	typeof(TestInput),
});

//create partition
var partitionGroup = PartitionGroup.GetOrCreate()

em.EnqueueCreateEntity(10, archetype, boxModel, (args) =>
{
	var (accessTokens, entityHandles, archetype) = args;
	foreach (var token in accessTokens)
	{
		ref var xform = ref token.GetComponentWriteRef<WorldXform>();
		xform = new WorldXform(){Position=__.Rand._NextVector3()*10};

	}

});
//asdfasd
//TODO:  modify EnqueueCreateEntity to take a generic TPartition where TPartition:class  and modify PartitionGroup to take normal objects (del interface)

