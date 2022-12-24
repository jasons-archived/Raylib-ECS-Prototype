// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System.Diagnostics;
using System.Reflection;

namespace GodotConsoleLauncher;

internal class Program
{
	static async Task Main(string[] args)
	{

		Console.WriteLine("enter main!");
		//var assembly = Assembly.LoadFile("C:\\repos\\NotNot\\tools\\.godot-bin\\Godot_v4.0-beta8_mono_win64\\Godot_v4.0-beta8_mono_win64.exe");
		//var type = assembly.GetType("Godot.OS");

		//assembly.EntryPoint.Invoke(null, new object[] { new string[] { "--path", "." } });

		var startInfo = new ProcessStartInfo()
		{
			FileName = "C:\\repos\\NotNot\\tools\\.godot-bin\\Godot_v4.0-beta8_mono_win64\\Godot_v4.0-beta8_mono_win64.exe",
			Arguments = "--path .",
			UseShellExecute = false,
			CreateNoWindow = true,
			//RedirectStandardOutput = true,
			//RedirectStandardError = true,
			//RedirectStandardInput = true,
			WorkingDirectory = "C:\\repos\\NotNot\\sources\\NotNot.Godot.Scratch",
			
		};
		var process = Process.Start(startInfo);

		Console.WriteLine("WaitForExitAsync!");
		
		while (process.HasExited == false)
		{
			//var line = await process.StandardOutput.ReadLineAsync();
			//Console.WriteLine(line);
			await Task.Delay(100);
		}

		
		
		//await process.WaitForExitAsync();

		Console.WriteLine("done!");
	}
}
