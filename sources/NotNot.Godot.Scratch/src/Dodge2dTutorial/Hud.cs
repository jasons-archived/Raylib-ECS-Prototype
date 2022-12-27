// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]
// [!!] Copyright ©️ NotNot Project and Contributors.
// [!!] By default, this file is licensed to you under the AGPL-3.0.
// [!!] However a Private Commercial License is available.
// [!!] See the LICENSE.md file in the project root for more info.
// [!!] -------------------------------------------------
// [!!] Contributions Guarantee Citizenship!
// [!!] Would you like to know more? https://github.com/NotNotTech/NotNot
// [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!] [!!]  [!!] [!!] [!!] [!!]

using System.Threading.Tasks;
using Godot;
using NotNot.Godot;

namespace Dodge2dTutorial;

/// <summary>
/// create hud, from https://docs.godotengine.org/en/latest/getting_started/first_2d_game/06.heads_up_display.html
/// </summary>
public partial class Hud : CanvasLayer
{
	public Label ScoreLabel { get; set; } = new()
	{
		Text="888",
		
	};
	public Label Message { get; set; } = new()
	{
		Text="Dodge the creepy guys!!!"
	};
	public Button StartButton { get; set; } = new()
	{
		Text="START"
	};
	public Timer MessageTimer { get; set; } = new()
	{
		
	};

	public override void _EnterTree()
	{
		//load the Xolonium-Regular.ttf font and set that as the override for ScoreLabel
		var font = GD.Load<FontFile>("res://asset/fonts/Xolonium-Regular.ttf");


		//adjust score
		{
			AddChild(ScoreLabel);
			//if you look at the docs of FontFile, it shows a code snippet:
			///// <para>var f = ResourceLoader.Load&lt;FontFile&gt;("res://BarlowCondensed-Bold.ttf");</para>
			///// <para>GetNode("Label").Set("custom_fonts/font", f);</para>
			///// <para>GetNode("Label").Set("custom_font_sizes/font_size", 64);</para>
			// the above doesn't actually work.  however...
			// from that I guessed the name "font" and "font_size" below, which works.
			ScoreLabel.AddThemeFontOverride("font", font);
			ScoreLabel.AddThemeFontSizeOverride("font_size", 64);
			ScoreLabel.LayoutMode =(int) Control.LayoutPreset.TopWide;
			ScoreLabel.HorizontalAlignment = HorizontalAlignment.Center;


			ScoreLabel.SetPositionCenterPercentScreenSpaceX(0.5f);


		}

		//adjust message
		{

			AddChild(Message);
			Message.AddThemeFontOverride("font", font);
			Message.AddThemeFontSizeOverride("font_size", 64);
			Message.LayoutMode = (int)Control.LayoutPreset.HcenterWide;
			Message.HorizontalAlignment = HorizontalAlignment.Center;
			Message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			Message.Size = new(600, 20);
			Message.SetPositionCenterPercentScreenSpace(0.5f, 0.5f);
		}
		//adjust start button
		{
			AddChild(StartButton);
			StartButton.AddThemeFontOverride("font", font);
			StartButton.AddThemeFontSizeOverride("font_size", 64);
			StartButton.LayoutMode = (int)Control.LayoutPreset.CenterBottom;
			StartButton.Size = new(300, 20);
			StartButton.SetPositionCenterPercentScreenSpace(0.5f, 0.9f);

			StartButton.Pressed += () =>
			{
				StartButton.Hide();
				EmitSignal(SignalName.StartGame);
				
			};
		}
		//adjust message timer
		{
			AddChild(MessageTimer);
			//MessageTimer.WaitTime = 2;
			MessageTimer.OneShot = true;
			MessageTimer.Timeout += () => { Message.Hide(); };
			

		}

	}


	[Signal]
	public delegate void StartGameEventHandler();

	public void ShowMessage(string text, double waitTime=2)
	{
		Message.Text = text;
		Message.Show();
		MessageTimer.WaitTime = waitTime;
		MessageTimer.Start();
	}

	public async Task ShowGameOver()
	{
		ShowMessage("Game Over");

		//await MessageTimer._ToSignal(nameof(MessageTimer.Timeout));
		await MessageTimer._TimeoutAwaiter();
		Message.Text = "Dodge the Creepy crapes! They are cooked too long!";
		Message.Show();


		await GetTree().CreateTimer(1)._TimeoutAwaiter();
		StartButton.Show();
	}

	public void UpdateScore(int score)
	{
		ScoreLabel.Text = score.ToString();
	}
	
	public override void _Process(double delta)
	{
		base._Process(delta);





		//StartButton.HorizontalAlignment = HorizontalAlignment.Center;
		//StartButton.CustomMinimumSize = new(100, 100);


		//add 200 margin to StartButton
		//StartButton.MarginLeft = 200;
		//StartButton.MarginRight = 200;
		//StartButton.

		//StartButton.OffsetTop = 500;
		//StartButton.OffsetBottom = -100;
		//StartButton.SetPositionCenterPercentScreenSpace(0.5f, 0.9f);
		//StartButton.SetAnchorsAndOffsetsPreset
	}
}
