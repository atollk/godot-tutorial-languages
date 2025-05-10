import SwiftGodot

@Godot
class Player: Area2D {
    @Export var speed: Int = 400
    var screenSize: Vector2 = Vector2.zero

    @Signal var hit: SimpleSignal

    @Node("CollisionShape2D") private var collisionShape2D: CollisionShape2D
    @Node("AnimatedSprite2D") private var playerAnimatedSprite: PlayerAnimatedSprite
    @Node("GPUParticles2D") private var gpuParticles2D: GPUParticles2D

    func start(position: Vector2) {
        self.position = position
        self.show()
        // TODO: Property name constant
        self.collisionShape2D.setDeferred(property: "disabled", value: Variant(false))
        self.gpuParticles2D.restart()
    }

    private static func getMovementVector() -> Vector2 {
        var velocity = Vector2.zero
        if Input.isActionPressed(action: StringName(Constants.Inputs.moveRight)) {
            velocity.x += 1
        }
        if Input.isActionPressed(action: StringName(Constants.Inputs.moveLeft)) {
            velocity.x -= 1
        }
        if Input.isActionPressed(action: StringName(Constants.Inputs.moveDown)) {
            velocity.y += 1
        }
        if Input.isActionPressed(action: StringName(Constants.Inputs.moveUp)) {
            velocity.y -= 1
        }
        return velocity
    }

    private func setSpriteVelocity(_ velocity: Vector2) {
        self.playerAnimatedSprite.setVelocity(velocity)
        self.gpuParticles2D.emitting = self.playerAnimatedSprite.isPlaying()
    }

    override func _process(delta: Double) {
        super._process(delta: delta)
        var velocity = Player.getMovementVector()
        if velocity.length() > 0 {
            velocity = velocity.normalized() * self.speed
            self.position += velocity * delta
            self.position = Vector2(
                x: self.position.x.clamped(to: 0...self.screenSize.x),
                y: self.position.y.clamped(to: 0...self.screenSize.y))
        }
        self.setSpriteVelocity(velocity)
    }

    override func _ready() {
        self.screenSize = getViewportRect().size
        self.hide()
        self.bodyEntered.connect { body in
            self.onBodyEntered(body)
        }
    }

    private func onBodyEntered(_ body: Node2D?) {
        self.hide()
        self.hit.emit()
        self.collisionShape2D.setDeferred(property: "disabled", value: Variant(true))
    }
}
