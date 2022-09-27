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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using NotNot.Godot;

namespace Dodge2dTutorial;
public class Game
{
	public Node2D Root { get; init; }

	public Area2D player;

	public void Initialize()
	{

		//GODOT BUG:  printing doesn't work when running via VS.  a workaround: add arguments `>out.log 2>&1` to the Launch Profile,  then open in VSCode extension "Log Viewer".  then  vscode window is basically a view of stdout+stderr.  stderr is serious lagged (write buffered?) but eventually works if app keeps running.
		{
			//Console.WriteLine("ready!");
			//GD.Print("ready TACO");
			//GD.PrintErr("boo!");
		}

		//DOCS https://docs.godotengine.org/en/latest/getting_started/first_2d_game/01.project_setup.html
		//customize window
		{
			var window = Root._FindParent<Window>();
			window.Size = new Vector2i(1024, 768);
			window.ContentScaleSize = window.Size; //set content view to be same as window dimensions
												   //window.ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;

			//adjust window for easier debugging
			{
				//always on top
				window.AlwaysOnTop = true;
				//all the way to the right side of screen
				var screenId = window.CurrentScreen;
				var screenSize = DisplayServer.ScreenGetSize(screenId);
				var windowPos = window.Position;
				windowPos.x = screenSize.x - window.Size.x;
				window.Position = windowPos;
			}

		}

		var player = CreateAddPlayer();

		var cust = new CustomNode2D();
		Root.AddChild(cust);


	}

	public void _Process(double delta)
	{

	}



	public Area2D CreateAddPlayer()
	{

		//DOCS: https://docs.godotengine.org/en/latest/getting_started/first_2d_game/02.player_scene.html#creating-the-player-scene
		{
			var player = new Area2D();
			player.Name = "player";
			var sprite = new AnimatedSprite2D();
			sprite.Frames = new SpriteFrames();
			sprite.Frames.RenameAnimation("default", "walk");
			sprite.Frames.RemoveAnimation("default"); //default is still in node list, but this line doesn't seem to remove it either.....
			sprite.Frames.AddAnimation("up");


			var godotTx = GD.Load<Texture2D>("res://asset/icon.svg");
			
			var smallImg = godotTx.GetImage();
			smallImg.Resize((int)(smallImg.GetWidth() * 0.95), (int)(smallImg.GetHeight() * 0.95));
			var smallTx = ImageTexture.CreateFromImage(smallImg);
			sprite.Frames.AddFrame("walk", godotTx);
			sprite.Frames.AddFrame("walk", smallTx);


			//sprite.Frames.AddFrame("walk", GD.Load<Texture2D>("res://assets/art/playerGrey_walk1.png"));
			//sprite.Frames.AddFrame("walk", GD.Load<Texture2D>("res://assets/art/playerGrey_walk2.png"));
			//sprite.Frames.AddFrame("up", GD.Load<Texture2D>("res://assets/art/playerGrey_up1.png"));
			//sprite.Frames.AddFrame("up", GD.Load<Texture2D>("res://assets/art/playerGrey_up2.png"));



			sprite.Play("walk");

			player.AddChild(sprite);



			var col = new CollisionShape2D();
			var capsule = new CapsuleShape2D(); //TODO: set dimensions of capsule
												//capsule.Height *= 0.9f;
												//capsule.Radius *= 0.9f;
			col.Shape = capsule;

			player.AddChild(col);


			sprite.Scale = Vector2.One;
			player.Scale = Vector2.One;



			Root.AddChild(player);

			player.Position = Root._FindParent<Window>().ContentScaleSize / 2; //center of screen

			return player;
		}

	}
}


public partial class CustomNode2D : Node2D
{
	private Texture2D _texture = new PlaceholderTexture2D() { Size = new Vector2(100, 50) };
	public Texture2D Texture
	{
		get
		{
			return _texture;
		}

		set
		{
			_texture = value;
			this.QueueRedraw();
		}
	}

	public override void _Draw()
	{
		//DrawTexture(_texture, new Vector2());


		var center = new Vector2(200, 200);
		float radius = 80;
		float angleFrom = 75;
		float angleTo = 195;
		var color = new Color(1, 0, 0);
		this._DrawCircleArc(center, radius, angleFrom, angleTo, color);



	}
	public override void _Process(double delta)
	{
		base._Process(delta);
		//   _texture = new PlaceholderTexture2D() { Size = new Vector2(100, 50) };



	}

}
