// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] This file is licensed to you under the MPL-2.0.
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace NotNot.Bcl.Threading.Advanced;

/// <summary>
/// allows toggling the boolean singleThreaded static variable, which will then cause calls to .Run() to run tasks one-at-a-time (synchronously) allowing easier single-thread debugging 
/// </summary>
public static class DebuggableTaskFactory
{
	/// <summary>
	/// set to TRUE to run tasks synchronously.
	/// </summary>
	static public bool singleThreaded = false;
	static LimitedConcurrencyLevelTaskScheduler lcts = new(1);
	static TaskFactory singleThreadedFactory = new(lcts);
	/// <summary>
	/// the underlying Task Factory that the .Run() method points to.   (Single Threaded if the singleThreaded==true, or the default Task.Factory otherwise)
	/// </summary>
	public static TaskFactory Factory
	{
		get
		{
			if (singleThreaded)
			{
				return singleThreadedFactory;
			}
			else
			{
				return Task.Factory;
			}
		}
	}

	public static Task Run(Action action)
	{
		return Factory.Run(action);
	}
	public static Task Run<TResult>(Func<TResult> action)
	{
		return Factory.Run(action);
	}
	public static Task Run(Func<Task> action)
	{
		return Factory.Run(action);
	}
	public static Task Run<TResult>(Func<Task<TResult>> action)
	{
		return Factory.Run(action);


	}
}

/// <summary>
/// Provides a task scheduler that ensures a maximum concurrency level while
/// running on top of the thread pool.
/// </summary>
/// <remarks>From https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=net-6.0</remarks>
public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
	// Indicates whether the current thread is processing work items.
	[ThreadStatic]
	private static bool _currentThreadIsProcessingItems;

	// The list of tasks to be executed
	private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

	// The maximum concurrency level allowed by this scheduler.
	private readonly int _maxDegreeOfParallelism;

	// Indicates whether the scheduler is currently processing work items.
	private int _delegatesQueuedOrRunning = 0;

	// Creates a new instance with the specified degree of parallelism.
	public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
	{
		if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
		_maxDegreeOfParallelism = maxDegreeOfParallelism;
	}

	// Queues a task to the scheduler.
	protected sealed override void QueueTask(Task task)
	{
		// Add the task to the list of tasks to be processed.  If there aren't enough
		// delegates currently queued or running to process tasks, schedule another.
		lock (_tasks)
		{
			_tasks.AddLast(task);
			if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
			{
				++_delegatesQueuedOrRunning;
				NotifyThreadPoolOfPendingWork();
			}
		}
	}

	// Inform the ThreadPool that there's work to be executed for this scheduler.
	private void NotifyThreadPoolOfPendingWork()
	{
		ThreadPool.UnsafeQueueUserWorkItem(_ =>
		{
			// Note that the current thread is now processing work items.
			// This is necessary to enable inlining of tasks into this thread.
			_currentThreadIsProcessingItems = true;
			try
			{
				// Process all available items in the queue.
				while (true)
				{
					Task item;
					lock (_tasks)
					{
						// When there are no more items to be processed,
						// note that we're done processing, and get out.
						if (_tasks.Count == 0)
						{
							--_delegatesQueuedOrRunning;
							break;
						}

						// Get the next item from the queue
						item = _tasks.First.Value;
						_tasks.RemoveFirst();
					}

					// Execute the task we pulled out of the queue
					base.TryExecuteTask(item);
				}
			}
			// We're done processing items on the current thread
			finally { _currentThreadIsProcessingItems = false; }
		}, null);
	}

	// Attempts to execute the specified task on the current thread.
	protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
	{
		// If this thread isn't already processing a task, we don't support inlining
		if (!_currentThreadIsProcessingItems) return false;

		// If the task was previously queued, remove it from the queue
		if (taskWasPreviouslyQueued)
			// Try to run the task.
			if (TryDequeue(task))
				return base.TryExecuteTask(task);
			else
				return false;
		else
			return base.TryExecuteTask(task);
	}

	// Attempt to remove a previously scheduled task from the scheduler.
	protected sealed override bool TryDequeue(Task task)
	{
		lock (_tasks) return _tasks.Remove(task);
	}

	// Gets the maximum concurrency level supported by this scheduler.
	public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

	// Gets an enumerable of the tasks currently scheduled on this scheduler.
	protected sealed override IEnumerable<Task> GetScheduledTasks()
	{
		bool lockTaken = false;
		try
		{
			Monitor.TryEnter(_tasks, ref lockTaken);
			if (lockTaken) return _tasks;
			else throw new NotSupportedException();
		}
		finally
		{
			if (lockTaken) Monitor.Exit(_tasks);
		}
	}
}
