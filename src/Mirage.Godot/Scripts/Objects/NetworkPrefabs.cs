using System.Collections.Generic;
using Godot;

namespace Mirage
{
    /// <summary>
    ///     A Resource that contains a list of prefabs that can be spawned on the network.
    /// </summary>
    public sealed partial class NetworkPrefabs : Resource
    {
        public List<PackedScene> Prefabs = new List<PackedScene>();
    }
}