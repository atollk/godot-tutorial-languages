using System;
using Godot;
using NodeGetterGenerators;

namespace tutorial;

[GenerateNodeGetter(typeof(Node), "Node")]
[VerifyNodeGetters("Player/AnimatedSprite2D")]
public partial class PlayerAnimatedSprite2d : AnimatedSprite2D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print(GetNodeNode());
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    public void SetVelocity(Vector2 velocity)
    {
        if (velocity.X != 0)
        {
            Play();
            Animation = "walk";
            FlipH = velocity.X < 0;
            FlipV = false;
        }
        else if (velocity.Y != 0)
        {
            Play();
            Animation = "up";
            FlipH = false;
            FlipV = velocity.Y > 0;
        }
        else
        {
            Stop();
        }
    }
}
