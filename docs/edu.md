links for tangental educational stuff here.



# gamedev
- spatial stuff, A*, etc
  - https://simblob.blogspot.com/
  - https://www.redblobgames.com/grids/hexagons/
  - 

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

# Business / Market analysis
- Market anaysis of Roblox https://www.fortressofdoors.com/so-you-want-to-compete-with-roblox/
  - https://news.ycombinator.com/item?id=28714779


# technical writing
- emoji lookup: https://gist.github.com/endolith/157796

# Project Management
- OSS Repo Checklist: https://gist.github.com/ZacharyPatten/08532b31ef5efc7593b32326b498023a

# dev
## c# performance
  - Scott Meyers: Cpu Caches and Why You Care: https://www.youtube.com/watch?v=WDIkqP4JbkE&t=247s


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
- https://gist.github.com/raysan5/bd8c0293d8b8da1e9e44d8ac435e9304

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