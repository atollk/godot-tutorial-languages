using System.Threading.Tasks;
using Godot;
using NodeGetterGenerators;

namespace tutorial;

[GenerateNodeGetter(typeof(Player), "Player")]
[GenerateNodeGetter(typeof(Timer), "StartTimer")]
[GenerateNodeGetter(typeof(Timer), "MobSpawnTimer")]
[GenerateNodeGetter(typeof(Timer), "MobSpeedTimer")]
[GenerateNodeGetter(typeof(Timer), "ScoreTimer")]
[GenerateNodeGetter(typeof(Hud), "HUD")]
[GenerateNodeGetter(typeof(Marker2D), "StartPosition")]
[GenerateNodeGetter(typeof(PathFollow2D), "MobPath/MobSpawnLocation")]
[GenerateNodeGetter(typeof(AudioStreamPlayer2D), "Music")]
[GenerateNodeGetter(typeof(AudioStreamPlayer2D), "DeathSound")]
[VerifyNodeGetters("Main")]
public partial class Main : Node
{
    [Export]
    public PackedScene? MobScene { get; set; }

    private int _score;
    private int _mobSpeed = 100;

    public override void _Ready()
    {
        base._Ready();
        GetNodeMobSpawnTimer().Timeout += OnMobSpawnTimerTimeout;
        GetNodeMobSpeedTimer().Timeout += OnMobSpeedTimerTimeout;
        GetNodeScoreTimer().Timeout += OnScoreTimerTimeout;
        GetNodeStartTimer().Timeout += OnStartTimerTimeout;
        GetNodePlayer().Hit += OnPlayerHit;
        GetNodeHUD().StartGame += OnHudStartGame;
        GetNodeMobSpawnTimer().Start(3);
    }

    private void NewGame()
    {
        _score = 0;
        _mobSpeed = 100;
        GetNodePlayer().Start(GetNodeStartPosition().Position);
        GetNodeStartTimer().Start();
        GetNodeHUD().UpdateScore(_score);
        GetNodeHUD().ShowMessage("Get Ready", fade: true);
        GetNodeMusic().Play();
        GetTree().CallGroup(Mob.MobGroup, Node.MethodName.QueueFree);
    }

    private async Task GameOver()
    {
        _mobSpeed = 100;
        GetNodeScoreTimer().Stop();
        GetNodeMobSpawnTimer().Start(3);
        GetNodeMobSpeedTimer().Stop();
        GetNodeMusic().Stop();
        GetNodeDeathSound().Play();
        await GetNodeHUD().ShowGameOver();
    }

    private async void OnPlayerHit()
    {
        await GameOver();
    }

    private void OnMobSpawnTimerTimeout()
    {
        if (MobScene == null)
        {
            GD.PrintErr("Could not spawn enemy, as MobScene is not set.");
            return;
        }

        var mob = MobScene.Instantiate<Mob>();
        var mobSpawnLocation = GetNodeMobPathMobSpawnLocation();
        mobSpawnLocation.ProgressRatio = GD.Randf();
        mob.Position = mobSpawnLocation.Position;

        var direction = mobSpawnLocation.Rotation + Mathf.Pi / 2;
        direction += (float)GD.RandRange(-Mathf.Pi / 4, Mathf.Pi / 4);
        mob.Rotation = direction;

        var velocity = new Vector2((float)(GD.RandRange(0.75, 1.5) * _mobSpeed), 0);
        mob.LinearVelocity = velocity.Rotated(direction);

        AddChild(mob);
    }

    private void OnMobSpeedTimerTimeout()
    {
        _mobSpeed += 10;
        GetNodeMobSpawnTimer().WaitTime = 100.0 / _mobSpeed;
    }

    private void OnScoreTimerTimeout()
    {
        _score += 1;
        GetNodeHUD().UpdateScore(_score);
    }

    private void OnStartTimerTimeout()
    {
        GetNodeMobSpeedTimer().Start();
        GetNodeMobSpawnTimer().Start(1);
        GetNodeScoreTimer().Start();
    }

    private void OnHudStartGame()
    {
        NewGame();
    }
}
