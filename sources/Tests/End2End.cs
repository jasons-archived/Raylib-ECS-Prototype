// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using NotNot.Rendering;

namespace Tests.End2End
{
	[TestClass]
	public class End2End
	{




		[TestMethod]
		public async Task EcsWorld()
		{
			{
				var engine = new Engine();
				//engine.Updater = new NotNot.HeadlessUpdater();
				engine.Initialize();
				//engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });

				engine.DefaultWorld.AddChild(new MoveSystem() { Name = "MOVE" });
				engine.DefaultWorld.AddChild(new TestInputSystem() { Name = "PLAYER" });

				engine.Updater.Start();
				await Task.Delay(10);

				var em = engine.DefaultWorld.entityManager;

				var archetype = em.GetOrCreateArchetype(new() { typeof(TestInput), typeof(WorldXform), typeof(Move) });

				em.EnqueueCreateEntity(1, archetype, (args) =>
				{
					var (accessTokens, entityHandles, archetype) = args;
					foreach (var accessToken in accessTokens)
					{
						ref var transform = ref accessToken.GetComponentWriteRef<WorldXform>();
						ref var move = ref accessToken.GetComponentWriteRef<Move>();
						transform = new WorldXform();// { value = Vector3.One };
						move = new() { pos = Vector3.Zero };						
					}
				});

				await Task.Delay(100);
				await engine.Updater.Stop();
			}

			GC.Collect();
			await Task.Delay(100);


		}

	



		[TestMethod]
		public async Task EcsWorldWithRendering()
		{
			{
				var engine = new Engine();
				//engine.Updater = new NotNot.HeadlessUpdater();
				engine.Initialize();
				//engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });

				engine.DefaultWorld.Phase2_Simulation.AddChild(new MoveSystem());
				engine.DefaultWorld.Phase2_Simulation.AddChild(new TestInputSystem());
				engine.Rendering.AddChild(new RenderReferenceImplementationSystem());

				engine.DefaultWorld.Phase2_Simulation.AddChild(new RenderPacketGenerationSystem());
				engine.Updater.Start();
				await Task.Delay(10);

				var em = engine.DefaultWorld.entityManager;

				var archetype = em.GetOrCreateArchetype(new() { typeof(TestInput), typeof(WorldXform), typeof(Move), typeof(IsVisible) });

				em.EnqueueCreateEntity(1, archetype, (args) =>
				{
					var (accessTokens, entityHandles, archetype) = args;
					foreach (var accessToken in accessTokens)
					{
						ref var transform = ref accessToken.GetComponentWriteRef<WorldXform>();
						ref var move = ref accessToken.GetComponentWriteRef<Move>();
						transform = new WorldXform();// { value = Vector3.One };
						move = new() { pos = Vector3.Zero };
					}
				});
				
				await Task.WhenAny(engine.RunningTask,Task.Delay(100000000));
				await engine.Updater.Stop();
			}

			GC.Collect();
			await Task.Delay(100);


		}


	}


}
