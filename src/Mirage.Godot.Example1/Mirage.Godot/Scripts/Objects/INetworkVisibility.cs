using System.Collections.Generic;

namespace Mirage.Godot.Scripts.Objects;

public interface INetworkVisibility
{
    bool OnCheckObserver(NetworkPlayer player);
    void OnRebuildObservers(HashSet<NetworkPlayer> observers, bool initialize);
}

/// <summary>
/// Default visible when no NetworkVisibility is added to the gameobject
/// </summary>
internal class AlwaysVisible(ServerObjectManager serverObjectManager) : INetworkVisibility
{
    private readonly ServerObjectManager _objectManager = serverObjectManager;
    private readonly NetworkServer _server = serverObjectManager.Server;

    public bool OnCheckObserver(NetworkPlayer player) => true;

    public void OnRebuildObservers(HashSet<NetworkPlayer> observers, bool initialize)
    {
        // add all server connections
        foreach (var player in _server.Players)
        {
            if (!player.SceneIsReady)
                continue;

            // todo replace this with a better visibility system (where default checks auth/scene ready)
            if (_objectManager.OnlySpawnOnAuthenticated && !player.IsAuthenticated)
                continue;

            observers.Add(player);
        }

        // add local host connection (if any)
        if (_server.LocalPlayer != null && _server.LocalPlayer.SceneIsReady)
            observers.Add(_server.LocalPlayer);
    }
}
