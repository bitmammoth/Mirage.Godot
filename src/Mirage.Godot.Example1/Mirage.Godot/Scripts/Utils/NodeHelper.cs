using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Mirage.Godot.Scripts.Objects;

namespace Mirage.Godot.Scripts.Utils;

internal static class NodeHelper
{
    /// <summary>
    /// Gets NetworkIdentity in first level of child
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public static NetworkIdentity GetNetworkIdentity(this Node node)
    {
        return node.GetChildren().OfType<NetworkIdentity>().First();
    }
    public static T GetSibling<T>(this Node node)
    {
        var parent = node.GetParent();
        var children = parent.GetChildren();
        var ofType = children.OfType<T>();
        var first = ofType.FirstOrDefault();
        return first;
    }
    public static T GetFirstChild<T>(this Node node)
    {
        var children = node.GetChildren();
        var ofType = children.OfType<T>();
        var first = ofType.FirstOrDefault();
        return first;
    }
    public static bool TryGetSibling<T>(this Node node, out T result)
    {
        result = node.GetSibling<T>();
        return result != null;
    }
    public static bool TryGetFirstChild<T>(this Node node, out T result)
    {
        result = node.GetFirstChild<T>();
        return result != null;
    }

    public static T[] GetComponentsInChildren<T>(this Node node)
    {
        return node.GetComponentsInChildrenEnumerable<T>().ToArray();
    }
    public static IEnumerable<T> GetComponentsInChildrenEnumerable<T>(this Node node)
    {
        // todo can we use find_children instead?
        if (node is T comp)
            yield return comp;

        foreach (var child in node.GetChildren())
        {
            foreach (var obj in child.GetComponentsInChildrenEnumerable<T>())
                yield return obj;
        }
    }

    public static T GetComponentInParent<T>(this Node node)
    {
        var parent = node.GetParent();
        return parent.TryGetFirstChild<T>(out var result) ? result : parent.GetComponentInParent<T>();
    }

    public static NetworkIdentity TryGetNetworkIdentity<T>(this T node) where T : Node, INetworkNode
    {
        var _identity = node.GetComponentInParent<NetworkIdentity>();

        // do this 2nd check inside first if so that we are not checking == twice on unity Object
        return _identity is null ? throw new InvalidOperationException($"Could not find NetworkIdentity for {node.Name}.") : _identity;
    }

    internal static IEnumerable<NetworkIdentity> GetAllNetworkIdentities(this Node node)
    {
        return node.GetTree().GetNodesInGroup(nameof(NetworkIdentity)).OfType<NetworkIdentity>();
    }
}

