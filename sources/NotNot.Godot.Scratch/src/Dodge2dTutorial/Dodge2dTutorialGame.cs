// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

global using NotNot.Bcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using NotNot.Godot;
using NotNot.Godot.Scratch.src.Dodge2dTutorial;
//using NotNot.Godot.Scratch.Dodge2dTutorial;

namespace Dodge2dTutorial;
public partial class Dodge2dTutorialGame : Node
{
	//public Node2D Root { get; private set; }

	public int Score;
	public Player player;
	public Random _rand = new();

	/// <summary>
	/// size of the computer screen game window is being displayed on
	/// </summary>
	private Vector2i ScreenSize { get; set; }


	/// <summary>
	/// the size of the viewport
	/// </summary>
	public Vector2 ViewportSize { get; private set; }

	public override void _EnterTree()
	{
		//DOCS https://docs.godotengine.org/en/latest/getting_started/first_2d_game/01.project_setup.html
		//customize window
		{
			var window = this._FindParent<Window>();
			window.Size = new Vector2i(1024, 768);
			window.ContentScaleSize = window.Size; //set content view to be same as window dimensions
												   //window.ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;

			window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;

			var screenId = window.CurrentScreen;
			this.ScreenSize = DisplayServer.ScreenGetSize(screenId);
			ViewportSize = GetViewport().GetVisibleRect().Size;

			//adjust window for easier debugging
			{
				//always on top
				window.AlwaysOnTop = true;
				//all the way to the right side of screen
				var windowPos = window.Position;
				windowPos.x = ScreenSize.x - window.Size.x;
				window.Position = windowPos;
			}


		}



		////demo custom 2d drawing
		//var cust = new CustomDrawingExample2D();
		//this.AddChild(cust);
	}

	public override void _Ready()
	{


		player = new Player();
		this.AddChild(player);

		//player.Start(this._FindParent<Window>().ContentScaleSize / 2);//center of screen

		//add props, timers (from https://docs.godotengine.org/en/latest/getting_started/first_2d_game/05.the_main_game_scene.html)
		{
			AddChild(MobTimer);
			AddChild(ScoreTimer);
			AddChild(StartTimer);
			AddChild(StartPosition);
		}

		//spawn mobs
		{
			var curve = new Curve2D();
			//create curve using the extent
			var extent = this.ViewportSize;
			curve.AddPoint(new Vector2(0, 0));
			curve.AddPoint(new Vector2(extent.x, 0));
			curve.AddPoint(new Vector2(extent.x, extent.y));
			curve.AddPoint(new Vector2(0, extent.y));
			curve.AddPoint(new Vector2(0, 0)); //close curve?
			MobPath.Curve = curve;


			var MobSpawnLocation = new PathFollow2D();
			
			MobPath.AddChild(MobSpawnLocation);

			AddChild(MobPath);

		}

		//timer callbacks
		{
			ScoreTimer.Timeout += () => Score += 1;
			StartTimer.Timeout += () =>
			{
				MobTimer.Start();
				ScoreTimer.Start();
			};
			MobTimer.Timeout += () =>
			{
				var mob = new Mob();
				//var spawnLocation = MobPath.GetNode<PathFollow2D>("MobSpawnLocation"); //doesn't work because we didn't name our node
				var spawnPath = MobPath._FindChild<PathFollow2D>()!;
				spawnPath.ProgressRatio = _rand.NextSingle();
				mob.Position = spawnPath.Position;

				//set mob direction perpendicular to the path direction
				var direction = spawnPath.Rotation + MathF.PI/2;
				//add some randomness to direction (using pi)
				direction += _rand._NextSingle(-MathF.PI / 4, MathF.PI / 4);
				mob.Rotation = direction;

				var velocity = new Vector2(_rand._NextSingle(150, 250), 0);
				mob.LinearVelocity = velocity.Rotated(direction);

				AddChild(mob);


			};
		}


		NewGame();
	}

	public void GameOver()
	{
		MobTimer.Stop();
		ScoreTimer.Stop();
	}

	public void NewGame()
	{
		Score = 0;
		//var startPosition = StartPosition;
		player.Start(StartPosition.Position);
		StartTimer.Start();

		//var mobTest = new Mob();
		//AddChild(mobTest);

	}
	

	[Export]
	public Timer MobTimer { get; set; } = new()
	{
		WaitTime = 0.5,
	};
	[Export]
	public Timer ScoreTimer { get; set; } = new()
	{
		WaitTime = 1,
	};
	[Export]
	public Timer StartTimer { get; set; } = new()
	{
		WaitTime = 2,
		OneShot = true,
	};
	[Export]
	public Marker2D StartPosition { get; set; } = new()
	{
		Position = new(240, 450),
	};

	[Export]
	public Path2D MobPath { get; set; } = new()
	{

	};

	//public override void _Process(double delta)
	//{

	//}



}

//public partial class Hud : CanvasLayer
//{

//}
