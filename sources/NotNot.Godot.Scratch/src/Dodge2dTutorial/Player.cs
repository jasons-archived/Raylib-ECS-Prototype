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
using NotNot.Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dodge2dTutorial;
public partial class Player : Area2D
{
	[Signal]
	public delegate void PlayerHitEventHandler();


	[Export] public int speed = 400;
	//public Vector2 ScreenSize;
	//[Export]
	AnimatedSprite2D animatedSprite2D;

	Game Game;


	private void Player_BodyEntered(Node2D body)
	{
		//throw new NotImplementedException();
		Hide();
		//EmitSignal(nameof(PlayerHitEventHandler));
		EmitSignal(SignalName.PlayerHit);

		//disable collisions from happening again, deffered as it is not safe to do so in the middle of a physics step
		//GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred("disabled", true);
		this._FindChild<CollisionShape2D>().SetDeferred("disabled", true);
	}
	
	public override void _Ready()
	{
		base._Ready();

		this.BodyEntered += Player_BodyEntered;

		var player = this;
		Game = this._FindParent<Game>();

		player.Name = "player";

		//ANIMATIONS
		{
			animatedSprite2D = new AnimatedSprite2D();
			animatedSprite2D.Frames = new SpriteFrames();
			animatedSprite2D.Frames.RenameAnimation("default", "walk");
			animatedSprite2D.Frames
				.RemoveAnimation(
					"default"); //default is still in node list, but this line doesn't seem to remove it either.....
			animatedSprite2D.Frames.AddAnimation("up");

			//godot icon for player
			var godotTx = GD.Load<Texture2D>("res://asset/icon.svg");

			//bobbing in/out for idle/walk
			var smallImg = godotTx.GetImage();
			smallImg.Resize((int)(smallImg.GetWidth() * 0.95), (int)(smallImg.GetHeight() * 0.95));
			var smallTx = ImageTexture.CreateFromImage(smallImg);
			animatedSprite2D.Frames.AddFrame("walk", godotTx);
			animatedSprite2D.Frames.AddFrame("walk", smallTx);

			//rotating around x axis for "jump"
			{
				var rotImg1 = smallTx.GetImage();
				var rotImg2 = smallTx.GetImage();
				rotImg1.Resize((int)(godotTx.GetWidth() * 0.66), (int)(godotTx.GetHeight()));
				rotImg2.Resize((int)(godotTx.GetWidth() * 0.18), (int)(godotTx.GetHeight()));
				animatedSprite2D.Frames.AddFrame("up", smallTx);
				animatedSprite2D.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg1));
				animatedSprite2D.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg2));
				animatedSprite2D.Frames.AddFrame("up", null);
				animatedSprite2D.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg2));
				animatedSprite2D.Frames.AddFrame("up", ImageTexture.CreateFromImage(rotImg1));

				animatedSprite2D.Frames.SetAnimationSpeed("up", 20);
			}
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



		animatedSprite2D.Play("walk");
		//sprite.Play("up");

		player.AddChild(animatedSprite2D);

		//COLLISIONS
		{
			//Set collision shape of player
			
			var col = new CollisionShape2D();

			var sprite = animatedSprite2D.Frames.GetFrame("walk", 0);
			var image = sprite.GetImage();
			var circle = new CircleShape2D() { Radius = image.GetUsedRect().Size.x / 2.1f };
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
		
		//hide player till ready
		this.Hide();
	}


	public void Start(Vector2 pos)
	{
		this.Position = pos;
		this.Show();
		//this.GetNode<CollisionShape2D>("CollisionShape2D").Disabled = false;
		this._FindChild<CollisionShape2D>().Disabled = false;
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
				//sprite.SetAnimation("up");
			}

			if (Input.IsActionPressed("move_down"))
			{
				velocity += Vector2.Down;
				//sprite.SetAnimation("up");
			}


			if (velocity.LengthSquared() > 0)
			{
				velocity = velocity.Normalized() * speed;
				animatedSprite2D.Play();

				//((AnimatedSprite2D)GetChild(0)).Play("walk");
			}
			else
			{
				animatedSprite2D.SetAnimation("walk");
				animatedSprite2D.Stop();
				//((AnimatedSprite2D)GetChild(0)).Play("up");
			}

			Position += velocity * (float)delta;
			//clamp to screen
			Position = Position.Clamp(Vector2.Zero, Game.ViewportSize);

			if (velocity.LengthSquared() > 0)
			{
				animatedSprite2D.Rotation = velocity.Angle()+ Mathf.DegToRad(90);
			}
			else
			{
				animatedSprite2D.Rotation = 0f;
			}
			//if (velocity.x > 0)
			//{
			//	animatedSprite2D.Rotation = Mathf.DegToRad(90);
			//}
			//else if (velocity.x < 0)
			//{
			//	animatedSprite2D.Rotation = Mathf.DegToRad(-90);
			//}
			//else if (velocity.y < 0)
			//{
			//	animatedSprite2D.Rotation = Mathf.DegToRad(0);
			//}
			//else if (velocity.y > 0)
			//{
			//	animatedSprite2D.Rotation = Mathf.DegToRad(180);
			//}
			//else
			//{
			//	animatedSprite2D.Rotation = Mathf.DegToRad(0);
			//}

			////choosing animations
			//if (velocity.x != 0)
			//{
			//	animatedSprite2D.Animation = "walk";
			//	animatedSprite2D.FlipV = false;
			//	// See the note below about boolean assignment.
			//	animatedSprite2D.FlipH = velocity.x < 0;
			//}
			//else if (velocity.y != 0)
			//{
			//	animatedSprite2D.Animation = "up";
			//	animatedSprite2D.FlipV = velocity.y > 0;
			//}

		}
	}

}
