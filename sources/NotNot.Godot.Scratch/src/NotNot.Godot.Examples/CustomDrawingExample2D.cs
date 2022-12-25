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

namespace Dodge2dTutorial;

/// <summary>
/// https://docs.godotengine.org/en/latest/tutorials/2d/custom_drawing_in_2d.html
/// </summary>
public partial class CustomDrawingExample2D : Node2D
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
		DrawTexture(_texture, new Vector2(100, 100));


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
