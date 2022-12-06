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
