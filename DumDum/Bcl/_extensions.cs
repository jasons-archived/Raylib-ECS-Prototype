using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumDum.Bcl;


public static class zz__Extensions_Array
{
	//public static float _SUM(this IEnumerable<float> target)
	//{
	//	var total = 0f;
	//	foreach (var item in target)
	//	{
	//		total += item;
	//	}
	//	return total;
	//}
	//public static float _AVG(this IEnumerable<float> source)
	//{
	//	var total = 0f;
	//	var loopCount = 0;
	//	foreach (var item in source)
	//	{
	//		loopCount++;
	//		total += item;
	//	}
	//	return total / loopCount;

	//}
	//public static float _MAX(this IEnumerable<float> target)
	//{

	//	var loopCount = 0;
	//	var max = float.NegativeInfinity;
	//	foreach (var item in target)
	//	{
	//		loopCount++;
	//		max = item > max ? item : max;
	//	}
	//	if (loopCount == 0)
	//	{
	//		return float.NaN;
	//	}


	//	return max;
	//}
	//public static float _MIN(this IEnumerable<float> target)
	//{
	//	var loopCount = 0;
	//	var min = float.PositiveInfinity;
	//	foreach (var item in target)
	//	{
	//		loopCount++;
	//		min = item < min ? item : min;
	//	}
	//	if (loopCount == 0)
	//	{
	//		return float.NaN;
	//	}


	//	return min;
	//}

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