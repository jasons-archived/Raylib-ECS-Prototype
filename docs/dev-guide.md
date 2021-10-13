
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


# issues


# patterns
These are development pattern "tricks" needed to be used in this codebase.  It is likely that users of this framework need to use similar patterns.

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
If you work with unsafe code, you may wish to verify it does not have memory leaks/gc holes.  This can be verified by using GC Hole stress, as defined here:  https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/jit/investigate-stress.md#gc-hole-stress

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
  
