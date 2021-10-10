

using NotNot.Bcl;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine.Ecs;
using NotNot.Engine.Sim;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Engine;

public class Engine : DisposeGuard
{
	private SimManager _simManager = new();

	public RootNode RootNode { get => _simManager._root; }

	public World DefaultWorld { get; set; } = new() { Name="DefaultWorld"};

	public IUpdate Updater;

	public void Initialize()
	{
		__ERROR.Throw(Updater != null, "you must set the Updater property before calling Initialize()");

		Updater.Update += OnUpdate;


		if (DefaultWorld != null)
		{
			DefaultWorld.Initialize();
			RootNode.AddChild(DefaultWorld);
		}
	}

	protected async ValueTask OnUpdate(TimeSpan elapsed)
	{
		await _simManager.Update(elapsed);

		//return ValueTask.CompletedTask;
	}

	protected override void OnDispose()
	{
		Updater.Dispose();
		Updater = null;
		_simManager.Dispose();
		__ERROR.Throw(DefaultWorld.IsDisposed == true,"disposing simManager should have disposed all nodes inside");
		DefaultWorld = null;
		_simManager = null;

		base.OnDispose();
	}

}

public interface IUpdate : IDisposable
{
	public event Func<TimeSpan, ValueTask> Update;
}

public class SimpleUpdater : DisposeGuard, IUpdate
{
	public event Func<TimeSpan, ValueTask> Update;

	public async void Run()
	{

		var start = Stopwatch.GetTimestamp();
		long lastElapsed = 0;

		var loop = 0;
		while (true && IsDisposed == false)
		{
			loop++;
			lastElapsed = Stopwatch.GetTimestamp() - start;
			start = Stopwatch.GetTimestamp();
			//Console.WriteLine($" ======================== {loop} ({Math.Round(TimeSpan.FromTicks(lastElapsed).TotalMilliseconds,1)}ms) ============================================== ");
			await Update(TimeSpan.FromTicks(lastElapsed));
			//Console.WriteLine($"last Elapsed = {lastElapsed}");
		}
	}
}

