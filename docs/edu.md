links for tangental educational stuff here.

- [gamedev](#gamedev)
  - [gamedev technical blogs](#gamedev-technical-blogs)
- [ECS](#ecs)
- [Timesteps](#timesteps)
- [Graphics Programming](#graphics-programming)
- [Business / Market analysis](#business--market-analysis)
- [technical writing](#technical-writing)
- [Project Management](#project-management)
- [dev](#dev)
  - [c# performance](#c-performance)
  - [multithreading patterns](#multithreading-patterns)
  - [OSS gamedev](#oss-gamedev)
  - [Native Interop (PInvoke)](#native-interop-pinvoke)
    - [calling pinvoke that takes a callback](#calling-pinvoke-that-takes-a-callback)
  - [nuget publishing](#nuget-publishing)
- [game ideas](#game-ideas)
- [game postmortums](#game-postmortums)
- [art](#art)

# gamedev
- general gamedev
  - Game Programming Patterns, free web book: https://gameprogrammingpatterns.com/
  - Write a game, not an engine.  https://geometrian.com/programming/tutorials/write-games-not-engines/
  - https://www.reddit.com/r/gameenginedevs/
  - good video series on building an engine https://www.reddit.com/user/GameEngineSeries
- https://github.com/OneLoneCoder/videos/tree/master/olcRolePlayingGame

- spatial stuff, A*, etc
  - https://simblob.blogspot.com/
  - https://www.redblobgames.com/grids/hexagons/
  - pathfinding and 2d spatial algorithms https://www.redblobgames.com/
  - 
## gamedev technical blogs
- https://ourmachinery.com/post/
# ECS
Some ECS Intro videos that are mostly applicable to !!.Ecs
- https://www.youtube.com/watch?v=OqzUr-Rg6w4
- https://www.youtube.com/watch?v=3r4aFWqXY_8
- rant about unity ecs: https://www.youtube.com/watch?v=SQ7GJFL8ttE
- 

# Timesteps
 - https://medium.com/@tglaiel/how-to-make-your-game-run-at-60fps-24c61210fe75
 - https://gafferongames.com/post/fix_your_timestep/
 - https://web.archive.org/web/20171206005813/http://www.slickentertainment.com/2016/06/




# Graphics Programming
- high promise
  - opengl introduction https://learnopengl.com/Getting-started/Shaders
  - shader editor: SHADERed
    - https://github.com/dfranx/SHADERed
    - https://shadered.org/
      - can import from https://www.shadertoy.com/ via plugin
- intro
  - http://alextardif.com/LearningGraphics.html
- theory
  - https://vksegfault.github.io/posts/gentle-intro-gpu-inner-workings/   inner workings of the modern gpu
- multithread rendering
  - https://vkguide.dev/docs/extra-chapter/multithreading/
  - https://www.youtube.com/watch?v=0nTDFLMLX9k
  - https://www.youtube.com/watch?v=v2Q_zHG3vqg
- shaders
  - https://developer.nvidia.com/fx-composer
  - https://github.com/lettier/3d-game-shaders-for-beginners  beginner guides to shader dev
  - shadertoy.com
    - tutorials at bottom of page
    - include discord channel
- maths
  - https://liorsinai.github.io/mathematics/2021/11/05/quaternion-1-intro.html
- raylib model and animation import
  - iqm animation raylib 4.0 example - Tutorial
    - https://www.youtube.com/watch?v=_EurjoraotA
    - https://github.com/lsalzman/iqm
  - 
# Business / Market analysis
- Market anaysis of Roblox https://www.fortressofdoors.com/so-you-want-to-compete-with-roblox/
  - https://news.ycombinator.com/item?id=28714779


# technical writing
- emoji lookup: https://gist.github.com/endolith/157796
- markdown book publishing
  - using rust: https://github.com/rust-lang/mdBook
    - example book using this: https://github.com/rustwasm/book
# Project Management
- OSS Repo Checklist: https://gist.github.com/ZacharyPatten/08532b31ef5efc7593b32326b498023a

# dev
## c# performance
  - Scott Meyers: Cpu Caches and Why You Care: https://www.youtube.com/watch?v=WDIkqP4JbkE&t=247s
  - dotnet book of the runtime: low level https://github.com/dotnet/coreclr/blob/master/Documentation/botr/README.md

- row major traversal of matrix vs column major traversal

## multithreading patterns
- `async`/`await` Synchronization Context
  - https://devblogs.microsoft.com/dotnet/configureawait-faq/
  - https://blog.stephencleary.com/2013/11/taskrun-etiquette-examples-dont-use.html
  - 
- **Channels** for Producer/Consumer pattern
  - https://ndportmann.com/system-threading-channels/
  - https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/

## OSS gamedev
- porting old xna games to raylib
  - https://gist.github.com/raysan5/bd8c0293d8b8da1e9e44d8ac435e9304
- algorithms resources / samples
  - https://github.com/SimonDarksideJ/XNAGameStudio/wiki
  - https://github.com/MonoGame/MonoGame.Samples
  - monogame discord, libraries channel
## Native Interop (PInvoke)
- calling native methods that take variable length args
  - dealing with __arglist: 
    - https://www.c-sharpcorner.com/UploadFile/b942f9/calling-unmanaged-functions-which-take-a-variable-number-of-arguments-from-C-Sharp/
    - http://dedjo.blogspot.com/2008/11/how-to-pinvoke-varargs-variable.html
- mixed mode debugging: https://stackoverflow.com/questions/40355753/how-to-debug-and-step-into-a-native-code-project-from-inside-a-net-core-c-sharp
- convert C++ to c# 
  - (100 lines at a time) https://www.tangiblesoftwaresolutions.com/product_details/cplusplus_to_csharp_converter_details.html
- choose what file to use for dll import at runtime: https://docs.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform#custom-import-resolver

### calling pinvoke that takes a callback
https://devblogs.microsoft.com/dotnet/improvements-in-native-code-interop-in-net-5-0/
https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedcallersonlyattribute?view=net-5.0

- Method 1 (old way)
```cs
//native function
[DllImport("raylib.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
public static extern void SetTraceLogCallback([NativeTypeName("TraceLogCallback")] delegate* unmanaged[Cdecl]<int, sbyte*, sbyte*, void> callback);

//my callback
    private static void LogCustom(int msgType, sbyte* text, sbyte* args)
    {

        Console.WriteLine("hi");
    }

//a delegate for the signature
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void LogCustomDelegate(int msgType, sbyte* text, sbyte* args);

//code to call native/do callback:
LogCustomDelegate doit = LogCustom;
SetTraceLogCallback(
(delegate* unmanaged[Cdecl] < int, sbyte *, sbyte *, void >)Marshal.GetFunctionPointerForDelegate(doit)
);
```
- Method 2 (new net5+ way)
```cs
//native function
[DllImport("raylib.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
public static extern void SetTraceLogCallback([NativeTypeName("TraceLogCallback")] delegate* unmanaged[Cdecl]<int, sbyte*, sbyte*, void> callback);

//my callback
[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	private static void LogCustom2(int msgType, sbyte* text, sbyte* args)
	{
		Console.WriteLine("hi2");
	}

//code to call native/do callback
SetTraceLogCallback(&LogCustom2);
```

## nuget publishing
- include pdb's in nuget package: ```<AllowedOutputExtensionsInPackageBuildOutputFolder>$(AllowedOutputExtensionsInPackageBuildOutputFolder);.pdb</AllowedOutputExtensionsInPackageBuildOutputFolder>```
  - as per https://stackoverflow.com/a/48391188



# game ideas
- minecraft clones
  - https://github.com/rswinkle/Craft/tree/portablegl
    - using portable gl: https://github.com/rswinkle/PortableGL



# game postmortums
  - l4d / b4b: left for dead / back for blood
    - Back 4 Blood proves Valve carried Left 4 Dead https://www.youtube.com/watch?v=EdRLNUGmFC8&t=343s
- dwarf fortress
  - https://stackoverflow.blog/2021/12/31/700000-lines-of-code-20-years-and-one-developer-how-dwarf-fortress-is-built/
  - https://news.ycombinator.com/item?id=29757875
  - https://if50.substack.com/p/2006-dwarf-fortress
- text adventure games: https://if50.substack.com/archive?sort=new
- tradewars:
  - https://if50.substack.com/p/1991-trade-wars-2002



# art
  - svg editor online 
    - https://minimator.app/#/project/0
    - http://100r.co/site/dotgrid.html
      - web version: https://hundredrabbits.github.io/Dotgrid/
    - 