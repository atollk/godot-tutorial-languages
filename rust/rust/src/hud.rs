use crate::get_node;
use godot::classes::{BaseButton, Button, CanvasLayer, ICanvasLayer, Label, Timer};
use godot::obj::{Base, WithBaseField};
use godot::prelude::*;

#[derive(GodotClass)]
#[class(base=CanvasLayer)]
pub struct HUD {
    base: Base<CanvasLayer>,
}

#[godot_api]
impl HUD {
    #[signal]
    pub fn start_game();

    pub fn show_message(&self, text: &str, fade: bool) {
        let mut message = self.get_node_message();
        message.set_text(text);
        message.show();
        if fade {
            self.get_node_message_timer().start();
        }
    }

    pub fn update_score(&self, score: i32) {
        self.get_node_score_label().set_text(&score.to_string());
    }

    pub fn show_game_over(&self) {
        let self_gd = self.to_gd();
        godot::task::spawn(async move {
            let hud = self_gd.bind();
            hud.show_message("Game Over", true);
            hud.get_node_message_timer()
                .signals()
                .timeout()
                .to_future()
                .await;
            hud.show_message("Dodge The Creeps!", false);
            self_gd
                .get_tree()
                .unwrap()
                .create_timer(1.0)
                .unwrap()
                .signals()
                .timeout()
                .to_future()
                .await;
            hud.get_node_start_button().show();
        });
    }

    fn on_start_button_pressed(&mut self) {
        self.get_node_start_button().hide();
        self.signals().start_game().emit();
    }

    fn on_message_timer_timeout(&mut self) {
        self.get_node_message().hide();
    }

    get_node!(Label, "Message");
    get_node!(Label, "ScoreLabel");
    get_node!(Timer, "MessageTimer");
    get_node!(Button, "StartButton");
}

#[godot_api]
impl ICanvasLayer for HUD {
    fn init(base: Base<CanvasLayer>) -> Self {
        Self { base }
    }

    fn ready(&mut self) {
        self.get_node_start_button()
            .upcast::<BaseButton>()
            .signals()
            .pressed()
            .connect_obj(&self.to_gd(), Self::on_start_button_pressed);
        self.get_node_message_timer()
            .signals()
            .timeout()
            .connect_obj(&self.to_gd(), Self::on_message_timer_timeout);
    }
}
