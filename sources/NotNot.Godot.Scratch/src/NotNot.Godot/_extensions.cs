// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Godot;

//public static class _Extensions_AnimatedSprite2D
//{

//	public static void _AddFrame(this AnimatedSprite2D sprite, StringName animName, StringName resPath)
//	{
//		if (sprite.Frames.HasAnimation(animName) is false)
//		{
//			sprite.Frames.AddAnimation(animName);
//		}
//		//load texture
//		var texture = GD.Load<Texture2D>(resPath);
//		//add frame to animation
//		sprite.Frames.AddFrame(animName, texture);
//	}
//}

public static class _Extensions_Object
{
	/// <summary>
	/// syntax sugar to await signals named "timeout"
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static SignalAwaiter _TimeoutAwaiter(this global::Godot.Object source, string signalName="timeout")
	{
		return source.ToSignal(source, signalName);
	}


	//public static SignalAwaiter _ToSignal(this global::Godot.Object source, string signalName)
	//{
	//	return source.ToSignal(source, signalName);
	//}
	//public static SignalAwaiter _ToSignal(this global::Godot.Object source, Action signal,
	//	[CallerArgumentExpression("signal")]string signalName="")
	//{

	//	return source.ToSignal(source, signalName);
	//}
}
public static class _Extensions_Control
{
	//set position of the node relative to screenspace
	public static void SetPositionCenterPercentScreenSpaceX(this Control control, float xPercent)
	{
		var viewport = control.GetViewport();
		var viewportSize = viewport.GetVisibleRect().Abs().Size;

		var x = viewportSize.x * xPercent;
		x -= control.Size.x / 2;

		control.Position = new Vector2(x, control.Position.y);

	}
	public static void SetPositionCenterPercentScreenSpaceY(this Control control, float yPercent)
	{
		var viewport = control.GetViewport();
		var viewportSize = viewport.GetVisibleRect().Abs().Size;

		var y = viewportSize.y * yPercent;
		y -= control.Size.y / 2;

		control.Position = new Vector2(control.Position.x, y);

	}

	public static void SetPositionCenterPercentScreenSpace(this Control control, float xPercent, float yPercent)
	{
		control.SetPositionCenterPercentScreenSpace(new Vector2(xPercent, yPercent));
	}

	public static void SetPositionCenterPercentScreenSpace(this Control control, Vector2 percent)
		{
			var viewport = control.GetViewport();
		var viewportSize = viewport.GetVisibleRect().Abs().Size;

		var pos = viewportSize * percent;
		pos -= control.Size / 2;

		control.Position = pos;

	}
}

public static class _Extensions_Node
{
	/// <summary>
	/// Finds the first parent of the given type T, walking up the hiearchy until reached.  null if not found
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="node"></param>
	/// <returns></returns>
	public static T? _FindParent<T>(this Node node) where T : Node
	{
		var current = node;
		while (true)
		{
			var parent = current.GetParent();
			if (parent == null)
			{
				return null;
			}

			if (parent is T)
			{
				return parent as T;
			}
			current = parent;
		}
	}
	/// <summary>
	/// Finds the first child of the given type T, walking down the hiearchy until depth is reached (default depth 0).  null if not found
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="node"></param>
	/// <param name="name"></param>
	/// <param name="depth"></param>
	/// <param name="includInternal"></param>
	/// <returns></returns>
	public static T? _FindChild<T>(this Node node, string? name = null, int depth = 0, bool includInternal = false) where T : Node
	{
		depth -= 1;
		foreach (var child in node.GetChildren(includInternal))
		{
			if (child is T && (name == null || child.Name == name))
			{
				return child as T;
			}
		}
		if (depth >= 0)
		{
			foreach (var child in node.GetChildren(includInternal))
			{
				var result = _FindChild<T>(child, name, depth, includInternal);
				if (result != null)
				{
					return result;
				}
			}
		}
		return null;

	}
}
