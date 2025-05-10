using System.Collections.Generic;

namespace tutorial;

public static class Extensions
{
    public static T GetNodeOrThrow<T>(this Godot.Node node, string name) where T : Godot.Node
    {
        return node.GetNode<T>(name) ?? throw new KeyNotFoundException($"Could not find node '{name}' of type '{typeof(T).Name}'");
    }
}