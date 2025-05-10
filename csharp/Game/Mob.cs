using Godot;
using NodeGetterGenerators;

namespace tutorial;

[VerifyNodeGetters("Mob")]
public partial class Mob : RigidBody2D
{
    public const string MobGroup = "mobs";

    [Node("AnimatedSprite2D")]
    private partial AnimatedSprite2D AnimatedSprite2D { get; }

    [Node("VisibleOnScreenNotifier2D")]
    private partial VisibleOnScreenNotifier2D VisibleOnScreenNotifier2D { get; }

    public override void _Ready()
    {
        base._Ready();
        var mobTypes = AnimatedSprite2D.SpriteFrames.GetAnimationNames();
        AnimatedSprite2D.Animation = mobTypes[GD.Randi() % mobTypes.Length];
        VisibleOnScreenNotifier2D.ScreenExited += OnScreenExited;
        AddToGroup(MobGroup);
    }

    private void OnScreenExited()
    {
        QueueFree();
    }
}
