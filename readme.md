


- [NotNot?](#notnot)
- [Current status: DO NOT USE!](#current-status-do-not-use)
- [Overview](#overview)
  - [ECS](#ecs)
  - [ExecManager](#execmanager)
    - [Multithreaded by default](#multithreaded-by-default)
  - [goals](#goals)
  - [non-goals](#non-goals)
- [required knowledge](#required-knowledge)
  - [c](#c)
  - [architecture](#architecture)
- [patterns](#patterns)
- [antipaterns](#antipaterns)
- [design guidelines](#design-guidelines)
  - [Naming](#naming)
  - [Error/Test handling](#errortest-handling)
  - [Git Repository](#git-repository)
- [testing and verificaiton](#testing-and-verificaiton)
  - [unsafe code verification](#unsafe-code-verification)
    - [c# tricks/notes/perf](#c-tricksnotesperf)
- [issues](#issues)
- [notes / scratch stuff below...](#notes--scratch-stuff-below)
  - [feature notes](#feature-notes)
  - [other private notes/scratch](#other-private-notesscratch)
  - [oss community notes](#oss-community-notes)
    - [funding](#funding)
    - [important feature needs:](#important-feature-needs)
    - [rendering?](#rendering)
    - [Spatial partitioning notes](#spatial-partitioning-notes)
- [TODO:](#todo)

# NotNot?
It's not not an engine.

This is an **execution engine** and an **ecs** Framework.  Not library.  You must build your game on top of, not next to this.

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
  - Steps needed to debug a sample project and full engine soure should be only:
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



# required knowledge

## c#
- understanding of `return ref` and passing structs by reference.  Prerequisie is good understanding of value types vs reference types.
- understanding of `Span<T>` and how it is a pointer to memory somewhere else
- `async`/`await` and `Task` patterns.  
  
## architecture
- general knowledge of Entity Component Systems.  
  - The concept of components as resources having read/write locks, allowing coordination between multiple threads.
  - The concept that each System you create is effectively it's own thread.
  - Entity creation/deletion occurs at the start of each frame, between entity/component access by systems.
  - A great strategy is to spin off your own `async Task` in a System to do long-running work that is independent of other components/systems, with a sync point in a future `.Update()` method call.

# patterns
These are development pattern "tricks" needed to be used in this codebase.  It is likely that users of this engine need to use similar patterns.

- cast input `ref` struct to ptr to circumvent `return ref` type checks.   Only use this when you ***KNOW*** the function is safe, but here's how:
```cs
	public unsafe ref readonly TComponent GetReadRef(AllocToken allocToken)
	{
		return ref GetReadRef(ref *&allocToken); //cast to ptr using *& to circumvent return ref safety check
	}
```
- for multithreading, use `async/await`.  do not use thread blocking such as `Thread.Sleep()`.  The two ways of thread synchronization are not compatable.
   -  this goes with synchronization objects too.   Do not use `lock`, use `SemephoreSlim`.  Do not use `ReaderWriterLockSlim`, use `AsyncReaderWriterLockSlim` from `DotNext.Threading`.
- for `MemoryOwner<T>` and `SpanOwner<T>` be suer to use them with a `using` clause, or to `Dispose()` them when done.  This tells the system when you are done using them.  This is especially ***CRITICALLY important for `SpanOwner<T>` otherwise it will cause a memory leak***.

# antipaterns
These are patterns that have known problems.  Do not use them, ever.
- Do not use `Task.Wait(timeout)` for any timeout greater than zero.  It relies on a OS timer that on windows has a resolution of `15ms`, meaning that your block will always be at least `15ms`.
- `lock` or `Thread.Sleep()`.  See the above pattern on using `async/await` instead.



# design guidelines

Generally following the [standard dotnet design guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/) with the following differences:

## Naming
1. WIP is put in files prefixed with an underscore, such as `_utils.cs`.  These will be put into proper `Namespace/Class.cs` layouts when the feature is done/stable.
   - this lets the working dev have more flexibility during design/implementation, and lets other devs know that this is a work-in-progress section.
   - when code leaves this WIP file, it should be properly documented (all classes and members should have intellisense)
1. member fields prefixed with `_` and first letter lowercase. 
   - prefixing makes it easy to distinguish between local and member variables.   
   - using a modern IDE you can ignore the "cost" when doing discovery (intelisense dropdown, autocomplete)
1. Prefix Class Properties/Methods with `DANGER` if is it's returned value can result in undefined behavior.  
   - This is chiefly used for pointer based `unsafe` computations where "bad input" may not result in an exception thrown, but instead results in corrupted data being returned.
3. prefix Class Properties/Methods meant for "internal use only" with `_`
   - even `protected internal` members are visible to derived classes.  prefixing with an underscore signals to the user that these are not meant for common workflow scenarios.
4. Prefix important/commonly used globals with `__` such as `__DEBUG.Assert()`
   - same reasoning as the member field `_` prefix shown above.
5. Prefix ***very*** special classes/members with ``__WHAT_`, where `WHAT` is the special thing.
   - for example, conditional unit test entrypoints might be named `__TEST_Unit()` or maybe `__UNITTEST_GcVerification()` or `__UNITTEST_RunAll()`
6. Prefix extension methods with `_` and if there is anything unusual about them, add a suffix like `_Unsafe` to give a hint to the users.  
   - prefixing allows easy identification as a custom extension method without impacting autocomplete/intelisense.
   - if using a suffix, Add approriate intellisense docs (to the containing static class at minimum) so the user can understand the meaning of the suffix.
     - `_Unsafe` is used for code that has some tricky usage pattern, usuall because of using pointers (unsafe code) that will go out of scope once the underlying object changes.  Generally speaking, use these method results immediately and do not make changes (add/removes for collections) until afterwards.
     - `_Exp` is used for experimental code that seems to work fine, but might have some hidden performance "gotcha" or relies on some experimental feature that might change/break/be-deleted in the future.
7. Prefix extension method containing static classes with `zz_Extensions_` 
   - so that it doesn't polute intellisense dropdowns, and is still descriptive.
1. add the `[ThreadSafe]` attribute for classes that are thread safe for certain situations.   
   - For example thread safe for Add,ReadExisting use `[ThreadSafe(ThreadSituation.Add,ThreadSituation.ReadExisting)]`.
   - This doesn't do anything except add a little documentation for now.   Later we can add attribute based runtime verification.

it would be good to also follow these: 
- https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md  which we also mostly follow.
- https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md 
## Error/Test handling
1. use `#CHECKED` for costly code/data/usage verifications.  use `#DEBUG` for helpful hints about mistakes being made.  use `#TEST` for inline testing code
   - This allows turning on/off any combination to get desired runtime effect.
   - be sure to not mix these in a way that leads to broken code when one or more is disabled.
1. inline tests are okay, but should not block the main thread (run them via `Task.Run()`) and should not run every frame of execution.  
   - Generally run them once on first use, and be sure the cost is optimized away if not using `#TEST` builds
   - For an example pattern where you want to run the test periotically (checking for GC race conditions for example) see `StructArray100<T>`
1. If the user does something bad that is going to mess up the execution or results in not doing what the user intends, throw an exception via `__ERROR.Throw()` 
   - throwing a specific/custom Exception class is okay too, I guess?  
   - If you don't have a specific/custom exception class, prefer `_ERROR.Throw()` over `throw new Exception()` because I think it provides better customization later.


## Git Repository
`main` is the stable work branch.  individual features to be put in their own branches, as are bugfixes, being merged back into main when done.  releases will be in the `release` branch with appropriate tagging for version.


# testing and verificaiton

## unsafe code verification
any work done to unsafe code should be verified by using GC Hole stress, as defined here:  https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/jit/investigate-stress.md#gc-hole-stress

- Specifically, add the `DOTNET_GCStress=0xF` to launchsettings.json  (can be created via the startup project properties window)





- 
### c# tricks/notes/perf
- use record structs for comparison/lookups: https://nietras.com/2021/06/14/csharp-10-record-struct/
- use `MemoryOwner<T>` for shared pool objects
- `ReadOnlySpan<T>` and `Span<T>` are treated special by the CLR, it tracks their lifetimes and ensures references are valid
   - also these enjoy other special things like casting from pointers or `stackalloc`
- the Stack is only about  `1mb` so only use `stackalloc` for small temp allocations.   anything bigger use `MemoryOwner<T>`
- on a `x64` win10 machine, a memorypage is `4096` bytes.  
- for making instances of generic types:    `var listType = typeof(List<>).MakeGenericType(yourType)` and `Activator.CreateInstance(listType)`
- Need to get some tips on some random c# feature?  https://goalkicker.com/DotNETFrameworkBook/DotNETFrameworkNotesForProfessionals.pdf
  - also an algorithms book, for things like A* https://goalkicker.com/AlgorithmsBook/
  


# issues

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
  - 
- math helper libs: Silk.net
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
- RayLib / Raylib CS Bindings
   - https://github.com/ChrisDill/Raylib-cs/wiki/FAQ
   - https://www.raylib.com/examples.html
- Urho3d
  - https://urho3d.io/documentation/HEAD/index.html
  - https://github.com/xamarin/urho
- Ogre Next
  - https://github.com/OGRECave/ogre-next
  - ***DOES NOT SUPPORT C#***
- example silk based voxel rendering engine: https://github.com/Redhacker1/MinecraftClone-Core/tree/main/Engine
review these tutorials if need to do basic things like camera/frustum: https://github.com/gametutorials/tutorials/tree/master/OpenGL



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