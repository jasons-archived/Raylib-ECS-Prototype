using Godot;
using System;
using NotNot.Bcl.Diagnostics;
using Dodge2dTutorial;
using System.Diagnostics;

public partial class Root : Node2D
{
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.AddChild(new Dodge2dTutorialGame());
	}



	private double elapsed = 0;
	private double lastElapsed = 0;
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		elapsed += delta;

		//print to console once per second
		if (((int)lastElapsed) != ((int)elapsed))
		{
			//Console.WriteLine($"taco {delta:0N} WasPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.WasPaused} IsPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.IsPaused}");
			Console.WriteLine("testing Console.WriteLine()");
			GD.Print("testing GD.Print()");
			GD.PrintErr("testing GD.PrintErr()");
		}
		
		lastElapsed = elapsed;
		



	}
}
