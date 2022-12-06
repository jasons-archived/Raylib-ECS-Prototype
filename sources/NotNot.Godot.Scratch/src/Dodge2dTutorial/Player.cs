// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] 
// [!!] Copyright ©️ NotNot Project and Contributors. 
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available. 
// [!!] See the LICENSE.md file in the project root for more info. 
// [!!] ------------------------------------------------- 
// [!!] Contributions Guarantee Citizenship! 
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot 
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dodge2dTutorial;
public partial class Player : Area2D
{
	[Export] public int speed = 400;
	//public Vector2 ScreenSize;
	//[Export]
	AnimatedSprite2D sprite;

	public override void _Ready()
	{
		base._Ready();

		var player = this;


		player.Name = "player";


		sprite = new AnimatedSprite2D();
		sprite.Frames = new SpriteFrames();
		sprite.Frames.RenameAnimation("default", "walk");
		sprite.Frames
			.RemoveAnimation(
				"default"); //default is still in node list, but this line doesn't seem to remove it either.....
		sprite.Frames.AddAnimation("up");

		//godot icon for player
		var godotTx = GD.Load<Texture2D>("res://asset/icon.svg");

		//bobbing in/out for idle/walk
		var smallImg = godotTx.GetImage();
		smallImg.Resize((int)(smallImg.GetWidth() * 0.95), (int)(smallImg.GetHeight() * 0.95));
		var smallTx = ImageTexture.CreateFromImage(smallImg);
		sprite.Frames.AddFrame("walk", godotTx);
		sprite.Frames.AddFrame("walk", smallTx);

		//rotating around x axis for "jump"
		{
			var rotImg1 = smallTx.GetImage();
			var rotImg2 = smallTx.GetImage();
			rotImg1.Resize((int)(godotTx.GetWidth() * 0.66), (int)(godotTx.GetHeight()));
			rotImg2.Resize((int)(godotTx.GetWidth() * 0.18), (int)(godotTx.GetHeight()));
			sprite.Frames.AddFrame("up", smallTx);
			sprite.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg1));
			sprite.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg2));
			sprite.Frames.AddFrame("up", null);
			sprite.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg2));
			sprite.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg1));

			sprite.Frames.SetAnimationSpeed("up", 20);
		}







		//smallTx.GetImage().GetUsedRect

		//add a custom image
		{
			//var testImage = new Image();
			//testImage.Create(godotTx.GetWidth(), godotTx.GetHeight(), false, Image.Format.Rgba8);
			//testImage.Fill(Godot.Colors.White);
			//sprite.Frames.AddFrame("walk", ImageTexture.CreateFromImage(testImage));
		}


		//sprite.Frames.AddFrame("walk", GD.Load<Texture2D>("res://assets/art/playerGrey_walk1.png"));
		//sprite.Frames.AddFrame("walk", GD.Load<Texture2D>("res://assets/art/playerGrey_walk2.png"));
		//sprite.Frames.AddFrame("up", GD.Load<Texture2D>("res://assets/art/playerGrey_up1.png"));
		//sprite.Frames.AddFrame("up", GD.Load<Texture2D>("res://assets/art/playerGrey_up2.png"));



		sprite.Play("walk");
		//sprite.Play("up");

		player.AddChild(sprite);

		//Set collision shape of player
		{

			var col = new CollisionShape2D();

			var circle = new CircleShape2D() { Radius = smallImg.GetUsedRect().Size.x / 2f };
			col.Shape = circle;

			//var capsule = new CapsuleShape2D()
			//{
			//	Radius = smallImg.GetUsedRect().Size.x / 2f,

			//};
			//									//capsule.Height *= 0.9f;
			//									//capsule.Radius *= 0.9f;
			//col.Shape = capsule;

			player.AddChild(col);
		}

		//sprite.Scale = Vector2.One;
		//player.Scale = Vector2.One;




		//Root.AddChild(player);

	}


	public override void _Process(double delta)
	{
		//base._Process(delta);

		

		//if (sprite != null)
		{

			var velocity = Vector2.Zero;

			if (Input.IsActionPressed("move_right"))
			{
				velocity += Vector2.Right;
			}

			if (Input.IsActionPressed("move_left"))
			{
				velocity += Vector2.Left;
			}

			if (Input.IsActionPressed("move_up"))
			{
				velocity += Vector2.Up;
				//GD.Print("got move_up");
				//sprite.Play("up");
				sprite.SetAnimation("up");
			}

			if (Input.IsActionPressed("move_down"))
			{
				velocity += Vector2.Down;
				sprite.SetAnimation("up");
			}

			if (velocity.LengthSquared() > 1)
			{
				velocity = velocity.Normalized();
			}

			if (velocity.LengthSquared() > 0)
			{
				velocity = velocity.Normalized() * speed;
				sprite.Play();

				//((AnimatedSprite2D)GetChild(0)).Play("walk");
			}
			else
			{
				sprite.SetAnimation("walk");
				sprite.Stop();
				//((AnimatedSprite2D)GetChild(0)).Play("up");
			}
		}
	}

}
