using Godot;
using System;
using NotNot.Bcl.Diagnostics;

public partial class Root : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Console.WriteLine($"taco {delta} WasPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.WasPaused} IsPaused? {NotNot.Bcl.Diagnostics.Advanced.DebuggerInfo.IsPaused}");
	}
}
