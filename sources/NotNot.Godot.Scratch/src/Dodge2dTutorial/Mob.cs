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
using NotNot.Bcl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotNot.Godot.Scratch.src.Dodge2dTutorial;
public partial class Mob : RigidBody2D
{
	public AnimatedSprite2D Sprite { get; set; }
	public CollisionShape2D CollisionShape { get; set; }
	public VisibleOnScreenNotifier2D VisibleOnScreenNotifier { get; set; }

	public override void _EnterTree()
	{
		base._EnterTree();

		//initialize new objects and add them to the scene
		Sprite = new AnimatedSprite2D();
		AddChild(Sprite);
		CollisionShape = new CollisionShape2D();
		AddChild(CollisionShape);

		VisibleOnScreenNotifier = new VisibleOnScreenNotifier2D();
		AddChild(VisibleOnScreenNotifier);

		//reduce size
		Sprite.Scale = new(0.75f, 0.75f);

		this.GravityScale = 0f;
		this.CollisionMask = 0;

		//setup enemy animation
		{
			Sprite.Frames = new SpriteFrames();
			Sprite.Frames.RemoveAnimation("default");
			Sprite.SpeedScale = 2;

			Sprite.Frames.AddAnimation("fly");
			Sprite.Frames.AddFrame("fly", GD.Load<Texture2D>("res://asset/art/enemyFlyingAlt_1.png"));
			Sprite.Frames.AddFrame("fly", GD.Load<Texture2D>("res://asset/art/enemyFlyingAlt_2.png"));
			Sprite.Frames.AddAnimation("swim");
			Sprite.Frames.AddFrame("swim", GD.Load<Texture2D>("res://asset/art/enemySwimming_1.png"));
			Sprite.Frames.AddFrame("swim", GD.Load<Texture2D>("res://asset/art/enemySwimming_2.png"));
			Sprite.Frames.AddAnimation("walk");
			Sprite.Frames.AddFrame("walk", GD.Load<Texture2D>("res://asset/art/enemyWalking_1.png"));
			Sprite.Frames.AddFrame("walk", GD.Load<Texture2D>("res://asset/art/enemyWalking_2.png"));
		}

		//setup enemy collision
		{
			//setup enemy collision based on sprite transparency
			{
				var sprite = Sprite.Frames.GetFrame("fly", 0);
				var image = sprite.GetImage();
				var bb = image.GetUsedRect();
				var circle = new CircleShape2D();
				circle.Radius = bb.Size.x / 2;
				CollisionShape.Shape = circle;
			}
		}

	}

	public override void _Ready()
	{

		//play a random animation
		{
			//var mobTypes = Sprite.Frames.GetAnimationNames();
			//var random = new Random();
			//var mobType = mobTypes[random.Next(0, mobTypes.Length)];
			//Sprite.Play(mobType); ;

			Sprite.Play(Sprite.Frames.GetAnimationNames()._PickRandom());
		}

		//connect to the visible on screen notifier
		{
			VisibleOnScreenNotifier.ScreenExited += VisibleOnScreenNotifier_ScreenExited;

			
		}

	}

	private void VisibleOnScreenNotifier_ScreenExited()
	{
		QueueFree();
		
	}
	
}
