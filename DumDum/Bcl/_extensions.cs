using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumDum.Bcl;


public static class zz__Extensions_Array
{


	public static float _SUM(this float[] target)
	{

		target.Average();
		var total = 0f;
		for (var i = 0; i < target.Length; i++)
		{
			total += target[i];
		}

		return total;
	}
	public static float _AVG(this float[] target)
	{

		var total = target._SUM();
		return total / target.Length;
	}
	public static float _MAX(this float[] target)
	{
		if (target.Length == 0)
		{
			return float.NaN;
		}

		var max = float.NegativeInfinity;
		for (var i = 0; i < target.Length; i++)
		{
			max = target[i] > max ? target[i] : max;
		}

		return max;
	}
	public static float _MIN(this float[] target)
	{
		if (target.Length == 0)
		{
			return float.NaN;
		}

		var min = float.PositiveInfinity;
		for (var i = 0; i < target.Length; i++)
		{
			min = target[i] < min ? target[i] : min;
		}

		return min;
	}
}

public static class zz__Extensions_List
{
	private static Random _rand = new();

	public static bool _TryRemoveRandom<T>(this List<T> target, out T value)
	{
		if (target.Count == 0)
		{
			value = default;
			return false;
		}
		var index = -1;
		lock (_rand)
		{
			index = _rand.Next(0, target.Count);
		}

		value = target[index];
		target.RemoveAt(index);
		return true;
	}
}