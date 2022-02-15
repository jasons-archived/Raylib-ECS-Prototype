// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;

namespace NotNot.Rendering;





public class StaticModelTechnique : IRenderTechnique3d
{
	public bool doBatching=false;
	public bool IsInitialized { get; set; }
	public void Initialize()
	{
		if (IsInitialized) { return; }
		IsInitialized = true;

		//Mesh cube = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);
		//shader = Raylib.LoadShader("resources/shaders/glsl330/base_lighting_instanced.vs", "resources/shaders/glsl330/lighting.fs"); //TODO: move to a global
		if (doBatching)
		{
			shader = Raylib.LoadShader("resources/shaders/glsl330/base_lighting_instanced.vs", "resources/shaders/glsl330/lighting.fs"); //TODO: move to a global
		}
		else
		{
			shader = Raylib.LoadShader("resources/shaders/glsl330/base_lighting.vs", "resources/shaders/glsl330/lighting.fs"); //TODO: move to a global
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

		lights = new RLights();
		lights.CreateLight(RLights.LightType.LIGHT_DIRECTIONAL, new Vector3(50, 50, 0), Vector3.Zero, WHITE, shader);


		OnInitialize(this);
		//Material material = LoadMaterialDefault();

		//unsafe
		//{
		//	MaterialMap* maps = (MaterialMap*)material.maps.ToPointer();
		//	maps[(int)MATERIAL_MAP_DIFFUSE].color = RED;
		//	//((MaterialMap*)material.maps)[(int)MATERIAL_MAP_DIFFUSE].color = RED;
		//}


		//mesh = cube;
		//this.shader = shader;
		//this.material = material;
	}

	public Action<StaticModelTechnique> OnInitialize;

	public Mesh mesh;
	public Shader shader;
	public Material material;
	public RLights lights;


	public unsafe void DoDraw(RenderPacket3d renderPacket)
	{
		if (IsInitialized == false)
		{
			Initialize();
		}
		//__DEBUG.Throw(IsInitialized);

		Raylib.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, new Vector3[] { RenderReferenceImplementationSystem.camera.position }, SHADER_UNIFORM_VEC3);
		Raylib.SetShaderValue(shader, (int)SHADER_LOC_VECTOR_VIEW, RenderReferenceImplementationSystem.camera.position, SHADER_UNIFORM_VEC3);

		var xforms = renderPacket.instances.Span;
		//var mesh = renderMesh.mesh;
		//var material = renderMesh.material;
		if(doBatching)
		{
			//instanced
			//var transposedXforms = Mem<Matrix4x4>.Allocate(xforms.Length, false);
			var transposedXforms = stackalloc Matrix4x4[xforms.Length];
			for (var i = 0; i < xforms.Length; i++)
			{
				transposedXforms[i] = Matrix4x4.Transpose(xforms[i]); //IMPORTANT: raylib is row-major.   need to transpose dotnet (column major) to match
			}
			Raylib.DrawMeshInstanced(mesh, material, transposedXforms, xforms.Length);
		}
		else {
			//non-instanced
			for (var i = 0; i < xforms.Length; i++)
			{
				Raylib.DrawMesh(mesh, material, Matrix4x4.Transpose(xforms[i])); //IMPORTANT: raylib is row-major.   need to transpose dotnet (column major) to match
			}
		}
		if (xforms.Length == 0)
		{
			
			__ERROR.WriteLine($"Packet is EMPTY!!>?!?!  packet.IsEmpty={renderPacket.IsEmpty}");
		}

		//Console.WriteLine($"cpuId={Thread.GetCurrentProcessorId()}, mtId={Thread.CurrentThread.ManagedThreadId}");
	}

	public void ConstructPacketProperties(RenderPacket3d renderPacket)
	{
		
	}

}
