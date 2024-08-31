using Godot;
using Mirage.Godot.Example1.NetworkPositionSync;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Components;
using Mirage.Godot.Scripts.Objects;
using Mirage.Godot.Scripts.Utils;
using Mirage.Logging;

namespace Example2d
{
    [GlobalClass]
    public partial class Manager2d : Node
    {
        [Export] public NetworkManager NetworkManager;
        [Export] public ServerObjectManager ServerObjectManager;
        [Export] public NetworkServer Server;
        [Export] public ClientObjectManager ClientObjectManager;
        [Export] public PackedScene playerPrefab;
        [Export] public int cubeCount = 10;
        [Export] public PackedScene cubePrefab;

        private NetworkIdentity Spawn(PackedScene prefab)
        {
            var clone = prefab.Instantiate();
            GetTree().Root.CallDeferred("add_child", clone);

            var identity = clone.GetNetworkIdentity();
            identity.PrefabHash = PrefabHashHelper.GetPrefabHash(prefab);
            return identity;
        }

        public override void _Ready()
        {
            ClientObjectManager.RegisterPrefab(playerPrefab);
            ClientObjectManager.RegisterPrefab(cubePrefab);

            Server.Started.AddListener(ServerStarted);
            Server.Authenticated += ServerAuthenticated;
            LogFactory.GetLogger<SyncPositionBehaviourCollection>().filterLogType = LogType.Log;
        }

        private void ServerStarted()
        {
            for (var i = 0; i < cubeCount; i++)
            {
                var clone = Spawn(cubePrefab);
                ServerObjectManager.Spawn(clone);
            }
        }

        private void ServerAuthenticated(NetworkPlayer player)
        {
            var clone = Spawn(playerPrefab);
            ServerObjectManager.AddCharacter(player, clone);
        }
    }
}

