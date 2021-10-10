using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotNot.Bcl.Diagnostics;
using NotNot.Engine._internal.ExecPipeline;
using scratch_console.Helpers.Pipeline;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Tests.Internals;
	[TestClass]
	public class BasicWorkflow
	{
		[TestMethod]
		public async Task Engine_StartStop()
		{
			var engine = new NotNot.Engine.Engine();
			engine.Updater = new NotNot.Engine.HeadlessUpdater();
			engine.Initialize();

			engine.Updater.Start();


			await Task.Delay(100);

			await engine.Updater.Stop();
			__ERROR.Assert(engine.DefaultWorld._lastUpdate._timeStats._frameId > 30);
			engine.Dispose();
			__ERROR.Throw(engine.IsDisposed);

		}

		[TestMethod]
		public async Task ExecutionFramework_e2e()
		{

			using var manager = new SimManager() { };


			var start = Stopwatch.GetTimestamp();
			long lastElapsed = 0;


			//add some test nodes
			manager.Register(new TimestepNodeTest { ParentName = "root", Name = "A", TargetFps = 1 });
			manager.Register(new DelayTest { ParentName = "root", Name = "A2" });

			manager.Register(new DelayTest { ParentName = "A", Name = "B", _updateBefore = { "A2" }, _writeResources = { "taco" } });
			manager.Register(new DelayTest { ParentName = "A", Name = "B2", _readResources = { "taco" } });
			manager.Register(new DelayTest { ParentName = "A", Name = "B3", _readResources = { "taco" } });
			manager.Register(new DelayTest { ParentName = "A", Name = "B4!", _updateAfter = { "A2" }, _readResources = { "taco" } });

			manager.Register(new DelayTest { ParentName = "B", Name = "C" });
			manager.Register(new DelayTest { ParentName = "B", Name = "C2" });
			manager.Register(new DelayTest { ParentName = "B", Name = "C3", _updateAfter = { "C" } });




			manager.Register(new DebugPrint { ParentName = "C3", Name = "DebugPrint" });


			var loop = 0;
			while (true)
			{

				loop++;
				lastElapsed = Stopwatch.GetTimestamp() - start;
				start = Stopwatch.GetTimestamp();
				//Console.WriteLine($" ======================== {loop} ({Math.Round(TimeSpan.FromTicks(lastElapsed).TotalMilliseconds,1)}ms) ============================================== ");
				await manager.Update(TimeSpan.FromTicks(lastElapsed));
				//Console.WriteLine($"last Elapsed = {lastElapsed}");
				if (loop > 2000)
				{
					break;
				}
			}
			


		}
	}

