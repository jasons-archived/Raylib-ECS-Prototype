﻿// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
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
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Bcl.Collections;



/// <summary>
/// from https://egorikas.com/max-and-min-heap-implementation-with-csharp/
/// and https://www.youtube.com/watch?v=t0Cq6tVNRBA
/// </summary>
public abstract class HeapBase
{
	protected int[] _elements;
	protected int _count;
	public int Count { get => _count; }

	public HeapBase(int size = 10)
	{
		size = Math.Max(size, 10);
		_elements = new int[size];
	}

	protected int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
	protected int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
	protected int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

	protected bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < _count;
	protected bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < _count;
	protected bool IsRoot(int elementIndex) => elementIndex == 0;

	protected int GetLeftChild(int elementIndex) => _elements[GetLeftChildIndex(elementIndex)];
	protected int GetRightChild(int elementIndex) => _elements[GetRightChildIndex(elementIndex)];
	protected int GetParent(int elementIndex) => _elements[GetParentIndex(elementIndex)];

	protected void Swap(int firstIndex, int secondIndex)
	{
		var temp = _elements[firstIndex];
		_elements[firstIndex] = _elements[secondIndex];
		_elements[secondIndex] = temp;
	}

	public bool IsEmpty()
	{
		return _count == 0;
	}

	public int Peek()
	{
		if (_count == 0)
			throw new IndexOutOfRangeException();

		return _elements[0];
	}

	public int Pop()
	{
		if (_count == 0)
			throw new IndexOutOfRangeException();

		var result = _elements[0];
		_elements[0] = _elements[_count - 1];
		_count--;

		ReCalculateDown();

		return result;
	}

	public void Push(int element)
	{
		if (_count == _elements.Length)
		{
			Array.Resize(ref _elements, _elements.Length * 2);
			//throw new IndexOutOfRangeException();
		}


		_elements[_count] = element;
		_count++;

		ReCalculateUp();
	}

	protected abstract void ReCalculateUp();
	protected abstract void ReCalculateDown();

}

public class MinHeap : HeapBase
{

	protected override void ReCalculateDown()
	{
		int index = 0;
		while (HasLeftChild(index))
		{
			var smallerIndex = GetLeftChildIndex(index);
			if (HasRightChild(index) && GetRightChild(index) < GetLeftChild(index))
			{
				smallerIndex = GetRightChildIndex(index);
			}

			if (_elements[smallerIndex] >= _elements[index])
			{
				break;
			}

			Swap(smallerIndex, index);
			index = smallerIndex;
		}
	}

	protected override void ReCalculateUp()
	{
		var index = _count - 1;
		while (!IsRoot(index) && _elements[index] < GetParent(index))
		{
			var parentIndex = GetParentIndex(index);
			Swap(parentIndex, index);
			index = parentIndex;
		}
	}
}

/// <summary>
/// from https://egorikas.com/max-and-min-heap-implementation-with-csharp/
/// and https://www.youtube.com/watch?v=t0Cq6tVNRBA
/// </summary>
public class MaxHeap : HeapBase
{

	protected override void ReCalculateDown()
	{
		int index = 0;
		while (HasLeftChild(index))
		{
			var biggerIndex = GetLeftChildIndex(index);
			if (HasRightChild(index) && GetRightChild(index) > GetLeftChild(index))
			{
				biggerIndex = GetRightChildIndex(index);
			}

			if (_elements[biggerIndex] < _elements[index])
			{
				break;
			}

			Swap(biggerIndex, index);
			index = biggerIndex;
		}
	}

	protected override void ReCalculateUp()
	{
		var index = _count - 1;
		while (!IsRoot(index) && _elements[index] > GetParent(index))
		{
			var parentIndex = GetParentIndex(index);
			Swap(parentIndex, index);
			index = parentIndex;
		}
	}
}
