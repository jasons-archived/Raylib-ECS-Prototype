﻿// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Generic;

namespace NotNot.Bcl.Collections._unused
{
	/// <summary>
	/// allows finding an instance of an object by an identifier.
	/// <para>Internally stores using a weak reference so it does not prevent garbage collection.</para> 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[ThreadSafety(ThreadSituation.Always)]
	public class ObjLookup<T> where T : class
	{

		public Dictionary<int, WeakReference<T>> _storage = new();
		private int _lastId = 0;
		private readonly object _lock = new();
		public int Add(T item)
		{
			lock (this._lock)
			{
				var toReturn = this._lastId++;
				this._storage.Add(toReturn, new(item));

				return toReturn;
			}
		}
		public void Remove(int id)
		{
			lock (this._lock)
			{
				this._storage.Remove(id);
			}
		}

		public T? Get(int id)
		{
			lock (this._lock)
			{
				if (this._storage.TryGetValue(id, out var weakRef))
				{
					if (weakRef.TryGetTarget(out var toReturn))
					{
						return toReturn;
					}
				}
			}
			return null;
		}
	}
}