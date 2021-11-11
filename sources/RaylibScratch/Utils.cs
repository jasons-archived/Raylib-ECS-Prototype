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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.ShaderUniformDataType;

namespace RaylibScratch;


public struct Light
{
	public bool enabled;
	public LightType type;
	public Vector3 position;
	public Vector3 target;
	public Color color;

	public int enabledLoc;
	public int typeLoc;
	public int posLoc;
	public int targetLoc;
	public int colorLoc;
}

public enum LightType
{
	LIGHT_DIRECTIONAL,
	LIGHT_POINT
}

public static class Rlights
{
	public static Light CreateLight(int lightsCount, LightType type, Vector3 pos, Vector3 targ, Color color, Shader shader)
	{
		Light light = new Light();

		light.enabled = true;
		light.type = type;
		light.position = pos;
		light.target = targ;
		light.color = color;

		string enabledName = "lights[" + lightsCount + "].enabled";
		string typeName = "lights[" + lightsCount + "].type";
		string posName = "lights[" + lightsCount + "].position";
		string targetName = "lights[" + lightsCount + "].target";
		string colorName = "lights[" + lightsCount + "].color";

		light.enabledLoc = GetShaderLocation(shader, enabledName);
		light.typeLoc = GetShaderLocation(shader, typeName);
		light.posLoc = GetShaderLocation(shader, posName);
		light.targetLoc = GetShaderLocation(shader, targetName);
		light.colorLoc = GetShaderLocation(shader, colorName);

		UpdateLightValues(shader, light);

		return light;
	}

	public static void UpdateLightValues(Shader shader, Light light)
	{
		// Send to shader light enabled state and type
		Utils.SetShaderValue(shader, light.enabledLoc, light.enabled ? 1 : 0, SHADER_UNIFORM_INT);
		Utils.SetShaderValue(shader, light.typeLoc, (int)light.type, SHADER_UNIFORM_INT);

		// Send to shader light target position values
		float[] position = new[] { light.position.X, light.position.Y, light.position.Z };
		Utils.SetShaderValue(shader, light.posLoc, position, SHADER_UNIFORM_VEC3);

		// Send to shader light target position values
		float[] target = { light.target.X, light.target.Y, light.target.Z };
		Utils.SetShaderValue(shader, light.targetLoc, target, SHADER_UNIFORM_VEC3);

		// Send to shader light color values
		float[] color = new[] { (float)light.color.r / (float)255, (float)light.color.g / (float)255, (float)light.color.b / (float)255, (float)light.color.a / (float)255 };
		Utils.SetShaderValue(shader, light.colorLoc, color, SHADER_UNIFORM_VEC4);
	}
}




/*
Utility functions for parts of the api that are not easy to interact with via pinvoke.
Not included in the bindings as there are multiple ways to handle these cases.
I prefer to leave that choice to the user.
*/
public static class Utils
{
	// Extension providing SubText
	public static string SubText(this string input, int position, int length)
	{
		return input.Substring(position, Math.Min(length, input.Length));
	}

	/*
	Utility to convert the IntPtr from GetDroppedFiles to a string[].

	GetDroppedFiles is a char** but the length varies based on MAX_FILEPATH_LENGTH.

	#if defined(__linux__)
		#define MAX_FILEPATH_LENGTH    4096     // Use Linux PATH_MAX value
	#else
		#define MAX_FILEPATH_LENGTH     512     // Use common value
	#endif

	Here is how it allocates the strings.

	// GLFW3 Window Drop Callback, runs when drop files into window
	// NOTE: Paths are stored in dynamic memory for further retrieval
	// Everytime new files are dropped, old ones are discarded
	static void WindowDropCallback(GLFWwindow *window, int count, const char **paths)
	{
		ClearDroppedFiles();

		CORE.Window.dropFilesPath = (char **)RL_MALLOC(sizeof(char *)*count);

		for (int i = 0; i < count; i++)
		{
			CORE.Window.dropFilesPath[i] = (char *)RL_MALLOC(sizeof(char)*MAX_FILEPATH_LENGTH);
			strcpy(CORE.Window.dropFilesPath[i], paths[i]);
		}

		CORE.Window.dropFilesCount = count;
	}

	If it was fixed I think the following could work.

	// Get dropped files names (memory should be freed)
	[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStr, SizeConst=512)]
	public static extern string[] GetDroppedFiles(ref int count);
	*/
	public static string[] MarshalDroppedFiles(ref int count)
	{
		string[] droppedFileStrings = new string[count];
		IntPtr pointer = Raylib.GetDroppedFiles(ref count);

		string[] s = new string[count];
		char[] word;
		int i;
		int j;
		int size;

		// TODO: this is a mess, find a better way
		unsafe
		{
			byte** str = (byte**)pointer.ToPointer();

			i = 0;
			while (i < count)
			{
				j = 0;
				while (str[i][j] != 0)
					j++;
				size = j;
				word = new char[size];
				j = 0;
				while (str[i][j] != 0)
				{
					word[j] = (char)str[i][j];
					j++;
				}
				s[i] = new string(word);

				i++;
			}
		}
		return s;
	}

	public unsafe static Material GetMaterial(ref Model model, int materialIndex)
	{
		Material* materials = (Material*)model.materials.ToPointer();
		return *materials;
	}

	public unsafe static Texture2D GetMaterialTexture(ref Model model, int materialIndex, MaterialMapIndex mapIndex)
	{
		Material* materials = (Material*)model.materials.ToPointer();
		MaterialMap* maps = (MaterialMap*)materials[0].maps.ToPointer();
		return maps[(int)mapIndex].texture;
	}

	public unsafe static void SetMaterialTexture(ref Model model, int materialIndex, MaterialMapIndex mapIndex, ref Texture2D texture)
	{
		Material* materials = (Material*)model.materials.ToPointer();
		Raylib.SetMaterialTexture(ref materials[materialIndex], (int)mapIndex, texture);
	}

	public unsafe static void SetMaterialShader(ref Model model, int materialIndex, ref Shader shader)
	{
		Material* materials = (Material*)model.materials.ToPointer();
		materials[materialIndex].shader = shader;
	}

	//// Helper functions to pass data into raylib. Pins the data so we can pass in a stable IntPtr to raylib.
	////THE ORIGINAL
	//public static void SetShaderValue<T>(Shader shader, int uniformLoc, T value, ShaderUniformDataType uniformType)
	//{
	//    GCHandle pinnedData = GCHandle.Alloc(value, GCHandleType.Pinned);
	//    Raylib.SetShaderValue(shader, uniformLoc, pinnedData.AddrOfPinnedObject(), uniformType);
	//    pinnedData.Free();
	//}
	public static unsafe void SetShaderValue<T>(Shader shader, int uniformLoc, ref T value, ShaderUniformDataType uniformType) where T : unmanaged
	{
		fixed (T* valuePtr = &value)
		{
			Raylib.SetShaderValue(shader, uniformLoc, (IntPtr)valuePtr, uniformType);
		}
	}
	public static unsafe void SetShaderValue<T>(Shader shader, int uniformLoc, T value, ShaderUniformDataType uniformType) where T : unmanaged
	{
		Raylib.SetShaderValue(shader, uniformLoc, (IntPtr)(&value), uniformType);
	}
	public static unsafe void SetShaderValue<T>(Shader shader, int uniformLoc, T[] values, ShaderUniformDataType uniformType) where T : unmanaged
	{
		SetShaderValue(shader, uniformLoc, (Span<T>)values, uniformType);
	}
	public static unsafe void SetShaderValue<T>(Shader shader, int uniformLoc, Span<T> values, ShaderUniformDataType uniformType) where T : unmanaged
	{
		fixed (T* valuePtr = values)
		{
			Raylib.SetShaderValue(shader, uniformLoc, (IntPtr)valuePtr, uniformType);
		}
	}

	//// Helper functions to pass data into raylib. Pins the data so we can pass in a stable IntPtr to raylib.
	//public static unsafe void SetShaderValue2<T>(Shader shader, int uniformLoc, T value, ShaderUniformDataType uniformType)
	//{
	//    GCHandle pinnedData = GCHandle.Alloc(value, GCHandleType.Pinned);            
	//    Raylib.SetShaderValue(shader, uniformLoc, pinnedData.AddrOfPinnedObject(), uniformType);
	//    pinnedData.Free();
	//}
	////
	//// Summary:
	////     Set shader uniform value value refers to a const void *
	//[DllImport("raylib", CallingConvention = CallingConvention.Cdecl)]
	//public static extern void SetShaderValue(Shader shader, int uniformLoc, IntPtr value, ShaderUniformDataType uniformType);

	public static void SetShaderValueV<T>(Shader shader, int uniformLoc, T[] value, ShaderUniformDataType uniformType, int count)
	{
		GCHandle pinnedData = GCHandle.Alloc(value, GCHandleType.Pinned);
		Raylib.SetShaderValueV(shader, uniformLoc, pinnedData.AddrOfPinnedObject(), uniformType, count);
		pinnedData.Free();
	}
}
