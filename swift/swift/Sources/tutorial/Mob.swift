import SwiftGodot

class Mob: RigidBody2D {
    @Node("AnimatedSprite2D") var animatedSprite2D: AnimatedSprite2D
    @Node("VisibleOnScreenNotifier2D") var visibleOnScreenNotifier2D: VisibleOnScreenNotifier2D

    static let MobGroup = "mobs"

    override func _ready() {
        let mobTypes = self.animatedSprite2D.spriteFrames?.getAnimationNames()
        if let mobTypes = mobTypes {
            // TODO
            let y = GD.randi() % mobTypes.size()
            let x = mobTypes[Int(y)]
            animatedSprite2D.animation = StringName(x)
        }
        self.visibleOnScreenNotifier2D.screenExited.connect {
            self.onScreenExited()
        }
        self.addToGroup(StringName(Mob.MobGroup))
    }

    private func onScreenExited() {
        self.queueFree()
    }
}
