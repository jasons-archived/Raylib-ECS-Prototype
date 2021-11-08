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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotNot.Bcl.Threading;

/// <summary>
/// A custom task scheduler, allowing control over what threads a task executes on 
/// </summary>
/// <remarks>from https://www.wisdomjobs.com/e-university/c-dot-net-tutorial-225/using-a-custom-task-schedular-542.html
/// 
/// </remarks>
public class CustomTaskScheduler : TaskScheduler, IDisposable
{
	private BlockingCollection<Task> taskQueue;
	private Thread[] threads;
	public CustomTaskScheduler(int concurrency)
	{
		// initialize the collection and the thread array
		taskQueue = new BlockingCollection<Task>();
		threads = new Thread[concurrency];
		// create and start the threads
		for (int i = 0; i < threads.Length; i++)
		{
			threads[i] = new Thread(() =>
			{
				// loop while the blocking collection is not
				// complete and try to execute the next task
				foreach (Task t in taskQueue.GetConsumingEnumerable())
				{
					TryExecuteTask(t);
				}
			});
			threads[i].Start();
		}
	}
	protected override void QueueTask(Task task)
	{
		if (task.CreationOptions.HasFlag(TaskCreationOptions.LongRunning))
		{
			// create a dedicated thread to execute this task
			new Thread(() =>
			{
				TryExecuteTask(task);
			}).Start();
		}
		else
		{
			// add the task to the queue
			taskQueue.Add(task);
		}
	}
	protected override bool TryExecuteTaskInline(Task task,
	bool taskWasPreviouslyQueued)
	{
		// only allow inline execution if the executing thread is one
		// belonging to this scheduler
		if (threads.Contains(Thread.CurrentThread))
		{
			return TryExecuteTask(task);
		}
		else
		{
			return false;
		}
	}
	public override int MaximumConcurrencyLevel
	{
		get
		{
			return threads.Length;
		}
	}
	protected override IEnumerable<Task> GetScheduledTasks()
	{
		return taskQueue.ToArray();
	}
	public void Dispose()
	{
		// mark the collection as complete
		taskQueue.CompleteAdding();
		// wait for each of the threads to finish
		foreach (Thread t in threads)
		{
			t.Join();
		}
	}
}
/// <summary>
/// from https://www.wisdomjobs.com/e-university/c-dot-net-tutorial-225/using-a-custom-task-schedular-542.html
/// </summary>
internal class Listing_22_ExampleUsage
{
	static void Main(string[] args)
	{
		// get the processor count for the system
		int procCount = Environment.ProcessorCount;
		// create a custom scheduler
		CustomTaskScheduler scheduler = new CustomTaskScheduler(procCount);
		Console.WriteLine("Custom scheduler ID: {0}", scheduler.Id);
		Console.WriteLine("Default scheduler ID: {0}", TaskScheduler.Default.Id);
		// create a cancellation token source
		CancellationTokenSource tokenSource = new CancellationTokenSource();
		// create a task
		Task task1 = new Task(() =>
		{
			Console.WriteLine("Task {0} executed by scheduler {1}",
			Task.CurrentId, TaskScheduler.Current.Id);
			// create a child task - this will use the same
			// scheduler as its parent
			Task.Factory.StartNew(() =>
			{
				Console.WriteLine("Task {0} executed by scheduler {1}",
				Task.CurrentId, TaskScheduler.Current.Id);
			});
			// create a child and specify the default scheduler
			Task.Factory.StartNew(() =>
			{
				Console.WriteLine("Task {0} executed by scheduler {1}",
				Task.CurrentId, TaskScheduler.Current.Id);
			}, tokenSource.Token, TaskCreationOptions.None, TaskScheduler.Default);
		});
		// start the task using the custom scheduler
		task1.Start(scheduler);
		// create a continuation - this will use the default scheduler
		task1.ContinueWith(antecedent =>
		{
			Console.WriteLine("Task {0} executed by scheduler {1}",
			Task.CurrentId, TaskScheduler.Current.Id);
		});
		// create a continuation using the custom scheduler
		task1.ContinueWith(antecedent =>
		{
			Console.WriteLine("Task {0} executed by scheduler {1}",
			Task.CurrentId, TaskScheduler.Current.Id);
		}, scheduler);


	}
}
