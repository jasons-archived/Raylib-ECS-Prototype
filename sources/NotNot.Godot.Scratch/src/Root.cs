using Godot;
using System;
using NotNot.Bcl.Diagnostics;
using Dodge2dTutorial;
using System.Diagnostics;

public partial class Root : Node2D
{

	Game game;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Debugger.Launch();
		
		game = new Game();// {Root=this};
		//game.Initialize();
		this.AddChild(game);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Console.WriteLine($"taco {delta:0N} WasPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.WasPaused} IsPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.IsPaused}");

		GD.Print("gd.print sorcery! mango!");
		GD.PrintErr("gd.printErr");
		//ame._Process(delta);

	}
}
