// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System.Runtime.CompilerServices;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
using static Raylib_CsLo.ConfigFlags;
using static Raylib_CsLo.Color;
using static Raylib_CsLo.CameraProjection;
using static Raylib_CsLo.ShaderLocationIndex;
using static Raylib_CsLo.ShaderUniformDataType;
using static Raylib_CsLo.MaterialMapIndex;
using static Raylib_CsLo.CameraMode;
using static Raylib_CsLo.KeyboardKey;
using System.Runtime;
using System.Runtime.InteropServices;

namespace RaylibScratch;




public class RenderSystem
{
	System.Drawing.Size screenSize = new(1920, 1080);
	string windowTitle;

	public static Camera3D camera = new()
	{
		position = new Vector3(0.0f, 10.0f, 10.0f), // Camera3D position
		target = new Vector3(0.0f, 0.0f, 0.0f),      // Camera3D looking at point
		up = new Vector3(0.0f, 1.0f, 0.0f),          // Camera3D up vector (rotation towards target)
		fovy = 45.0f,                             // Camera3D field-of-view Y
		projection_ = CameraProjection.CAMERA_PERSPECTIVE,                   // Camera3D mode type
	};

	private Nito.AsyncEx.AsyncContextThread _renderThread;
	public Task renderTask;
	public void Start()
	{
		_renderThread = new Nito.AsyncEx.AsyncContextThread();
		renderTask = _renderThread.Factory.Run(_RenderThread_Worker);
		//GetGamepadName(0);

	}

	
	//public int mtId;

	public AsyncAutoResetEvent renderGate = new(true);
	public async Task _RenderThread_Worker()
	{
		Console.WriteLine("render thread start");
		//Raylib_CsLo.CsLoSettings.config.OpenGl43 = true;

		//init
		Raylib.InitWindow(screenSize.Width, screenSize.Height, windowTitle);
		var technique = new ModelTechnique();
		technique.Init();

		var rand = new Random();
		var renderPacket = new RenderPacket3d();
		renderPacket.instances = new Matrix4x4[100];
		for (var i = 0; i < renderPacket.instances.Length; i++)
		{
			renderPacket.instances[i] = Matrix4x4.Identity;
			renderPacket.instances[i].Translation = new Vector3(rand.NextSingle() - 0.5f, rand.NextSingle() - 0.5f, rand.NextSingle() - 0.5f) * 10;
		}
		//to try:
		// LowLatency Mode
		//[SuppressGCTransition]
		//GC.Pause() before enddraw
		//increase time of main loop, measure
		//sync main loop


		Thread.BeginThreadAffinity();
		Thread.CurrentThread.Priority = ThreadPriority.Highest;

		Raylib.SetTargetFPS(120);
		Raylib.SetWindowState(ConfigFlags.FLAG_VSYNC_HINT);
		var swElapsed = Stopwatch.StartNew();
		var swTotal = Stopwatch.StartNew();
		//render loop

		var swEndDraw = new Stopwatch();
		var swLoop = new Stopwatch();
		var loopCount = 0;
		var framesCounter = 0;
		while (!Raylib.WindowShouldClose())
		{
			swLoop.Restart();
			loopCount++;
			//renderGate.Set();

			var elapsed = (float)swElapsed.Elapsed.TotalSeconds;
			swElapsed.Restart();
			var totalTime = (float)swTotal.Elapsed.TotalSeconds;
















			//await Task.Delay(rand.Next(10));

			//Console.WriteLine($"_RenderThread_Worker AFINITY.  cpuId={Thread.GetCurrentProcessorId()}, mtId={Thread.CurrentThread.ManagedThreadId}");
			//mtId = Thread.CurrentThread.ManagedThreadId;

			_UpdateRenderPacket(renderPacket, elapsed, totalTime);

			//draw
			Raylib.BeginDrawing();
			Raylib.ClearBackground(RAYWHITE);
			Raylib.BeginMode3D(camera);













			// Update
			//-----------------------------------------------------
			if (IsKeyPressed(KEY_F)) ToggleFullscreen();  // modifies window size when scaling!

			if (IsKeyPressed(KEY_R))
			{
				if (IsWindowState(FLAG_WINDOW_RESIZABLE)) ClearWindowState(FLAG_WINDOW_RESIZABLE);
				else SetWindowState(FLAG_WINDOW_RESIZABLE);
				
			}

			if (IsKeyPressed(KEY_D))
			{
				if (IsWindowState(FLAG_WINDOW_UNDECORATED)) ClearWindowState(FLAG_WINDOW_UNDECORATED);
				else SetWindowState(FLAG_WINDOW_UNDECORATED);
			}

			if (IsKeyPressed(KEY_H))
			{
				if (!IsWindowState(FLAG_WINDOW_HIDDEN)) SetWindowState(FLAG_WINDOW_HIDDEN);

				framesCounter = 0;
			}

			if (IsWindowState(FLAG_WINDOW_HIDDEN))
			{
				framesCounter++;
				if (framesCounter >= 240) ClearWindowState(FLAG_WINDOW_HIDDEN); // Show window after 3 seconds
			}

			if (IsKeyPressed(KEY_N))
			{
				if (!IsWindowState(FLAG_WINDOW_MINIMIZED)) MinimizeWindow();

				framesCounter = 0;
			}

			if (IsWindowState(FLAG_WINDOW_MINIMIZED))
			{
				framesCounter++;
				if (framesCounter >= 240) RestoreWindow(); // Restore window after 3 seconds
			}

			if (IsKeyPressed(KEY_M))
			{
				// NOTE: Requires FLAG_WINDOW_RESIZABLE enabled!
				if (IsWindowState(FLAG_WINDOW_MAXIMIZED)) RestoreWindow();
				else MaximizeWindow();
			}

			if (IsKeyPressed(KEY_U))
			{
				if (IsWindowState(FLAG_WINDOW_UNFOCUSED)) ClearWindowState(FLAG_WINDOW_UNFOCUSED);
				else SetWindowState(FLAG_WINDOW_UNFOCUSED);
			}

			if (IsKeyPressed(KEY_T))
			{
				if (IsWindowState(FLAG_WINDOW_TOPMOST)) ClearWindowState(FLAG_WINDOW_TOPMOST);
				else SetWindowState(FLAG_WINDOW_TOPMOST);
			}

			if (IsKeyPressed(KEY_A))
			{
				if (IsWindowState(FLAG_WINDOW_ALWAYS_RUN)) ClearWindowState(FLAG_WINDOW_ALWAYS_RUN);
				else SetWindowState(FLAG_WINDOW_ALWAYS_RUN);
			}

			if (IsKeyPressed(KEY_V))
			{
				if (IsWindowState(FLAG_VSYNC_HINT)) ClearWindowState(FLAG_VSYNC_HINT);
				else SetWindowState(FLAG_VSYNC_HINT);
			}
			DrawText(TextFormat("Screen Size: [%i, %i]", GetScreenWidth(), GetScreenHeight()), 10, 40, 10, GREEN);

			// Draw window state info
			DrawText("Following flags can be set after window creation:", 10, 60, 10, GRAY);
			if (IsWindowState(FLAG_FULLSCREEN_MODE)) DrawText("[F] FLAG_FULLSCREEN_MODE: on", 10, 80, 10, LIME);
			else DrawText("[F] FLAG_FULLSCREEN_MODE: off", 10, 80, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_RESIZABLE)) DrawText("[R] FLAG_WINDOW_RESIZABLE: on", 10, 100, 10, LIME);
			else DrawText("[R] FLAG_WINDOW_RESIZABLE: off", 10, 100, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_UNDECORATED)) DrawText("[D] FLAG_WINDOW_UNDECORATED: on", 10, 120, 10, LIME);
			else DrawText("[D] FLAG_WINDOW_UNDECORATED: off", 10, 120, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_HIDDEN)) DrawText("[H] FLAG_WINDOW_HIDDEN: on", 10, 140, 10, LIME);
			else DrawText("[H] FLAG_WINDOW_HIDDEN: off", 10, 140, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_MINIMIZED)) DrawText("[N] FLAG_WINDOW_MINIMIZED: on", 10, 160, 10, LIME);
			else DrawText("[N] FLAG_WINDOW_MINIMIZED: off", 10, 160, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_MAXIMIZED)) DrawText("[M] FLAG_WINDOW_MAXIMIZED: on", 10, 180, 10, LIME);
			else DrawText("[M] FLAG_WINDOW_MAXIMIZED: off", 10, 180, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_UNFOCUSED)) DrawText("[G] FLAG_WINDOW_UNFOCUSED: on", 10, 200, 10, LIME);
			else DrawText("[U] FLAG_WINDOW_UNFOCUSED: off", 10, 200, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_TOPMOST)) DrawText("[T] FLAG_WINDOW_TOPMOST: on", 10, 220, 10, LIME);
			else DrawText("[T] FLAG_WINDOW_TOPMOST: off", 10, 220, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_ALWAYS_RUN)) DrawText("[A] FLAG_WINDOW_ALWAYS_RUN: on", 10, 240, 10, LIME);
			else DrawText("[A] FLAG_WINDOW_ALWAYS_RUN: off", 10, 240, 10, MAROON);
			if (IsWindowState(FLAG_VSYNC_HINT)) DrawText("[V] FLAG_VSYNC_HINT: on", 10, 260, 10, LIME);
			else DrawText("[V] FLAG_VSYNC_HINT: off", 10, 260, 10, MAROON);

			DrawText("Following flags can only be set before window creation:", 10, 300, 10, GRAY);
			if (IsWindowState(FLAG_WINDOW_HIGHDPI)) DrawText("FLAG_WINDOW_HIGHDPI: on", 10, 320, 10, LIME);
			else DrawText("FLAG_WINDOW_HIGHDPI: off", 10, 320, 10, MAROON);
			if (IsWindowState(FLAG_WINDOW_TRANSPARENT)) DrawText("FLAG_WINDOW_TRANSPARENT: on", 10, 340, 10, LIME);
			else DrawText("FLAG_WINDOW_TRANSPARENT: off", 10, 340, 10, MAROON);
			if (IsWindowState(FLAG_MSAA_4X_HINT)) DrawText("FLAG_MSAA_4X_HINT: on", 10, 360, 10, LIME);
			else DrawText("FLAG_MSAA_4X_HINT: off", 10, 360, 10, MAROON);



























			//draw renderpackets
			technique.DoDraw(renderPacket);
			//await DoDraw(technique, renderPacket);

			Raylib.DrawGrid(100, 1.0f);
			Raylib.EndMode3D();
			//Raylib.DrawText("Reference Rendering", 10, 40, 20, Color.DARKGRAY);
			Raylib.DrawFPS(10, 10);

			swEndDraw.Restart();

			{
				//renderGate.Set();
				//GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				//GC.Collect();
				//GC.WaitForPendingFinalizers();
				var current = GCSettings.LatencyMode;
				GCSettings.LatencyMode = GCLatencyMode.LowLatency;
				//var crit = GC.TryStartNoGCRegion(100000000);
				{
					//Raylib.EndDrawing();
					//Thread.BeginCriticalRegion();
					EndDrawing();
					//Thread.EndCriticalRegion();
				}
				//if (crit)
				//{
				//	GC.EndNoGCRegion();
				//}
				GCSettings.LatencyMode = current;
			}
			renderGate.Set();
			var endDrawElapsed = swEndDraw.ElapsedMilliseconds;
			var loopElapsed = swLoop.ElapsedMilliseconds;
			
			if (loopCount % 1000 == 0 || loopElapsed > 50)
			{
				Console.WriteLine($"loop={(int)(loopElapsed-endDrawElapsed)}  endDraw={(int)endDrawElapsed}  GC={GCInfo.Get()}  cpuId={Thread.GetCurrentProcessorId()}, mtId={Thread.CurrentThread.ManagedThreadId}");
			}
		}

		Thread.EndThreadAffinity();
		renderGate.Set();
		Raylib.CloseWindow();
		Console.WriteLine("render thread done");
	}

	private async Task DoDraw(ModelTechnique technique, RenderPacket3d renderPacket)
	{
		technique.DoDraw(renderPacket);
		await Task.Delay(0);
	}
	///// <summary>End canvas drawing and swap buffers (double buffering)</summary>
	//[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
	////[System.Runtime.]
	////[SuppressGCTransition]
	//public static extern void EndDrawing();


	private void _UpdateRenderPacket(RenderPacket3d renderPacket, float elapsed, float totalTime)
	{
		totalTime *= 2;

		//move objects
		for (var i = 0; i < renderPacket.instances.Length; i++)
		{

			ref var currXform = ref renderPacket.instances[i];
			var currentPos = currXform.Translation;
			currXform = Matrix4x4.CreateFromYawPitchRoll(0, 0, i + totalTime);
			currXform.Translation = currentPos + new Vector3(MathF.Sin(totalTime), 0f, MathF.Cos(totalTime)) * elapsed * 3;
		}

	}


}

public class ModelTechnique
{

	public Mesh mesh;
	public Shader shader;
	public Material material;
	//public 
	public void Init()
	{

		//shader
		{
			shader = Raylib.LoadShader("resources/shaders/glsl330/base_lighting.vs",
				"resources/shaders/glsl330/lighting.fs"); //TODO: move to a global

			// Get some shader loactions
			unsafe
			{
				int* locs = (int*)shader.locs;
				locs[(int)SHADER_LOC_MATRIX_MVP] = GetShaderLocation(shader, "mvp");
				locs[(int)SHADER_LOC_VECTOR_VIEW] = GetShaderLocation(shader, "viewPos");
				locs[(int)SHADER_LOC_MATRIX_MODEL] = GetShaderLocationAttrib(shader, "instanceTransform");
			}

			// Ambient light level
			int ambientLoc = GetShaderLocation(shader, "ambient");

			Raylib.SetShaderValue(shader, ambientLoc, new float[] { 0.2f, 0.2f, 0.2f, 1.0f }, SHADER_UNIFORM_VEC4);


			//Rlights.CreateLight(0, LightType.LIGHT_DIRECTIONAL, new Vector3(50, 50, 0), Vector3.Zero, WHITE, shader);
		}

		//mesh
		mesh = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);

		//material
		material = Raylib.LoadMaterialDefault();
		unsafe
		{
			MaterialMap* maps = (MaterialMap*)material.maps;//.ToPointer();
			maps[(int)MATERIAL_MAP_DIFFUSE].color = RED;
			//((MaterialMap*)material.maps)[(int)MATERIAL_MAP_DIFFUSE].color = RED;
		}
	}


	public void DoDraw(RenderPacket3d renderPacket)
	{


		Raylib.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, new Vector3[] { RenderSystem.camera.position }, SHADER_UNIFORM_VEC3);
		Raylib.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, RenderSystem.camera.position, SHADER_UNIFORM_VEC3);

		var xforms = renderPacket.instances;
		//var mesh = renderMesh.mesh;
		//var material = renderMesh.material;
		//TODO: when raylib 4 is released change to use instanced based.   right now (3.7.x) there's a bug where it doesn't render instances=1
		for (var i = 0; i < xforms.Length; i++)
		{
			Raylib.DrawMesh(mesh, material, Matrix4x4.Transpose(xforms[i])); //IMPORTANT: raylib is row-major.   need to transpose dotnet (column major) to match
		}
		if (xforms.Length == 0)
		{
			Console.WriteLine("Packet is EMPTY!!>?!?!");
		}

		//Console.WriteLine($"cpuId={Thread.GetCurrentProcessorId()}, mtId={Thread.CurrentThread.ManagedThreadId}");
	}



}

public class RenderPacket3d
{
	public Matrix4x4[] instances;
}
