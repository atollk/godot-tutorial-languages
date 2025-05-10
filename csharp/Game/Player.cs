using Godot;
using NodeGetterGenerators;

namespace tutorial;

[VerifyNodeGetters("Player")]
public partial class Player : Area2D
{
    [Node("CollisionShape2D")]
    private partial CollisionShape2D CollisionShape2D { get; }

    [Node("AnimatedSprite2D")]
    private partial PlayerAnimatedSprite2d PlayerAnimatedSprite { get; }

    [Node("GPUParticles2D")]
    private partial GpuParticles2D GpuParticles2D { get; }

    [Export]
    public int Speed { get; set; } = 400;

    [Signal]
    public delegate void HitEventHandler();

    private Vector2 _screenSize;

    public override void _Ready()
    {
        base._Ready();
        _screenSize = GetViewportRect().Size;
        Hide();
        BodyEntered += OnBodyEntered;
    }

    public void Start(Vector2 position)
    {
        Position = position;
        Show();
        CollisionShape2D.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        GpuParticles2D.Restart();
    }

    private static Vector2 GetMovementVector()
    {
        var velocity = Vector2.Zero;
        if (Input.IsActionPressed(Constants.Inputs.MoveRight))
            velocity.X += 1;

        if (Input.IsActionPressed(Constants.Inputs.MoveLeft))
            velocity.X -= 1;

        if (Input.IsActionPressed(Constants.Inputs.MoveUp))
            velocity.Y -= 1;

        if (Input.IsActionPressed(Constants.Inputs.MoveDown))
            velocity.Y += 1;

        return velocity;
    }

    private void SetSpriteVelocity(Vector2 velocity)
    {
        var animatedSprite = PlayerAnimatedSprite;
        animatedSprite.SetVelocity(velocity);
        GpuParticles2D.Emitting = animatedSprite.IsPlaying();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        var velocity = GetMovementVector();
        if (velocity.Length() > 0)
        {
            velocity = velocity.Normalized() * Speed;
            Position += velocity * (float)delta;
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, _screenSize.X),
                Mathf.Clamp(Position.Y, 0, _screenSize.Y)
            );
        }
        SetSpriteVelocity(velocity);
    }

    private void OnBodyEntered(Node2D body)
    {
        Hide();
        EmitSignal(SignalName.Hit);
        CollisionShape2D.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
    }
}
