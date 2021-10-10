using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotNot.Bcl.Diagnostics;
using System;
using System.Threading.Tasks;

namespace Tests
{
	[TestClass]
	public class EcsTests
	{
		[TestMethod]
		public async Task BasicEngine_StartStop()
		{
			var engine = new NotNot.Engine.Engine();
			var updater = new NotNot.Engine.SimpleUpdater();
			engine.Updater = updater;
			engine.Initialize();

			var endTask = Task.Run(async () =>
			{
				await Task.Delay(100);
				await engine.Updater.Stop();				
			});
			await updater.Run();

			await endTask;

			engine.Dispose();

			__ERROR.Throw(engine.IsDisposed);

		}
	}
}
