using System.Globalization;
using System.Threading.Tasks;
using Godot;
using NodeGetterGenerators;
using Timer = Godot.Timer;

namespace tutorial;

[VerifyNodeGetters("HUD")]
public partial class Hud : CanvasLayer
{
    [Node("Message", cache: true)]
    private partial Label MessageLabel { get; }

    [Node("ScoreLabel")]
    private partial Label ScoreLabel { get; }

    [Node("StartButton")]
    private partial Button StartButton { get; }

    [Node("MessageTimer")]
    private partial Timer MessageTimer { get; }

    [Signal]
    public delegate void StartGameEventHandler();

    public override void _Ready()
    {
        base._Ready();
        StartButton.Pressed += OnStartButtonPressed;
        MessageTimer.Timeout += OnMessageTimerTimeout;
    }

    public void OnStartButtonPressed()
    {
        StartButton.Hide();
        EmitSignal(SignalName.StartGame);
    }

    public void OnMessageTimerTimeout()
    {
        MessageLabel.Hide();
    }

    public void ShowMessage(string text, bool fade)
    {
        MessageLabel.Text = text;
        MessageLabel.Show();
        if (fade)
            MessageTimer.Start();
    }

    public void UpdateScore(float score)
    {
        ScoreLabel.Text = score.ToString(CultureInfo.CurrentCulture);
    }

    public async Task ShowGameOver()
    {
        ShowMessage("Game Over", true);
        await ToSignal(MessageTimer, Timer.SignalName.Timeout);
        ShowMessage("Dodge The Creeps!", false);
        await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);
        StartButton.Show();
    }
}
