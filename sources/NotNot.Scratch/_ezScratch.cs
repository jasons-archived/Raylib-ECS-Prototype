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


namespace NotNot;

////////////////////////////////////////////////
//EZGAME ON HOLD: This is meant to be an easy way to get started.  put on hold until after a good playable demo is done.
////////////////////////////////////////////////

public abstract class EzGame
{

	
	public Engine engine;
	public void Initialize()
	{
		engine = new Engine();
		engine.Initialize();
		//add renderer
		engine.Rendering.AddChild(new NotNot.Rendering.RenderReferenceImplementationSystem());
	}

	public void Start()
	{
		engine.Updater.Start();
	}
	public Task OnStop()
	{
		return engine.Updater.Stop();
	}
	public abstract void OnInitialize();

	public abstract Task OnStart();


}

public static class SomeIndirection
{
	public static int[] values = new int[100];
}

public class FakeArray
{
	public ref int this[int index] => ref SomeIndirection.values[index];

	public static void Test()
	{
		FakeArray fakeArray = new FakeArray();
		ref var x = ref fakeArray[22];
		x = 42;
		if (SomeIndirection.values[22] != x)
		{
			throw new Exception("this exception won't be thrown");
		}
	}
}
