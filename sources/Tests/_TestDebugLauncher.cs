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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public static class _TestDebugLauncher
{
	public static async Task Main(params string[] args)
	{
		//var task = new Task(() => { }).ContinueWith((task)=>Test());


		//await Task.Delay(10000);



		//await task;

		//var testBcl = new Tests.Internals.BclBasic();
		//await testBcl.DotNextAsyncAutoResetEvent();

		var test = new Tests.End2End.End2End();
		await test.EcsWorldWithRendering();
	}







	public static async Task Test()
	{
		await Task.Delay(100);
		Console.WriteLine("HI");

	}
}
