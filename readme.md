


- [DumDum?](#dumdum)
- [Current status](#current-status)
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
- [notes / scratch stuff below...](#notes--scratch-stuff-below)
    - [feature notes](#feature-notes)
    - [c# tricks/notes/perf](#c-tricksnotesperf)
- [TODO:](#todo)

# DumDum?
A codename?

This is an **execution engine** and an **ecs** framework.  Not library.  You must build your game on top of, not next to this.

# Current status

still building first functional prototype.   



# Overview

## ECS
The ECS api is heavily inspired by Unity ECS.  very similar api surface.

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
- networking built in
- modular systems
- documentation:  
  - Class and namespace summaries at minimum.
  - end-to-end example games
  - samples via unit tests
- open source via AGPL3, with commercial licensing


## non-goals
- easy for beginner c# developers
- mobile or consoles
- computers incompatable with the tech choises made
- 2d games
- photorealism
- physical realism
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
- general knowledge of Entity Component Systems.  If you read/watch some intro on Unity ECS that is a good start.
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
1. prefix Class Properties/Methods meant for "internal use only" with `_`
   - even `protected internal` members are visible to derived classes.  prefixing with an underscore signals to the user that these are not meant for common workflow scenarios.
2. Prefix important/commonly used globals with `__` such as `__DEBUG.Assert()`
   - same reasoning as the member field `_` prefix shown above.
3. Prefix ***very*** special classes/members with ``__WHAT_`, where `WHAT` is the special thing.
   - for example, conditional unit test entrypoints might be named `__TEST_Unit()` or maybe `__UNITTEST_GcVerification()` or `__UNITTEST_RunAll()`
4. Prefix extension methods with `_` and if there is anything unusual about them, add a suffix like `_Unsafe` to give a hint to the users.  
   - prefixing allows easy identification as a custom extension method without impacting autocomplete/intelisense.
   - if using a suffix, Add approriate intellisense docs (to the containing static class at minimum) so the user can understand the meaning of the suffix.
     - `_Unsafe` is used for code that has some tricky usage pattern, usuall because of using pointers (unsafe code) that will go out of scope once the underlying object changes.  Generally speaking, use these method results immediately and do not make changes (add/removes for collections) until afterwards.
     - `_Exp` is used for experimental code that seems to work fine, but might have some hidden performance "gotcha" or relies on some experimental feature that might change/break/be-deleted in the future.
5. Prefix extension method containing static classes with `zz_Extensions_` 
   - so that it doesn't polute intellisense dropdowns, and is still descriptive.

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





# notes / scratch stuff below...




Logical object structure is

- SomeExternalTickPump
  - ExecManager
    - World
      - Gameplay systems
    - RenderingSystem
    - 
### feature notes
- networking using either Steamworks or Epic Services
- messaging system: try out https://github.com/Cysharp/MessagePipe
- utils:  
  - DotNext?
  - Windows Community Toolkit, High Performance package: https://docs.microsoft.com/en-us/windows/communitytoolkit/high-performance/introduction
    - https://github.com/CommunityToolkit/WindowsCommunityToolkit
    - https://docs.microsoft.com/en-us/windows/communitytoolkit/nuget-packages
- physics: https://github.com/bepu/bepuphysics2
- math helper libs: Silk.net
- input and other platform libs: Silk.net
- kitbash:  https://kenney.nl/tools/assetforge and https://kenney.itch.io/kenshape
- benchmarking: https://benchmarkdotnet.org/
- online codepen:
  - https://sharplab.io/
- linq: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/
- plinq: https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/introduction-to-plinq
- 
### c# tricks/notes/perf
- use record structs for comparison/lookups: https://nietras.com/2021/06/14/csharp-10-record-struct/
- use `MemoryOwner<T>` for shared pool objects
- `ReadOnlySpan<T>` and `Span<T>` are treated special by the CLR, it tracks their lifetimes and ensures references are valid
   - also these enjoy other special things like casting from pointers or `stackalloc`
- the Stack is only about  `1mb` so only use `stackalloc` for small temp allocations.   anything bigger use `MemoryOwner<T>`
- on a `x64` win10 machine, a memorypage is `4096` bytes.  
- for making instances of generic types:    `var listType = typeof(List<>).MakeGenericType(yourType)` and `Activator.CreateInstance(listType)`


# TODO:

- replace `Allocator` direct List indexer calls with `Span`
- Free Chunk should only happen when 2 full chunks are empty, to prevent thrashing
  - put the free in a lazy slot.  preallocate async.
  - `Parallel.ForEach(_columns, (columnList, loopState) ` line in Allocator.Free() should be changed to __.ForRange() workflow