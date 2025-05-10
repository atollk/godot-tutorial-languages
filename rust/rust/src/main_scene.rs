use crate::hud::HUD;
use crate::mob::{MOB_GROUP, Mob};
use crate::player::Player;
use crate::{get_node, get_node_named};
use godot::classes::{AudioStreamPlayer2D, Marker2D, PathFollow2D, Timer};
use godot::obj::Base;
use godot::prelude::*;
use std::f32::consts::PI;

#[derive(GodotClass)]
#[class(base=Node)]
struct Main {
    #[export]
    mob_scene: Option<Gd<PackedScene>>,
    score: i32,
    mob_speed: i32,
    base: Base<Node>,
}

#[godot_api]
impl Main {
    fn new_game(&mut self) {
        self.score = 0;
        self.mob_speed = 100;
        self.get_node_player()
            .bind_mut()
            .start(self.get_node_start_position().get_position());
        self.get_node_start_timer().start();
        self.get_node_hud().bind().update_score(self.score);
        self.get_node_hud().bind().show_message("Get Ready", true);
        self.get_node_music().play();
        self.to_gd()
            .get_tree()
            .unwrap()
            .call_group(MOB_GROUP, "queue_free", &[]);
    }

    fn game_over(&mut self) {
        self.mob_speed = 100;
        self.get_node_score_timer().stop();
        self.get_node_mob_spawn_timer().set_wait_time(3.0);
        self.get_node_mob_spawn_timer().start();
        self.get_node_mob_speed_timer().stop();
        self.get_node_music().stop();
        self.get_node_death_sound().play();
        self.get_node_hud().bind().show_game_over();
    }

    fn on_player_hit(&mut self) {
        self.game_over();
    }

    fn on_mob_spawn_timer_timeout(&mut self) {
        let mob_scene = self.mob_scene.as_ref().unwrap();
        let mut mob = mob_scene.instantiate().unwrap().cast::<Mob>();
        let mut mob_spawn_location = self.get_node_mob_spawn_location();
        mob_spawn_location.set_progress_ratio(rand::random_range(0.0..1.0));
        mob.set_position(mob_spawn_location.get_position());

        let direction = mob_spawn_location.get_rotation()
            + (PI / 2.0)
            + rand::random_range((-PI / 4.0)..=(PI / 4.0));
        mob.set_rotation(direction);

        let velocity = Vector2 {
            x: rand::random_range(0.75..=1.5) * (self.mob_speed as f32),
            y: 0.0,
        };
        mob.set_linear_velocity(velocity.rotated(direction));

        self.to_gd().add_child(&mob);
    }

    fn on_mob_speed_timer_timeout(&mut self) {
        self.mob_speed += 10;
        self.get_node_mob_spawn_timer()
            .set_wait_time(100.0 / (self.mob_speed as f64));
    }

    fn on_score_timer_timeout(&mut self) {
        self.score += 1;
        self.get_node_hud().bind().update_score(self.score);
    }

    fn on_start_timer_timeout(&mut self) {
        self.get_node_mob_speed_timer().start();
        self.get_node_mob_spawn_timer().set_wait_time(1.0);
        self.get_node_mob_spawn_timer().start();
        self.get_node_score_timer().start();
    }

    fn on_hud_start_game(&mut self) {
        self.new_game();
    }

    get_node!(Player, "Player");
    get_node!(Timer, "StartTimer");
    get_node!(Timer, "MobSpawnTimer");
    get_node!(Timer, "MobSpeedTimer");
    get_node!(Timer, "ScoreTimer");
    get_node_named!(HUD, "HUD", "hud");
    get_node!(Marker2D, "StartPosition");
    get_node_named!(
        PathFollow2D,
        "MobPath/MobSpawnLocation",
        "mob_spawn_location"
    );
    get_node!(AudioStreamPlayer2D, "Music");
    get_node!(AudioStreamPlayer2D, "DeathSound");
}

#[godot_api]
impl INode for Main {
    fn init(base: Base<Node>) -> Self {
        Self {
            mob_scene: None,
            score: 0,
            mob_speed: 100,
            base,
        }
    }

    fn ready(&mut self) {
        self.get_node_mob_spawn_timer()
            .signals()
            .timeout()
            .connect_obj(&self.to_gd(), Self::on_mob_spawn_timer_timeout);
        self.get_node_mob_speed_timer()
            .signals()
            .timeout()
            .connect_obj(&self.to_gd(), Self::on_mob_speed_timer_timeout);
        self.get_node_score_timer()
            .signals()
            .timeout()
            .connect_obj(&self.to_gd(), Self::on_score_timer_timeout);
        self.get_node_start_timer()
            .signals()
            .timeout()
            .connect_obj(&self.to_gd(), Self::on_start_timer_timeout);
        self.get_node_player()
            .signals()
            .hit()
            .connect_obj(&self.to_gd(), Self::on_player_hit);
        self.get_node_hud()
            .signals()
            .start_game()
            .connect_obj(&self.to_gd(), Self::on_hud_start_game);
        self.get_node_mob_spawn_timer().set_wait_time(3.0);
        self.get_node_mob_spawn_timer().start();
    }
}
