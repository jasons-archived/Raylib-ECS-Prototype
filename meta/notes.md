


- [Current status: DO NOT USE!](#current-status-do-not-use)
- [Overview](#overview)
  - [ECS](#ecs)
  - [ExecManager](#execmanager)
    - [Multithreaded by default](#multithreaded-by-default)
  - [goals](#goals)
  - [non-goals](#non-goals)
- [notes / scratch stuff below...](#notes--scratch-stuff-below)
  - [feature notes](#feature-notes)
  - [other private notes/scratch](#other-private-notesscratch)
  - [oss community notes](#oss-community-notes)
    - [funding](#funding)
    - [important feature needs:](#important-feature-needs)
    - [rendering?](#rendering)
      - [raylib](#raylib)
    - [Spatial partitioning notes](#spatial-partitioning-notes)
- [TODO:](#todo)
- [ecs review notes](#ecs-review-notes)



# Current status: DO NOT USE!
do no not not use it yet

still building first functional prototype.     



# Overview

## ECS
The ECS api is archetype based.

- Entities are dumb cattle, and query api reflects this.   Itteration over entities/components is done by array.
- Components may be classes.
- see [notnot/engine/ecs/readme.md](notnot/engine/ecs/readme.md) for more details

 

## ExecManager

Manages execution of "nodes" on top of it.  

Scheduling means:
- How often a node should execute
- What should the node wait for before executing
- What resources a node needs read/write access to



The ECS system will sit on top of this, and other things can too.

### Multithreaded by default
The execution engine is fully multithreaded.  Your nodes can be written in a single-threaded fashion as long as you specify what resource your node has read/write access to.


## goals
- Best FTDE (First-Time-Developer-Experience)
  - Steps needed to debug a sample project and full framework source should be only:
    1. Download Repository (may include Git LFS setup)
    2. Open RpgSample.sln
    3. Hit F5
    4. ... *wait a bit* ...
    5. Do stuff: Set Breakpoint / modify code+hotReload
- SLA (service level agreement)
   - 48hr rsponse time for issues.  Triage by 72hr mark.
   - 72hr response on PR requests edits.  (follow up, merge, reject)
   - monthly roadmap and changelist updates
- engine that runs on modern desktop platforms
- multithreaded.  take advantage of a 16 core system effectively.
- code based game development
- procedural workflows come first: dynamic objects, procedural scenes
- support world coordinates 1000 x 1000 km
- multiplayer by default
- modular systems
- documentation:  
  - Class and namespace summaries at minimum.
  - end-to-end example games
  - samples via unit tests
- open source via AGPL3, with commercial licensing, revenue split among contribs


## non-goals
- easy for beginner c# developers
- mobile or consoles
- computers incompatable with the tech choises made
- 2d games
- photorealism
- visual editor
- documentation
  - long-form text
  - stand-alone examples/samples
  - tutorial




- CPU Cache coherency/false sharing. 
   - see https://www.youtube.com/watch?v=WDIkqP4JbkE&t=247s
   - Problems arise only when all are true:
      - Independent values/variables fall on one cache line (64Bytes on x86/64)
      - Different cores concurrently access that line
      - Frequently
      - At least one is a writer
      - 
	  
	  
# notes / scratch stuff below...




Logical object structure is

- SomeExternalTickPump
  - ExecManager
    - World
      - Gameplay systems
    - RenderingSystem
    - 
## feature notes
- networking using either Steamworks or Epic Services
- messaging system: try out https://github.com/Cysharp/MessagePipe
- utils:  
  - DotNext?
  - Windows Community Toolkit, High Performance package: https://docs.microsoft.com/en-us/windows/communitytoolkit/high-performance/introduction
    - https://github.com/CommunityToolkit/WindowsCommunityToolkit
    - https://docs.microsoft.com/en-us/windows/communitytoolkit/nuget-packages
- physics: https://github.com/bepu/bepuphysics2
  - physics, intersection: https://www.realtimerendering.com/intersections.html

- math helper libs: Silk.net
  - - spatial maths: https://github.com/reeseschultz/ReeseUnityDemos/blob/master/Packages/com.reese.math/README.md#reeses-dots-math-extensions
- input and other platform libs: Silk.net
- kitbash:  https://kenney.nl/tools/assetforge and https://kenney.itch.io/kenshape
- map editor: https://trenchbroom.github.io/
- benchmarking: https://benchmarkdotnet.org/
- online codepen:
  - https://sharplab.io/
- linq: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/
   - https://github.com/jackmott/LinqFaster
   - https://github.com/asc-community/HonkPerf.NET
   - https://github.com/NetFabric/NetFabric.Hyperlinq
   - It looks like HyperLinQ is the best bet: https://github.com/asc-community/HonkPerf.NET/blob/main/Benchmarks/PoorestWidestBenchmarks.cs
- plinq: https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/introduction-to-plinq
- ascii drawing tools: https://asciiflow.com/#/
- github contributions CLA assistant:  https://github.com/cla-assistant/cla-assistant
- - dotnet API browser: https://source.dot.net/#System.Private.CoreLib/List.cs,39
- serialization: https://github.com/neuecc/MessagePack-CSharp
- gui
  - https://opengameart.org/content/ui-pack

- debugging
  - simPipeline debug/visualization
    - https://github.com/JamieDixon/GraphViz-C-Sharp-Wrapper
    - https://github.com/klassmann/libnoise-csharp


- timestep smoothing
  - https://blog.unity.com/technology/fixing-time-deltatime-in-unity-2020-2-for-smoother-gameplay-what-did-it-take
  - https://www.gafferongames.com/post/fix_your_timestep/


- perlin / noise generation
  - https://github.com/Auburn/FastNoiseLite



- free art assets
  - https://opengameart.org/content/ui-pack
  - 
## other private notes/scratch

- ecs system example
   - flecs https://github.com/SanderMertens/flecs
      - https://ajmmertens.medium.com/building-an-ecs-2-archetypes-and-vectorization-fe21690805f9
	  
	  
	  
## oss community notes

- blog
- discord
- Q&A / KB site  (stack exchange like)
- docs
- internal planning sites

- use github orgs (OSS)
- use contribution agreement like netfoundation
  - https://github.com/cla-assistant/github-action
  - https://github.com/cla-assistant/cla-assistant
  - https://github.com/Roblox/cla-signature-bot
- SLA for contributions
- **repository checklist**
  - https://gist.github.com/ZacharyPatten/08532b31ef5efc7593b32326b498023a
- project github badges: https://shields.io/category/downloads
### funding
- donations
	- patreon
	- github
	- https://www.oscollective.org/
	- dotnet foundation?
	- epic megagrant?
	- coinbase crypto
	- https://www.buymeacoffee.com/
- free credits
	- azure: https://cloudblogs.microsoft.com/opensource/2021/09/28/announcing-azure-credits-for-open-source-projects/
	
### important feature needs:
	- gizmos
	- plugin system (gizmo example)
	- asset store




### rendering?
a loaded topic.  there is scene management/spatial partitioning to consider.  camera, animation support
some potentail solutions

- Urho3d
  - https://urho3d.io/documentation/HEAD/index.html
  - https://github.com/xamarin/urho
- Ogre Next
  - https://github.com/OGRECave/ogre-next
  - ***DOES NOT SUPPORT C#***
- example silk based voxel rendering engine: https://github.com/Redhacker1/MinecraftClone-Core/tree/main/Engine
review these tutorials if need to do basic things like camera/frustum: https://github.com/gametutorials/tutorials/tree/master/OpenGL
- super low level projects, just in case:
  - https://github.com/mellinoe/veldrid


#### raylib
raylib seems most complete.  meaning least work to get a working renderer out-of-the-box for a "reference" renderer.
- RayLib / Raylib CS Bindings
   - https://github.com/ChrisDill/Raylib-cs/wiki/FAQ
   - https://www.raylib.com/examples.html
- raylib based debug frames.  **EXCELENT**
  - https://github.com/JeffM2501/TestFrame

### Spatial partitioning notes

- re monogame discord
  - prime31Role icon, Middleware — Today at 8:56 AM
    - Scene graph: https://github.com/prime31/Nez/blob/master/Nez.Portable/ECS/Scene.cs
    - Spatial partitioning: https://github.com/prime31/Nez/blob/master/Nez.Portable/Physics/SpatialHash.cs
  - Apos — Today at 8:59 AM
    - I have this one: https://github.com/Apostolique/Apos.Spatial/blob/main/Source/AABBTree.cs
    - Dcrew has this: https://github.com/DeanReynolds/Dcrew.Spatial/blob/master/src/Quadtree.cs
    - Based on this: https://github.com/RandyGaul/cute_framework/blob/master/src/cute_aabb_tree.cpp
    - btw, it's based on Randy's code, but his code is based on Box2D
         The Box2D creator has a presentation about it
         But our's has improvements
         Box2D has plans for other types of improvements but they aren't coded yet
         https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf
         One thing that's cool is that I ported the C code to C# and by doing so I found a bunch of bugs
         There are stuff that's undefined in C that C# doesn't allow as easily
    - This is what I do in my map editor: https://github.com/Apostolique/Apos.Editor/blob/8cf0cfdabe13629f2c42932090a2a57a68f4ba18/Game/Layer1/World.cs#L41-L48
      ```cs
      public void Draw(SpriteBatch s) {
         foreach (var e in Lilypads.Query(Camera.ViewRect).OrderBy(e => e))
               e.Draw(s);
         foreach (var e in Woods.Query(Camera.ViewRect).OrderBy(e => e))
               e.Draw(s);
         foreach (var e in Clouds.Query(Camera.ViewRect).OrderBy(e => e))
               e.Draw(s);
      }
      ```
- see also: https://github.com/Auios/Auios.QuadTree for object based
- other not really optimized math lib
  - https://github.com/gradientspace/geometry3Sharp
  - 

# TODO:



- need to create a custom MemoryOwner that clears ref types on dispose	
- Component Read/Write sentinels should just track when reads/writes are permitted.  if they occur outside of those times, assert.   This way we don't need to track who does all writes.
- //TODO: add expected cost of update metrics for current frame and past frames (to SimNode/Frame)
- SimNode Register/Unregister writes to the SimManager.nodeRegistry lookup.  This doesn't handle hiearchy chains added/removed, plus still need to discover the name somehow.  
  - maybe remove this.
  - and have node searches be expensive tree traversal
- 




valid ways of Adding a simNode
- to parent, regardless of if parent is attached yet
  - need a OnHierarchyAttached() / Detached() method
    - this sets the SimManager and registers to the simManager
- to SimManager, as long as specify parent by name
  - do this by passing parent name as parameter to manager.RegisterNode()



//deal with registration with SimManager
OnRegister()
OnUnregister()
IsRegistered


//deal with hiearchy
OnAdded()
OnRemoved()

handle MemoryOwner clear on dispose!
   and change the WriteMem.Allocate() method to take the option explicitly (no default)


Frame.FromPool() should recycle old frames, make chain for inspection of old state


# ecs review notes
- Systems Roots
  - start 
    - inputs
    - SYNC
      - updateFrame    
      - ecsDatabase
  - start
    - inputs
  - simulation
  - presentation
  - end


maths
- dot product:  (scalar product)
  - gives similarity
- cross product: (vector product)
  - gives normal
- game related libraries with maths:
  - https://github.com/tgjones/nexus
  - https://github.com/ykafia/stride/blob/master/sources/core/Stride.Core.Mathematics/Matrix.cs
  - https://github.com/RonenNess/MonoGame-SceneGraph
  - https://github.com/craftworkgames/MonoGame.Extended
  - 
  - 