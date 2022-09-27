using Godot;
using System;
using NotNot.Bcl.Diagnostics;
using Dodge2dTutorial;

public partial class Root : Node2D
{

	Game game;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		game = new Game(){Root=this};
		game.Initialize();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//Console.WriteLine($"taco {delta:0n} WasPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.WasPaused} IsPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.IsPaused}");

		game._Process(delta);

	}
}
