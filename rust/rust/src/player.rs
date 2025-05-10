use crate::{get_node, get_node_named, input_names};
use godot::classes::{AnimatedSprite2D, Area2D, CollisionShape2D, GpuParticles2D, IArea2D, Input};
use godot::global::clampf;
use godot::prelude::*;

#[derive(GodotClass)]
#[class(base=Area2D)]
pub struct Player {
    #[export]
    speed: i16,
    screen_size: Vector2,

    base: Base<Area2D>,
}

#[godot_api]
impl Player {
    #[signal]
    pub fn hit();

    pub fn start(&mut self, position: Vector2) {
        self.base_mut().set_position(position);
        self.base_mut().show();
        self.get_node_collision_shape2_d()
            .set_deferred("disabled", &Variant::from(false));
        self.get_node_gpu_particles_2d().restart();
    }

    fn on_body_entered(&mut self, _body: Gd<Node2D>) {
        self.base_mut().hide();
        self.signals().hit().emit();
        self.get_node_collision_shape2_d()
            .set_deferred("disabled", &Variant::from(true));
    }

    fn get_movement_vector(&self) -> Vector2 {
        let mut velocity = Vector2::ZERO;
        if Input::singleton().is_action_pressed(input_names::MOVE_RIGHT) {
            velocity.x = 1.0;
        }
        if Input::singleton().is_action_pressed(input_names::MOVE_LEFT) {
            velocity.x = -1.0;
        }
        if Input::singleton().is_action_pressed(input_names::MOVE_UP) {
            velocity.y = -1.0;
        }
        if Input::singleton().is_action_pressed(input_names::MOVE_DOWN) {
            velocity.y = 1.0;
        }
        velocity
    }

    fn set_sprite_velocity(&mut self, velocity: Vector2) {
        let mut animated_sprite = self.get_node_animated_sprite2_d();
        if velocity.x != 0.0 {
            animated_sprite.play();
            animated_sprite.set_animation("walk");
            animated_sprite.set_flip_h(velocity.x < 0.0);
            animated_sprite.set_flip_v(false);
        } else if velocity.y != 0.0 {
            animated_sprite.play();
            animated_sprite.set_animation("up");
            animated_sprite.set_flip_h(false);
            animated_sprite.set_flip_v(velocity.y > 0.0);
        } else {
            animated_sprite.stop();
        }
        self.get_node_gpu_particles_2d()
            .set_emitting(animated_sprite.is_playing());
    }

    get_node!(CollisionShape2D, "CollisionShape2D");
    get_node!(AnimatedSprite2D, "AnimatedSprite2D");
    get_node_named!(GpuParticles2D, "GPUParticles2D", "gpu_particles_2d");
}

#[godot_api]
impl IArea2D for Player {
    fn init(base: Base<Area2D>) -> Self {
        Self {
            speed: 400,
            screen_size: Vector2::ZERO,
            base,
        }
    }

    fn process(&mut self, delta: f64) {
        let mut velocity = self.get_movement_vector();
        if velocity.length() > 0.0 {
            velocity = velocity.normalized() * (self.speed as f32);
            let mut new_position = self.base().get_position() + velocity * (delta as f32);
            new_position.x = clampf(new_position.x as f64, 0.0, self.screen_size.x as f64) as real;
            new_position.y = clampf(new_position.y as f64, 0.0, self.screen_size.y as f64) as real;
            self.base_mut().set_position(new_position);
        }
        self.set_sprite_velocity(velocity);
    }

    fn ready(&mut self) {
        self.screen_size = self.base().get_viewport_rect().size;
        self.base_mut().hide();
        let self_gd = &self.to_gd();
        self.base_mut()
            .signals()
            .body_entered()
            .connect_obj(self_gd, Self::on_body_entered);
    }
}
