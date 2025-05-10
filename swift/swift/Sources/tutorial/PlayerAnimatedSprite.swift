import SwiftGodot

@Godot
class PlayerAnimatedSprite : AnimatedSprite2D {
    func setVelocity(_ velocity: Vector2) {
        if (velocity.x != 0)
        {
            self.play()
            self.animation = "walk"
            self.flipH = velocity.x < 0
            self.flipV = false
        }
        else if (velocity.y != 0)
        {
            self.play()
            self.animation = "up"
            self.flipH = false
            self.flipV = velocity.y > 0
        }
        else
        {
            self.stop()
        }
    }
}