
namespace Tests.Internals;
		 
[TestClass]
public class EngineBasic
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
	public async Task SimPipeline_e2e()
	{

		using var manager = new SimManager(null) { };


		var start = Stopwatch.GetTimestamp();
		long lastElapsed = 0;


		//add some test nodes
		manager.Register(new TimestepNodeTest { ParentName = "root", Name = "A", TargetFps = 1 });
		manager.Register(new DelayTest { ParentName = "root", Name = "A2" });

		manager.Register(new DelayTest { ParentName = "A", Name = "B", _updateBefore = { "A2" }, _registeredWriteLocks = { "taco" } });
		manager.Register(new DelayTest { ParentName = "A", Name = "B2", _registeredReadLocks = { "taco" } });
		manager.Register(new DelayTest { ParentName = "A", Name = "B3", _registeredReadLocks = { "taco" } });
		manager.Register(new DelayTest { ParentName = "A", Name = "B4!", _updateAfter = { "A2" }, _registeredReadLocks = { "taco" } });

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




	[TestMethod]
	public async Task Engine_WorldWithChild()
	{
		var engine = new NotNot.Engine.Engine();
		engine.Updater = new NotNot.Engine.HeadlessUpdater();
		engine.Initialize();



		engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });


		engine.Updater.Start();


		await Task.Delay(100);

		await engine.Updater.Stop();
		__ERROR.Assert(engine.DefaultWorld._lastUpdate._timeStats._frameId > 30);
		engine.Dispose();
		__ERROR.Throw(engine.IsDisposed);

        GC.Collect();
        await Task.Delay(100);
    }





	[TestMethod]
	public async Task Ecs_CreateEntity()
	{
		var engine = new NotNot.Engine.Engine();
		engine.Updater = new NotNot.Engine.HeadlessUpdater();
		engine.Initialize();
		engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });
		engine.Updater.Start();


		var em = engine.DefaultWorld.entityManager;

		var archetype = em.GetOrCreateArchetype(new(){typeof(int),typeof(bool)});

		em.EnqueueCreateEntity(100, archetype, (args) => {
            var (accessTokens, entityHandles, archetype) = args;
            foreach(var accessToken in accessTokens)
            {
                ref var cInt = ref accessToken.GetComponentWriteRef<int>();
                ref var cBool = ref accessToken.GetComponentWriteRef<bool>();
                cInt = accessToken.entityHandle.id;
                cBool = cInt % 2 == 0;
            }
        });


		await Task.Delay(100);

		await engine.Updater.Stop();
		__ERROR.Assert(engine.DefaultWorld._lastUpdate._timeStats._frameId > 30);
		engine.Dispose();
		__ERROR.Throw(engine.IsDisposed);

	}




}

