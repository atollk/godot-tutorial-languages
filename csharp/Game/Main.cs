using System.Threading.Tasks;
using Godot;
using NodeGetterGenerators;

namespace tutorial;

[VerifyNodeGetters("Main")]
public partial class Main : Node
{
    [Export]
    public PackedScene? MobScene { get; set; }

    [Node("Player")]
    private partial Player Player { get; }

    [Node("StartTimer")]
    private partial Timer StartTimer { get; }

    [Node("MobSpawnTimer")]
    private partial Timer MobSpawnTimer { get; }

    [Node("MobSpeedTimer")]
    private partial Timer MobSpeedTimer { get; }

    [Node("ScoreTimer")]
    private partial Timer ScoreTimer { get; }

    [Node("HUD")]
    private partial Hud HUD { get; }

    [Node("StartPosition")]
    private partial Marker2D StartPosition { get; }

    [Node("MobPath/MobSpawnLocation")]
    private partial PathFollow2D MobPathMobSpawnLocation { get; }

    [Node("Music")]
    private partial AudioStreamPlayer2D Music { get; }

    [Node("DeathSound")]
    private partial AudioStreamPlayer2D DeathSound { get; }

    private int _score;
    private int _mobSpeed = 100;

    public override void _Ready()
    {
        base._Ready();
        MobSpawnTimer.Timeout += OnMobSpawnTimerTimeout;
        MobSpeedTimer.Timeout += OnMobSpeedTimerTimeout;
        ScoreTimer.Timeout += OnScoreTimerTimeout;
        StartTimer.Timeout += OnStartTimerTimeout;
        Player.Hit += OnPlayerHit;
        HUD.StartGame += OnHudStartGame;
        MobSpawnTimer.Start(3);
    }

    private void NewGame()
    {
        _score = 0;
        _mobSpeed = 100;
        Player.Start(StartPosition.Position);
        StartTimer.Start();
        HUD.UpdateScore(_score);
        HUD.ShowMessage("Get Ready", fade: true);
        Music.Play();
        GetTree().CallGroup(Mob.MobGroup, Node.MethodName.QueueFree);
    }

    private async Task GameOver()
    {
        _mobSpeed = 100;
        ScoreTimer.Stop();
        MobSpawnTimer.Start(3);
        MobSpeedTimer.Stop();
        Music.Stop();
        DeathSound.Play();
        await HUD.ShowGameOver();
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
        var mobSpawnLocation = MobPathMobSpawnLocation;
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
        MobSpawnTimer.WaitTime = 100.0 / _mobSpeed;
    }

    private void OnScoreTimerTimeout()
    {
        _score += 1;
        HUD.UpdateScore(_score);
    }

    private void OnStartTimerTimeout()
    {
        MobSpeedTimer.Start();
        MobSpawnTimer.Start(1);
        ScoreTimer.Start();
    }

    private void OnHudStartGame()
    {
        NewGame();
    }
}
