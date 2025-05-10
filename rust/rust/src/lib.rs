mod hud;
mod main_scene;
mod mob;
mod player;

use godot::prelude::*;

struct TutorialRust;

#[gdextension]
unsafe impl ExtensionLibrary for TutorialRust {}

#[macro_export]
macro_rules! get_node {
    ($typ:ty, $name:literal) => {
        ::paste::paste! {
            fn [<get_node_ $name:snake>] (&self) -> Gd<$typ> {
                self.base().get_node_as($name)
            }
        }
    };
}

#[macro_export]
macro_rules! get_node_named {
    ($typ:ty, $node_name:literal, $function_name:literal) => {
        ::paste::paste! {
            fn [<get_node_ $function_name>] (&self) -> Gd<$typ> {
                self.base().get_node_as($node_name)
            }
        }
    };
}

mod input_names {
    pub const MOVE_LEFT: &str = "move_left";
    pub const MOVE_RIGHT: &str = "move_right";
    pub const MOVE_UP: &str = "move_up";
    pub const MOVE_DOWN: &str = "move_down";
}
