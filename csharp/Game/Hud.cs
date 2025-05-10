using System.Globalization;
using System.Threading.Tasks;
using Godot;
using NodeGetterGenerators;
using Timer = Godot.Timer;

namespace tutorial;

[GenerateNodeGetter(typeof(Label), "Message", cache: true)]
[GenerateNodeGetter(typeof(Label), "ScoreLabel")]
[GenerateNodeGetter(typeof(Button), "StartButton")]
[GenerateNodeGetter(typeof(Timer), "MessageTimer")]
[VerifyNodeGetters("HUD")]
public partial class Hud : CanvasLayer
{
    [Signal]
    public delegate void StartGameEventHandler();

    public override void _Ready()
    {
        base._Ready();
        GetNodeStartButton().Pressed += OnStartButtonPressed;
        GetNodeMessageTimer().Timeout += OnMessageTimerTimeout;
    }

    public void OnStartButtonPressed()
    {
        GetNodeStartButton().Hide();
        EmitSignal(SignalName.StartGame);
    }

    public void OnMessageTimerTimeout()
    {
        GetNodeMessage().Hide();
    }

    public void ShowMessage(string text, bool fade)
    {
        var message = GetNodeMessage();
        message.Text = text;
        message.Show();
        if (fade)
            GetNodeMessageTimer().Start();
    }

    public void UpdateScore(float score)
    {
        GetNodeScoreLabel().Text = score.ToString(CultureInfo.CurrentCulture);
    }

    public async Task ShowGameOver()
    {
        ShowMessage("Game Over", true);
        await ToSignal(GetNodeMessageTimer(), Timer.SignalName.Timeout);
        ShowMessage("Dodge The Creeps!", false);
        await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);
        GetNodeStartButton().Show();
    }
}
