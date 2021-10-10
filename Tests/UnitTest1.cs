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
			//var updater = new NotNot.Engine.SimpleUpdater();
			engine.Updater = new NotNot.Engine.SimpleUpdater();
			engine.Initialize();

			engine.Updater.Start();


			await Task.Delay(100);

			await engine.Updater.Stop();
			__ERROR.Assert(engine.DefaultWorld._lastUpdate._timeStats._frameId > 30);
			engine.Dispose();
			__ERROR.Throw(engine.IsDisposed);

		}
	}
}
