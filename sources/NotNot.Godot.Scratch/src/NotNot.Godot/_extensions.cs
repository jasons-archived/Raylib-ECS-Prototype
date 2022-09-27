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
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Godot;

public static class _CanvasItem_Extensions
{
	///https://docs.godotengine.org/en/latest/tutorials/2d/custom_drawing_in_2d.html?highlight=low%20level#arc-polygon-function
	public static void _DrawCircleArcPoly(this CanvasItem _this, Vector2 center, float radius, float angleFrom, float angleTo, Color color)
	{
		unsafe
		{
			int nbPoints = 32;

			using var spanGuard = NotNot.Bcl.SpanGuard<Vector2>.Allocate(nbPoints + 1);
			//var pointsArc =  new Vector2[nbPoints + 1];
			var pointsArc = spanGuard.DangerousGetArray().Array;

			pointsArc[0] = center;
			var colors = new Color[] { color };

			for (int i = 0; i <= nbPoints; i++) //BUG: should start at index 1, because 0 should be center?
			{
				float anglePoint = Mathf.DegToRad(angleFrom + i * (angleTo - angleFrom) / nbPoints - 90);
				pointsArc[i] = center + new Vector2(Mathf.Cos(anglePoint), Mathf.Sin(anglePoint)) * radius;
			}
			_this.DrawPolygon(pointsArc, colors);
		}
	}


	/// <summary>
	/// example showing how to draw custom 2d
	/// https://docs.godotengine.org/en/latest/tutorials/2d/custom_drawing_in_2d.html?highlight=low%20level
	/// </summary>
	public static void _DrawCircleArc(this CanvasItem _this, Vector2 center, float radius, float angleFrom, float angleTo, Color color)
	{
		unsafe
		{
			int nbPoints = 32;
			var pointsArc = stackalloc Vector2[nbPoints + 1]; // new Vector2[nbPoints + 1];

			for (int i = 0; i <= nbPoints; i++)
			{

				float anglePoint = Mathf.DegToRad(angleFrom + i * (angleTo - angleFrom) / nbPoints - 90f);
				pointsArc[i] = center + new Vector2(Mathf.Cos(anglePoint), Mathf.Sin(anglePoint)) * radius;
			}

			for (int i = 0; i < nbPoints - 1; i++)
			{
				_this.DrawLine(pointsArc[i], pointsArc[i + 1], color);
			}
		}
	}
}

public static class _Node_Extensions
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
}
