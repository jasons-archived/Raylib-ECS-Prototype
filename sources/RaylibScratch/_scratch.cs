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


using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.ConfigFlags;
using static Raylib_cs.Color;
using static Raylib_cs.CameraProjection;
using static Raylib_cs.ShaderLocationIndex;
using static Raylib_cs.ShaderUniformDataType;
using static Raylib_cs.MaterialMapIndex;
using static Raylib_cs.CameraMode;
using static Raylib_cs.KeyboardKey;

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
		projection = CameraProjection.CAMERA_PERSPECTIVE,                   // Camera3D mode type
	};

	
	public Task renderTask;
	public void Start()
	{

		renderTask = new Task(() =>
		{
			Nito.AsyncEx.AsyncContext.Run(_RenderThread_Worker);
		}, TaskCreationOptions.LongRunning);
		renderTask.Start();

		//_renderThread = new Thread(() =>
		//{
		//	Nito.AsyncEx.AsyncContext.Run(async () =>
		//	{
		//		renderTask = _RenderThread_Worker();
		//		return renderTask;
		//	});
		//});
		//_renderThread.Start();

	}



	public async Task _RenderThread_Worker()
	{
		Console.WriteLine("render thread start");
		Thread.BeginThreadAffinity();
		try
		{
			//init
			Raylib.InitWindow(screenSize.Width, screenSize.Height, windowTitle);
			var technique = new ModelTechnique();
			technique.Init();

			var rand = new Random();
			var renderPacket = new RenderPacket3d();
			renderPacket.instances = new Matrix4x4[100];
			for (var i = 0; i < renderPacket.instances.Length; i++)
			{
				renderPacket.instances[i]=Matrix4x4.Identity;
				renderPacket.instances[i].Translation = new Vector3(rand.NextSingle() - 0.5f, rand.NextSingle() - 0.5f, rand.NextSingle() - 0.5f) * 10;
			}




			var swElapsed = Stopwatch.StartNew();
			var swTotal = Stopwatch.StartNew();
			//render loop
			while (!Raylib.WindowShouldClose())
			{
				var elapsed =(float)swElapsed.Elapsed.TotalSeconds;
				swElapsed.Restart();
				var totalTime =(float) swTotal.Elapsed.TotalSeconds;

				_UpdateRenderPacket(renderPacket, elapsed, totalTime);

				//draw
				Raylib.BeginDrawing();
				Raylib.ClearBackground(Color.RAYWHITE);
				Raylib.BeginMode3D(camera);
				//draw renderpackets
				technique.DoDraw(renderPacket);

				Raylib.DrawGrid(100, 1.0f);
				Raylib.EndMode3D();
				Raylib.DrawText("Reference Rendering", 10, 40, 20, Color.DARKGRAY);
				Raylib.DrawFPS(10, 10);
				Raylib.EndDrawing();
			}
			Raylib.CloseWindow();

		}
		finally
		{
			Thread.EndThreadAffinity();
		}



		Console.WriteLine("render thread done");
	}

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

			Utils.SetShaderValue(shader, ambientLoc, new float[] { 0.2f, 0.2f, 0.2f, 1.0f }, SHADER_UNIFORM_VEC4);

			Rlights.CreateLight(0, LightType.LIGHT_DIRECTIONAL, new Vector3(50, 50, 0), Vector3.Zero, WHITE, shader);
		}

		//mesh
		mesh = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);

		//material
		material = Raylib.LoadMaterialDefault();
		unsafe
		{
			MaterialMap* maps = (MaterialMap*)material.maps.ToPointer();
			maps[(int)MATERIAL_MAP_DIFFUSE].color = RED;
			//((MaterialMap*)material.maps)[(int)MATERIAL_MAP_DIFFUSE].color = RED;
		}
	}


	public void DoDraw(RenderPacket3d renderPacket)
	{
	

		Utils.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, new Vector3[] { RenderSystem.camera.position }, SHADER_UNIFORM_VEC3);
		Utils.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, RenderSystem.camera.position, SHADER_UNIFORM_VEC3);

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
