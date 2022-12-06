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
//using NotNot.Godot.Scratch.Dodge2dTutorial;

namespace Dodge2dTutorial;
public class Game
{
	public Node2D Root { get; init; }

	public Player player;

	public void Initialize()
	{

		//GODOT BUG:  printing doesn't work when running via VS.  a workaround: add arguments `>out.log 2>&1` to the Launch Profile,  then open in VSCode extension "Log Viewer".  then  vscode window is basically a view of stdout+stderr.  stderr is serious lagged (write buffered?) but eventually works if app keeps running.
		{
			Console.WriteLine("ready!");
			GD.Print("ready TACO");
			GD.PrintErr("boo!");
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

		//var player = CreateAddPlayer();

		var player = new Player();
		player.Position = Root._FindParent<Window>().ContentScaleSize / 2; //center of screen
		Root.AddChild(player);

		var cust = new CustomNode2D();
		Root.AddChild(cust);


	}

	public void _Process(double delta)
	{

	}


	
}

/// <summary>
/// https://docs.godotengine.org/en/latest/tutorials/2d/custom_drawing_in_2d.html
/// </summary>
public partial class CustomNode2D : Node2D
{
	private Texture2D _texture = new PlaceholderTexture2D() { Size = new Vector2(100, 50) };

	private float _rotationAngle = 50;
	private float _angleFrom = 75;
	private float _angleTo = 195;

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
		DrawTexture(_texture, new Vector2(100,100));


		var center = new Vector2(200, 200);
		float radius = 80;
		//float angleFrom = 75;
		//float angleTo = 195;
		var color = new Color(1, 0, 0);
		this.DrawCircleArc(center, radius, _angleFrom, _angleTo, color);

		color = Colors.WebGreen;
		center = new Vector2(300, 300);
		this.DrawCircleArcPoly(center, radius, _angleFrom, _angleTo, color);

	}
	public override void _Process(double delta)
	{
		base._Process(delta);

		_angleFrom += _rotationAngle * (float)delta;
		_angleTo += _rotationAngle * (float)delta;

		if (_angleFrom > 360 && _angleTo > 360)
		{
			_angleTo %= 360;
			_angleFrom %= 360;
		}

		this.QueueRedraw();
	}



	/// <summary>
	/// https://docs.godotengine.org/en/latest/tutorials/2d/custom_drawing_in_2d.html?highlight=low%20level#arc-polygon-function
	/// </summary>
	public void DrawCircleArcPoly(Vector2 center, float radius, float angleFrom, float angleTo, Color color)
	{
		//unsafe
		{
			int nbPoints = 32;

			
			////sadly, can't use this because it could allocate an array bigger than what we want, and the godot `DrawPolygon` method doesn't take a length.
			//using var spanGuard = NotNot.Bcl.SpanGuard<Vector2>.Allocate(nbPoints + 1);
			//var pointsArc = spanGuard.DangerousGetArray().Array; 
			var pointsArc = new Vector2[nbPoints + 1];
			
			pointsArc[0] = center;
			var colors = new Color[] { color };

			for (int i = 1; i <= nbPoints; i++) //BUG: should start at index 1, because 0 should be center?
			{
				float anglePoint = Mathf.DegToRad(angleFrom + i * (angleTo - angleFrom) / nbPoints - 90);
				pointsArc[i] = center + new Vector2(Mathf.Cos(anglePoint), Mathf.Sin(anglePoint)) * radius;
			}
			this.DrawPolygon(pointsArc, colors);
		}
	}



	/// <summary>
	/// example showing how to draw custom 2d
	/// https://docs.godotengine.org/en/latest/tutorials/2d/custom_drawing_in_2d.html?highlight=low%20level
	/// </summary>
	public void DrawCircleArc(Vector2 center, float radius, float angleFrom, float angleTo, Color color)
	{
		unsafe
		{
			int nbPoints = 32;
			var pointsArc = stackalloc Vector2[nbPoints + 1]; // new Vector2[nbPoints + 1];

			for (int i = 0; i <= nbPoints; i++)
			{

				float anglePoint = Mathf.DegToRad(angleFrom + i * (angleTo - angleFrom) / nbPoints - 90f);
				pointsArc[i] = center + new Vector2(Mathf.Cos(anglePoint), Mathf.Sin(anglePoint)) * radius;
			}

			for (int i = 0; i < nbPoints - 1; i++)
			{
				this.DrawLine(pointsArc[i], pointsArc[i + 1], color);
			}
		}
	}


}
