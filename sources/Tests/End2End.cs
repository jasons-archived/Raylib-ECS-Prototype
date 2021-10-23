// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

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
				engine.Updater = new NotNot.Engine.HeadlessUpdater();
				engine.Initialize();
				//engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });

				engine.DefaultWorld.AddChild(new MoveSystem() { Name = "MOVE" });
				engine.DefaultWorld.AddChild(new PlayerInputSystem() { Name = "PLAYER" });

				engine.Updater.Start();
				await Task.Delay(10);

				var em = engine.DefaultWorld.entityManager;

				var archetype = em.GetOrCreateArchetype(new() { typeof(PlayerInput), typeof(Translation), typeof(Move) });

				em.EnqueueCreateEntity(1, archetype, (args) =>
				{
					var (accessTokens, entityHandles, archetype) = args;
					foreach (var accessToken in accessTokens)
					{
						ref var translation = ref accessToken.GetComponentWriteRef<Translation>();
						ref var move = ref accessToken.GetComponentWriteRef<Move>();
						translation = new Translation() { value = Vector3.One };
						move = new() { value = Vector3.Zero };
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
				engine.Updater = new NotNot.Engine.HeadlessUpdater();
				engine.Initialize();
				//engine.DefaultWorld.AddChild(new TimestepNodeTest() { TargetFps = 10 });

				engine.DefaultWorld.AddChild(new MoveSystem());
				engine.DefaultWorld.AddChild(new PlayerInputSystem());
				engine.Rendering.AddChild(new RaylibRendering());

				engine.Updater.Start();
				await Task.Delay(10);

				var em = engine.DefaultWorld.entityManager;

				var archetype = em.GetOrCreateArchetype(new() { typeof(PlayerInput), typeof(Translation), typeof(Move) });

				em.EnqueueCreateEntity(1, archetype, (args) =>
				{
					var (accessTokens, entityHandles, archetype) = args;
					foreach (var accessToken in accessTokens)
					{
						ref var translation = ref accessToken.GetComponentWriteRef<Translation>();
						ref var move = ref accessToken.GetComponentWriteRef<Move>();
						translation = new Translation() { value = Vector3.One };
						move = new() { value = Vector3.Zero };
					}
				});
				
				await Task.WhenAny(engine.RunningTask,Task.Delay(10000));
				await engine.Updater.Stop();
			}

			GC.Collect();
			await Task.Delay(100);


		}


	}
}
