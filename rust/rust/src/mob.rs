use crate::get_node;
use godot::classes::{AnimatedSprite2D, IRigidBody2D, RigidBody2D, VisibleOnScreenNotifier2D};
use godot::obj::Base;
use godot::prelude::*;

pub const MOB_GROUP: &str = "mobs";

#[derive(GodotClass)]
#[class(base=RigidBody2D)]
pub struct Mob {
    base: Base<RigidBody2D>,
}

#[godot_api]
impl Mob {
    fn on_screen_exited(&mut self) {
        self.to_gd().queue_free();
    }

    get_node!(AnimatedSprite2D, "AnimatedSprite2D");
    get_node!(VisibleOnScreenNotifier2D, "VisibleOnScreenNotifier2D");
}

#[godot_api]
impl IRigidBody2D for Mob {
    fn init(base: Base<RigidBody2D>) -> Self {
        Self { base }
    }

    fn ready(&mut self) {
        let mut animated_sprite = self.get_node_animated_sprite2_d();
        let mob_types = animated_sprite
            .get_sprite_frames()
            .unwrap()
            .get_animation_names();
        animated_sprite.set_animation(
            mob_types
                .get(rand::random_range(0..mob_types.len()))
                .unwrap()
                .arg(),
        );
        self.get_node_visible_on_screen_notifier2_d()
            .signals()
            .screen_exited()
            .connect_obj(&self.to_gd(), Self::on_screen_exited);
        self.to_gd().add_to_group(MOB_GROUP);
    }
}
