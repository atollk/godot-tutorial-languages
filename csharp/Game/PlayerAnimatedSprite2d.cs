using System;
using Godot;
using NodeGetterGenerators;

namespace tutorial;

[VerifyNodeGetters("Player/AnimatedSprite2D")]
public partial class PlayerAnimatedSprite2d : AnimatedSprite2D
{
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
