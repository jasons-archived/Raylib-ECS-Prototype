using DumDum.Bcl.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DumDum.Bcl;


public static class zz__Extensions_Object
{
	//public static bool _IsAny
}


public static class zz__Extensions_List
{
	static ThreadLocal<Random> _rand = new(() => new());

	public static bool _TryRemoveRandom<T>(this IList<T> target, out T value)
	{
		if (target.Count == 0)
		{
			value = default;
			return false;
		}
		var index = -1;
		//lock (_rand)
		{
			index = _rand.Value.Next(0, target.Count);
		}

		value = target[index];
		target.RemoveAt(index);
		return true;
	}
	public static bool _TryTakeLast<T>(this IList<T> target, out T value)
	{
		if (target.Count == 0)
		{
			value = default;
			return false;
		}

		var index = target.Count-1;
		value = target[index];
		target.RemoveAt(index);
		return true;
	}

	public static void _Randomize<T>(this IList<T> target)
	{
		//lock (_rand)
		{
			for (var index = 0; index < target.Count; index++)
			{
				var swapIndex = _rand.Value.Next(0, target.Count);
				var value = target[index];
				target[index] = target[swapIndex];
				target[swapIndex] = value;
			}
		}

	}

	/// <summary>
	/// warning: do not modify list while enumerating span
	/// </summary>
	public static Span<T> _AsSpan_Unsafe<T>(this List<T> list)
	{
		return CollectionsMarshal.AsSpan(list);
	}

}


/// <summary>Extension methods for <see cref="TaskCompletionSource{TResult}"/>.</summary>
/// <threadsafety static="true" instance="false"/>
/// <remarks>from: https://github.com/tunnelvisionlabs/dotnet-threading/blob/3e99a9d13476a1e8224d81f282f3cedad143c1bc/Rackspace.Threading/TaskCompletionSourceExtensions.cs</remarks>
public static class zz_Extensions_TaskCompletionSource
{
	/// <summary>Transfers the result of a <see cref="Task{TResult}"/> to a <see cref="TaskCompletionSource{TResult}"/>.</summary>
	/// <remarks>
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.RanToCompletion"/> state,
	/// the result of the task is assigned to the <see cref="TaskCompletionSource{TResult}"/>
	/// using the <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Faulted"/> state,
	/// the unwrapped exceptions are bound to the <see cref="TaskCompletionSource{TResult}"/>
	/// using the <see cref="TaskCompletionSource{TResult}.SetException(IEnumerable{Exception})"/>
	/// method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Canceled"/> state,
	/// the <see cref="TaskCompletionSource{TResult}"/> is transitioned to the
	/// <see cref="TaskStatus.Canceled"/> state using the
	/// <see cref="TaskCompletionSource{TResult}.SetCanceled"/> method.</para>
	/// </remarks>
	/// <typeparam name="TSource">Specifies the result type of the source <see cref="Task{TResult}"/>.</typeparam>
	/// <typeparam name="TResult">Specifies the result type of the <see cref="TaskCompletionSource{TResult}"/>.</typeparam>
	/// <param name="taskCompletionSource">The <see cref="TaskCompletionSource{TResult}"/> instance.</param>
	/// <param name="task">The result task whose completion results should be transferred.</param>
	/// <exception cref="ArgumentNullException">
	/// <para>If <paramref name="taskCompletionSource"/> is <see langword="null"/>.</para>
	/// <para>-or-</para>
	/// <para>If <paramref name="task"/> is <see langword="null"/>.</para>
	/// </exception>
	/// <exception cref="ObjectDisposedException">
	/// <para>If the underlying <see cref="Task{TResult}"/> of <paramref name="taskCompletionSource"/> was disposed.</para>
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// <para>If the underlying <see cref="Task{TResult}"/> produced by <paramref name="taskCompletionSource"/> is already
	/// in one of the three final states: <see cref="TaskStatus.RanToCompletion"/>,
	/// <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.</para>
	/// </exception>
	public static void SetFromTask<TSource, TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Task<TSource> task)
		where TSource : TResult
	{
		if (taskCompletionSource == null)
			throw new ArgumentNullException("taskCompletionSource");
		if (task == null)
			throw new ArgumentNullException("task");

		switch (task.Status)
		{
			case TaskStatus.RanToCompletion:
				taskCompletionSource.SetResult(task.Result);
				break;

			case TaskStatus.Faulted:
				taskCompletionSource.SetException(task.Exception.InnerExceptions);
				break;

			case TaskStatus.Canceled:
				taskCompletionSource.SetCanceled();
				break;

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}
	public static void SetFromTask(this TaskCompletionSource taskCompletionSource, Task task)
	{
		if (taskCompletionSource == null)
			throw new ArgumentNullException("taskCompletionSource");
		if (task == null)
			throw new ArgumentNullException("task");

		switch (task.Status)
		{
			case TaskStatus.RanToCompletion:
				taskCompletionSource.SetResult();
				break;

			case TaskStatus.Faulted:
				taskCompletionSource.SetException(task.Exception.InnerExceptions);
				break;

			case TaskStatus.Canceled:
				taskCompletionSource.SetCanceled();
				break;

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}

	/// <summary>
	/// Transfers the result of a <see cref="Task{TResult}"/> to a <see cref="TaskCompletionSource{TResult}"/>,
	/// using a specified result value when the task is in the <see cref="TaskStatus.RanToCompletion"/>
	/// state.
	/// </summary>
	/// <remarks>
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.RanToCompletion"/> state,
	/// the specified <paramref name="result"/> value is assigned to the
	/// <see cref="TaskCompletionSource{TResult}"/> using the
	/// <see cref="TaskCompletionSource{TResult}.SetResult(TResult)"/> method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Faulted"/> state,
	/// the unwrapped exceptions are bound to the <see cref="TaskCompletionSource{TResult}"/>
	/// using the <see cref="TaskCompletionSource{TResult}.SetException(IEnumerable{Exception})"/>
	/// method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Canceled"/> state,
	/// the <see cref="TaskCompletionSource{TResult}"/> is transitioned to the
	/// <see cref="TaskStatus.Canceled"/> state using the
	/// <see cref="TaskCompletionSource{TResult}.SetCanceled"/> method.</para>
	/// </remarks>
	/// <typeparam name="TResult">Specifies the result type of the <see cref="TaskCompletionSource{TResult}"/>.</typeparam>
	/// <param name="taskCompletionSource">The <see cref="TaskCompletionSource{TResult}"/> instance.</param>
	/// <param name="task">The result task whose completion results should be transferred.</param>
	/// <param name="result">The result of the completion source when the specified task completed successfully.</param>
	/// <exception cref="ArgumentNullException">
	/// <para>If <paramref name="taskCompletionSource"/> is <see langword="null"/>.</para>
	/// <para>-or-</para>
	/// <para>If <paramref name="task"/> is <see langword="null"/>.</para>
	/// </exception>
	/// <exception cref="ObjectDisposedException">
	/// <para>If the underlying <see cref="Task{TResult}"/> of <paramref name="taskCompletionSource"/> was disposed.</para>
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// <para>If the underlying <see cref="Task{TResult}"/> produced by <paramref name="taskCompletionSource"/> is already
	/// in one of the three final states: <see cref="TaskStatus.RanToCompletion"/>,
	/// <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.</para>
	/// </exception>
	public static void SetFromTask<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Task task, TResult result)
	{
		switch (task.Status)
		{
			case TaskStatus.RanToCompletion:
				taskCompletionSource.SetResult(result);
				break;

			case TaskStatus.Faulted:
				taskCompletionSource.SetException(task.Exception.InnerExceptions);
				break;

			case TaskStatus.Canceled:
				taskCompletionSource.SetCanceled();
				break;

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}

	/// <summary>Attempts to transfer the result of a <see cref="Task{TResult}"/> to a <see cref="TaskCompletionSource{TResult}"/>.</summary>
	/// <remarks>
	/// <para>This method will return <see langword="false"/> if the <see cref="Task{TResult}"/>
	/// provided by <paramref name="taskCompletionSource"/> is already in one of the three
	/// final states: <see cref="TaskStatus.RanToCompletion"/>, <see cref="TaskStatus.Faulted"/>,
	/// or <see cref="TaskStatus.Canceled"/>. This method also returns <see langword="false"/>
	/// if the underlying <see cref="Task{TResult}"/> has already been disposed.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.RanToCompletion"/> state,
	/// the result of the task is assigned to the <see cref="TaskCompletionSource{TResult}"/>
	/// using the <see cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/> method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Faulted"/> state,
	/// the unwrapped exceptions are bound to the <see cref="TaskCompletionSource{TResult}"/>
	/// using the <see cref="TaskCompletionSource{TResult}.TrySetException(IEnumerable{Exception})"/>
	/// method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Canceled"/> state,
	/// the <see cref="TaskCompletionSource{TResult}"/> is transitioned to the
	/// <see cref="TaskStatus.Canceled"/> state using the
	/// <see cref="TaskCompletionSource{TResult}.TrySetCanceled"/> method.</para>
	/// </remarks>
	/// <typeparam name="TSource">Specifies the result type of the source <see cref="Task{TResult}"/>.</typeparam>
	/// <typeparam name="TResult">Specifies the result type of the <see cref="TaskCompletionSource{TResult}"/>.</typeparam>
	/// <param name="taskCompletionSource">The <see cref="TaskCompletionSource{TResult}"/> instance.</param>
	/// <param name="task">The result task whose completion results should be transferred.</param>
	/// <returns>
	/// <para><see langword="true"/> if the operation was successful.</para>
	/// <para>-or-</para>
	/// <para><see langword="false"/> if the operation was unsuccessful or the object has already been disposed.</para>
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// <para>If <paramref name="taskCompletionSource"/> is <see langword="null"/>.</para>
	/// <para>-or-</para>
	/// <para>If <paramref name="task"/> is <see langword="null"/>.</para>
	/// </exception>
	public static bool TrySetFromTask<TSource, TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Task<TSource> task)
		where TSource : TResult
	{
		switch (task.Status)
		{
			case TaskStatus.RanToCompletion:
				return taskCompletionSource.TrySetResult(task.Result);

			case TaskStatus.Faulted:
				return taskCompletionSource.TrySetException(task.Exception.InnerExceptions);

			case TaskStatus.Canceled:
				return taskCompletionSource.TrySetCanceled();

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}
	public static bool TrySetFromTask(this TaskCompletionSource taskCompletionSource, Task task)
	{
		switch (task.Status)
		{
			case TaskStatus.RanToCompletion:
				return taskCompletionSource.TrySetResult();

			case TaskStatus.Faulted:
				return taskCompletionSource.TrySetException(task.Exception.InnerExceptions);

			case TaskStatus.Canceled:
				return taskCompletionSource.TrySetCanceled();

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}

	/// <summary>
	/// Attempts to transfer the result of a <see cref="Task{TResult}"/> to a <see cref="TaskCompletionSource{TResult}"/>,
	/// using a specified result value when the task is in the <see cref="TaskStatus.RanToCompletion"/>
	/// state.
	/// </summary>
	/// <remarks>
	/// <para>This method will return <see langword="false"/> if the <see cref="Task{TResult}"/>
	/// provided by <paramref name="taskCompletionSource"/> is already in one of the three
	/// final states: <see cref="TaskStatus.RanToCompletion"/>, <see cref="TaskStatus.Faulted"/>,
	/// or <see cref="TaskStatus.Canceled"/>. This method also returns <see langword="false"/>
	/// if the underlying <see cref="Task{TResult}"/> has already been disposed.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.RanToCompletion"/> state,
	/// the specified <paramref name="result"/> value is assigned to the
	/// <see cref="TaskCompletionSource{TResult}"/> using the
	/// <see cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/> method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Faulted"/> state,
	/// the unwrapped exceptions are bound to the <see cref="TaskCompletionSource{TResult}"/>
	/// using the <see cref="TaskCompletionSource{TResult}.TrySetException(IEnumerable{Exception})"/>
	/// method.</para>
	///
	/// <para>If <paramref name="task"/> is in the <see cref="TaskStatus.Canceled"/> state,
	/// the <see cref="TaskCompletionSource{TResult}"/> is transitioned to the
	/// <see cref="TaskStatus.Canceled"/> state using the
	/// <see cref="TaskCompletionSource{TResult}.TrySetCanceled"/> method.</para>
	/// </remarks>
	/// <typeparam name="TResult">Specifies the result type of the <see cref="TaskCompletionSource{TResult}"/>.</typeparam>
	/// <param name="taskCompletionSource">The <see cref="TaskCompletionSource{TResult}"/> instance.</param>
	/// <param name="task">The result task whose completion results should be transferred.</param>
	/// <param name="result">The result of the completion source when the specified task completed successfully.</param>
	/// <returns>
	/// <para><see langword="true"/> if the operation was successful.</para>
	/// <para>-or-</para>
	/// <para><see langword="false"/> if the operation was unsuccessful or the object has already been disposed.</para>
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// <para>If <paramref name="taskCompletionSource"/> is <see langword="null"/>.</para>
	/// <para>-or-</para>
	/// <para>If <paramref name="task"/> is <see langword="null"/>.</para>
	/// </exception>
	public static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Task task, TResult result)
	{
		switch (task.Status)
		{
			case TaskStatus.RanToCompletion:
				return taskCompletionSource.TrySetResult(result);

			case TaskStatus.Faulted:
				return taskCompletionSource.TrySetException(task.Exception.InnerExceptions);

			case TaskStatus.Canceled:
				return taskCompletionSource.TrySetCanceled();

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}

	/// <summary>Transfers the result of a canceled or faulted <see cref="Task"/> to the <see cref="TaskCompletionSource{TResult}"/>.</summary>
	/// <typeparam name="TResult">Specifies the type of the result.</typeparam>
	/// <param name="taskCompletionSource">The TaskCompletionSource.</param>
	/// <param name="task">The task whose completion results should be transferred.</param>
	public static void SetFromFailedTask<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, Task task)
	{
		switch (task.Status)
		{
			case TaskStatus.Faulted:
				taskCompletionSource.SetException(task.Exception.InnerExceptions);
				break;

			case TaskStatus.Canceled:
				taskCompletionSource.SetCanceled();
				break;

			case TaskStatus.RanToCompletion:
				throw new InvalidOperationException("Failed tasks must be in the Canceled or Faulted state.");

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}
	public static void SetFromFailedTask(this TaskCompletionSource taskCompletionSource, Task task)
	{
		switch (task.Status)
		{
			case TaskStatus.Faulted:
				taskCompletionSource.SetException(task.Exception.InnerExceptions);
				break;

			case TaskStatus.Canceled:
				taskCompletionSource.SetCanceled();
				break;

			case TaskStatus.RanToCompletion:
				throw new InvalidOperationException("Failed tasks must be in the Canceled or Faulted state.");

			default:
				throw new InvalidOperationException("The task was not completed.");
		}
	}
}

/// <summary>
/// The included numeric extension methods utilize experimental CLR behavior to allow generic numerical operations. Might work great, might have hidden perf costs?
/// </summary>
public static class zz_Extensions_Numeric
{
	public static T _Round<T>(this T value, int digits, MidpointRounding mode = MidpointRounding.AwayFromZero) where T : IFloatingPoint<T>
	{
		return T.Round(value, digits, mode);
	}
}


//public static class zz_Extensions_IEnumerable
//{
//	public static T _Sum<T>(this IEnumerable<T> values) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
//	{
//		var toReturn = T.AdditiveIdentity;
//		foreach (var val in values)
//		{
//			toReturn += val;
//		}
//		return toReturn;
//	}
//	public static T _Avg<T>(this IEnumerable<T> values) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IDivisionOperators<T, float, T>
//	{
//		var count = 0;
//		var toReturn = T.AdditiveIdentity;
//		foreach (var val in values)
//		{
//			count++;
//			toReturn += val;
//		}
//		return toReturn / count;
//	}
//	public static T _Min<T>(this IEnumerable<T> values) where T : IMinMaxValue<T>, IComparisonOperators<T, T>
//	{
//		var toReturn = T.MaxValue;

//		foreach (var val in values)
//		{
//			if (toReturn > val)
//			{
//				toReturn = val;
//			}
//		}
//		return toReturn;
//	}
//	public static T _Max<T>(this IEnumerable<T> values) where T : IMinMaxValue<T>, IComparisonOperators<T, T>
//	{
//		var toReturn = T.MinValue;

//		foreach (var val in values)
//		{
//			if (toReturn < val)
//			{
//				toReturn = val;
//			}
//		}
//		return toReturn;
//	}
//}

public static class zz_Extensions_Task
{
	/// <summary>
	/// like Task.ContinueWith() but if the task is already completed, gives the callback an opportunity to complete synchronously.
	/// </summary>
	/// <param name="task"></param>
	/// <param name="callback"></param>
	/// <returns></returns>
	public static Task _ContinueWithSyncOrAsync(this Task task, Func<Task, Task> callback)
	{
		if (task.IsCompleted)
		{
			return callback(task);
		}
		else
		{
			return task.ContinueWith(callback);
		}
	}
}
public static class zz_Extensions_Dictionary
{

	public static TValue _GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> onAddNew) where TKey : notnull
	{
		if (!dict.TryGetValue(key, out var value))
		{
			value = onAddNew();
			dict.Add(key, value);
		}
		return value;
	}

	/// <summary>
	/// get by reference!   ref returns allow efficient storage of structs in dictionaries
	/// These are UNSAFE in that further modifying (adding/removing) the dictionary while using the ref return will break things!
	/// </summary>
	public static ref TValue _GetValueRef_Unsafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out bool exists)
		where TKey : notnull
		where TValue : struct
	{
		ref var toReturn = ref CollectionsMarshal.GetValueRefOrNullRef(dict, key);
		exists = System.Runtime.CompilerServices.Unsafe.IsNullRef(ref toReturn) == false;
		return ref toReturn;
	}
	public static ref TValue _GetValueRef_Unsafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
		where TKey : notnull
		where TValue : struct
	{
		return ref CollectionsMarshal.GetValueRefOrNullRef(dict, key);
	}
	public static ref TValue _GetValueRefOrAddDefault_Unsafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out bool exists)
		where TKey : notnull
		where TValue : struct
	{
		return ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out exists);
	}
	public static unsafe ref TValue _GetValueRefOrAddDefault_Unsafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
		where TKey : notnull
		where TValue : struct
	{
		bool exists;
		return ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out *&exists);
	}
	//	//below is bad pattern:  instead just set the ref returned value to the new.  (avoid struct copy)
	//	public static unsafe ref TValue _GetValueRefOrAdd_Unsafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func_Ref<TValue> onAddNew) 
	//		where TKey : notnull
	//		where TValue : struct
	//	{		
	//		bool exists;
	//		ref var toReturn = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out *&exists);
	//		if (exists != true)
	//		{
	//			ref var toAdd = ref onAddNew();
	//			dict.Add(key, toAdd);
	//#if DEBUG
	//			toReturn = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out *&exists);
	//			__DEBUG.Assert(exists);
	//#else
	//			toReturn = ref dict._GetValueRef_Unsafe(key);
	//#endif		
	//		}
	//		return ref toReturn;
	//	}
}

/// <summary>
/// The included numeric extension methods utilize experimental CLR behavior to allow generic numerical operations. Might work great, might have hidden perf costs?
/// </summary>
public static class zz_Extensions_Span
{
	[ThreadStatic]
	static Random _rand = new();

	public static void _Randomize<T>(this Span<T> target)
	{
		//lock (_rand)
		{
			for (var index = 0; index < target.Length; index++)
			{
				var swapIndex = _rand.Next(0, target.Length);
				var value = target[index];
				target[index] = target[swapIndex];
				target[swapIndex] = value;
			}
		}
	}

	/// <summary>
	/// returns true if both spans starting address in memory is the same.  Different length and/or type is ignored.
	/// </summary>
	public static unsafe bool _ReferenceEquals<T1, T2>(ref this Span<T1> target,ref Span<T2> other) where T1: unmanaged where T2: unmanaged
	{
		fixed (T1* pSpan1 = target)
		{
			fixed (T2* pSpan2 = other)
			{
				return pSpan1 == pSpan2;
			}
		}	

	}
	/// <summary>
	/// cast this span as another.  Any extra bytes remaining are ignored (the number of bytes in the castTo may be smaller than the original)
	/// </summary>
	public static unsafe Span<TTo> _CastAs<TFrom, TTo>(ref this Span<TFrom> target) where TFrom : unmanaged where TTo : unmanaged
	{
		return MemoryMarshal.Cast<TFrom, TTo>(target);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<T> _AsReadOnly<T>(this Span<T> span)
	{
		return span;
	}


	///// <summary>
	///// get ref to item at index 0
	///// </summary>
	//public static ref T _GetRef<T>(this Span<T> span)
	//{		
	//	return System.Runtime.InteropServices.MemoryMarshal.GetReference(span);
	//}


	
	public static T _Sum<T>(this Span<T> values) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
	{
		return values._AsReadOnly()._Sum();
	}
	public static T _Sum<T>(this ReadOnlySpan<T> values) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
	{
		var toReturn = T.AdditiveIdentity;
		foreach (var val in values)
		{
			toReturn += val;
		}
		return toReturn;
	}

	public static T _Avg<T>(this Span<T> values) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IDivisionOperators<T, float, T>
	{
		return values._AsReadOnly()._Avg();
	}
	public static T _Avg<T>(this ReadOnlySpan<T> values) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>, IDivisionOperators<T, float, T>
	{
		var count = 0;
		var toReturn = T.AdditiveIdentity;
		foreach (var val in values)
		{
			count++;
			toReturn += val;
		}
		return toReturn / count;
	}
	public static T _Min<T>(this Span<T> values) where T : IMinMaxValue<T>, IComparisonOperators<T, T>
	{
		return values._AsReadOnly()._Min();
	}
	public static T _Min<T>(this ReadOnlySpan<T> values) where T : IMinMaxValue<T>, IComparisonOperators<T, T>
	{
		var toReturn = T.MaxValue;

		foreach (var val in values)
		{
			if (toReturn > val)
			{
				toReturn = val;
			}
		}
		return toReturn;
	}
	public static T _Max<T>(this Span<T> values) where T : IMinMaxValue<T>, IComparisonOperators<T, T>
	{
		return values._AsReadOnly()._Max();
	}
	public static T _Max<T>(this ReadOnlySpan<T> values) where T : IMinMaxValue<T>, IComparisonOperators<T, T>
	{
		var toReturn = T.MinValue;

		foreach (var val in values)
		{
			if (toReturn < val)
			{
				toReturn = val;
			}
		}
		return toReturn;
	}
}


public static class zz_Extensions_Timespan
{
	/// <summary>
	/// given an interval, find the previous occurance of that interval's multiple. (prior to this timespan).  
	/// <para>If This timespan is precisely a multiple of interval, itself will be returned.</para>
	/// </summary>
	public static TimeSpan _IntervalPrior(this TimeSpan target, TimeSpan interval)
	{
		var remainder = target.Ticks % interval.Ticks;
		return TimeSpan.FromTicks(target.Ticks - remainder);
	}
	/// <summary>
	/// given an interval, find the next occurance of that interval's multiple.
	/// </summary>
	public static TimeSpan _IntervalNext(this TimeSpan target, TimeSpan interval)
	{
		return target._IntervalPrior(interval) + interval;
	}
}
