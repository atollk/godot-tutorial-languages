using Godot;
using NodeGetterGenerators;

namespace tutorial;

[GenerateNodeGetter(typeof(AnimatedSprite2D), "AnimatedSprite2D")]
[GenerateNodeGetter(typeof(VisibleOnScreenNotifier2D), "VisibleOnScreenNotifier2D")]
[VerifyNodeGetters("Mob")]
public partial class Mob : RigidBody2D
{
    public const string MobGroup = "mobs";

    public override void _Ready()
    {
        base._Ready();
        var mobTypes = GetNodeAnimatedSprite2D().SpriteFrames.GetAnimationNames();
        GetNodeAnimatedSprite2D().Animation = mobTypes[GD.Randi() % mobTypes.Length];
        GetNodeVisibleOnScreenNotifier2D().ScreenExited += OnScreenExited;
        AddToGroup(MobGroup);
    }

    private void OnScreenExited()
    {
        QueueFree();
    }
}
