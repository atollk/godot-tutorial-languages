import SwiftGodot

class Hud: CanvasLayer {
    @Node("Message") var message: Label
    @Node("ScoreLabel") var scoreLabel: Label
    @Node("StartButton") var startButton: Button
    @Node("MessageTimer") var messageTimer: Timer

    @Signal var startGame: SimpleSignal

    func showMessage(text: String, fade: Bool) {
        self.message.text = text
        self.message.show()
        if fade {
            self.messageTimer.start()
        }
    }

    func updateScore(score: Float) {
        self.scoreLabel.text = String(score)
    }

    func showGameOver() async {
        self.showMessage(text: "Game Over", fade: true)
        await self.messageTimer.timeout.emitted
        self.showMessage(text: "Dodge The Creeps!", fade: false)
        await self.getTree()?.createTimer(timeSec: 1.0)?.timeout.emitted
        self.startButton.show()
    }

    override func _ready() {
        self.startButton.pressed.connect {
            self.onStartButtonPressed()
        }
        self.messageTimer.timeout.connect {
            self.onMessageTimerTimeout()
        }
    }

    private func onStartButtonPressed() {
        self.startButton.hide()
        self.startGame.emit()
    }

    private func onMessageTimerTimeout() {
        self.message.hide()
    }
}
