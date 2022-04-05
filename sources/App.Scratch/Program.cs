/*******************************************************************************************
*
*   raylib [shaders] example - rlgl module usage for instanced meshes
*
*   This example uses [rlgl] module funtionality (pseudo-OpenGL 1.1 style coding)
*
*   This example has been created using raylib 3.5 (www.raylib.com)
*   raylib is licensed under an unmodified zlib/libpng license (View raylib.h for details)
*
*   Example contributed by @seanpringle and reviewed by Ramon Santamaria (@raysan5)
*
*   Copyright (c) 2020 @seanpringle
*
********************************************************************************************/

using System;
using System.Diagnostics;
using System.Numerics;
using Raylib_CsLo;
using NotNot;
using static Raylib_CsLo.Raylib;
using static Raylib_CsLo.ConfigFlags;
using static Raylib_CsLo.Color;
using static Raylib_CsLo.CameraProjection;
using static Raylib_CsLo.ShaderLocationIndex;
using static Raylib_CsLo.ShaderUniformDataType;
using static Raylib_CsLo.MaterialMapIndex;
using static Raylib_CsLo.CameraMode;
using static Raylib_CsLo.KeyboardKey;
using NotNot.Bcl;

public class shaders_mesh_instancing
{
	public static int Main()
	{
		bool isBatched = false;
		// Initialization
		//--------------------------------------------------------------------------------------
		const int screenWidth = 1920;
		const int screenHeight = 1080;
		//const int fps = 600;

		SetConfigFlags(FLAG_MSAA_4X_HINT);  // Enable Multi Sampling Anti Aliasing 4x (if available)
		InitWindow(screenWidth, screenHeight, "raylib [shaders] example - rlgl mesh instanced");

		int speed = 30;                 // Speed of jump animation
		int groups = 2;                 // Count of separate groups jumping around
		float amp = 10;                 // Maximum amplitude of jump
		float variance = 0.8f;          // Global variance in jump height
		float loop = 0.0f;              // Individual cube's computed loop timer

		// Used for various 3D coordinate & vector ops
		float x = 0.0f;
		float y = 0.0f;
		float z = 0.0f;

		// Define the camera to look into our 3d world
		Camera3D camera = new Camera3D();
		camera.position = Vector3.One * 80f;// new Vector3(52.0f, 52.0f, 12.0f);
		camera.target = new Vector3(0.0f, 0.0f, 0.0f);
		camera.up = new Vector3(0.0f, 1.0f, 0.0f);
		camera.fovy = 45.0f;
		camera.projection_ = CAMERA_PERSPECTIVE;

		// Number of instances to display
		const int instances = 8000;

		Matrix4x4[] rotations = new Matrix4x4[instances];    // Rotation state of instances
		Matrix4x4[] rotationsInc = new Matrix4x4[instances]; // Per-frame rotation animation of instances
		Matrix4x4[] translations = new Matrix4x4[instances]; // Locations of instances

		// Scatter random cubes around
		for (int i = 0; i < instances; i++)
		{
			x = GetRandomValue(-50, 50);
			y = GetRandomValue(-50, 50);
			z = GetRandomValue(-50, 50);
			translations[i] = Matrix4x4.CreateTranslation(x, y, z);

			x = GetRandomValue(0, 360);
			y = GetRandomValue(0, 360);
			z = GetRandomValue(0, 360);
			Vector3 axis = Vector3.Normalize(new Vector3(x, y, z));
			float angle = (float)GetRandomValue(0, 10) * RayMath.DEG2RAD;

			rotationsInc[i] = Matrix4x4.CreateFromAxisAngle(axis, angle);
			rotations[i] = Matrix4x4.Identity;
		}

		Matrix4x4[] transforms = new Matrix4x4[instances];   // Pre-multiplied transformations passed to rlgl
		
		Shader shader;
		if (isBatched)
		{
			shader = LoadShader("resources/shaders/glsl330/base_lighting_instanced.vs",
				"resources/shaders/glsl330/lighting.fs");
		}
		else
		{
			shader = LoadShader("resources/shaders/glsl330/base_lighting.vs", "resources/shaders/glsl330/lighting.fs");
		}

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

		var rLights = new NotNot.Rendering.RaylibLightHelper();
		rLights.CreateLight(NotNot.Rendering.RaylibLightHelper.LightType.LIGHT_DIRECTIONAL, new Vector3(50, 50, 0), Vector3.Zero, WHITE, shader);

		Material material = LoadMaterialDefault();
		material.shader = shader;
		unsafe
		{
			MaterialMap* maps = (MaterialMap*)material.maps;
			maps[(int)MATERIAL_MAP_DIFFUSE].color = RED;
		}


		int textPositionY = 300;

		// Simple frames counter to manage animation
		int framesCounter = 0;

		Mesh cube = GenMeshCube(1.0f, 1.0f, 1.0f);
		//var cubeModel = LoadModelFromMesh(cube);
		//unsafe
		//{
		//	Material* materials = (Material*)cubeModel.materials;
		//	materials[0] = material;
		//}

		SetCameraMode(camera, CAMERA_FREE); // Set a free camera mode

		//SetTargetFPS(fps);                   // Set our game to run at 60 frames-per-second
		//--------------------------------------------------------------------------------------

		// Main game loop
		var timer = Stopwatch.StartNew();
		while (!WindowShouldClose())        // Detect window close button or ESC key
		{
			var elapsed = timer.ElapsedMilliseconds/100f;
			var posOffset = new Vector3(MathF.Sin(elapsed), MathF.Cos(elapsed / 2), MathF.Sin(elapsed/4));

			// Update the light shader with the camera view position
			//float[] cameraPos = { camera.position.X, camera.position.Y, camera.position.Z };
			//Utils.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, cameraPos, SHADER_UNIFORM_VEC3);
			Raylib.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, ref camera.position, SHADER_UNIFORM_VEC3);

			// Apply per-instance transformations
			for (int i = 0; i < instances; i++)
			{
				rotations[i] = Matrix4x4.Multiply(rotations[i], rotationsInc[i]);
				transforms[i] = Matrix4x4.Multiply(rotations[i], translations[i]);

				// Get the animation cycle's framesCounter for this instance
				loop = (float)((framesCounter + (int)(((float)(i % groups) / groups) * speed)) % speed) / speed;

				// Calculate the y according to loop cycle
				y = (MathF.Sin(loop * MathF.PI * 2)) * amp * ((1 - variance) + (variance * (float)(i % (groups * 10)) / (groups * 10)));

				// Clamp to floor
				y = (y < 0) ? 0.0f : y;

				//transforms[i] = Matrix4x4.Multiply(transforms[i], Matrix4x4.CreateTranslation(0.0f, y, 0.0f));
				//transforms[i] = Matrix4x4.Multiply(transforms[i], Matrix4x4.CreateTranslation(posOffset));
				transforms[i].Translation += posOffset;
				transforms[i] = Matrix4x4.Transpose(transforms[i]);
			}
			//----------------------------------------------------------------------------------

			// Draw
			//----------------------------------------------------------------------------------
			BeginDrawing();
			ClearBackground(RAYWHITE);

			BeginMode3D(camera);
			if (isBatched)
			{
				var transposedXforms = Mem<Matrix4x4>.Allocate(transforms, false);

				//DrawMeshInstanced(cube, material, transforms, instances);
				DrawMeshInstanced(cube, material, transposedXforms.Span, instances);
			}
			else
			{

				for (int i = 0; i < instances; i++)
				{
					DrawMesh(cube, material, transforms[i]);
				}

			}

			EndMode3D();

		

			DrawFPS(10, 10);

			EndDrawing();
			//----------------------------------------------------------------------------------
		}

		// De-Initialization
		//--------------------------------------------------------------------------------------
		CloseWindow();        // Close window and OpenGL context
							  //--------------------------------------------------------------------------------------

		return 0;
	}
}

