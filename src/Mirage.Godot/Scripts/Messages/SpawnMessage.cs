using System;
using System.Text;
using Godot;

namespace Mirage.Messages
{
    [NetworkMessage]
    public struct SpawnMessage
    {
        /// <summary>
        /// netId of new or existing object
        /// </summary>
        public uint NetId;
        /// <summary>
        /// Is the spawning object the local player. Sets ClientScene.localPlayer
        /// </summary>
        public bool IsLocalPlayer;
        /// <summary>
        /// Sets hasAuthority on the spawned object
        /// </summary>
        public bool IsOwner;

        //public bool IsMainCharacter;

        /// <summary>
        /// The id of the scene object to spawn
        /// </summary>
        public ulong? SceneId;
        /// <summary>
        /// The id of the prefab to spawn
        /// <para>If sceneId != 0 then it is used instead of prefabHash</para>
        /// </summary>
        public int? PrefabHash;

        /// <summary>
        /// Spawn values to set after spawning object, values based on <see cref="NetworkIdentity.TransformSpawnSettings"/>
        /// </summary>
        public SpawnValues SpawnValues;

        /// <summary>
        /// The serialized component data
        /// <remark>ArraySegment to avoid unnecessary allocations</remark>
        /// </summary>    
        public ArraySegment<byte> Payload;

        public override string ToString()
        {
            string spawnIDStr;
            if (SceneId.HasValue)
                spawnIDStr = $"SceneId:{SceneId.Value}";
            else if (PrefabHash.HasValue)
                spawnIDStr = $"PrefabHash:{PrefabHash.Value:X}";
            else
                spawnIDStr = $"SpawnId:Error";

            string authStr;
            if (IsLocalPlayer)
                authStr = "LocalPlayer";
            else if (IsOwner)
                authStr = "Owner";
            else
                authStr = "Remote";

            return $"SpawnMessage[NetId:{NetId},{spawnIDStr},Authority:{authStr},{SpawnValues},Payload:{Payload.Count}bytes]";
        }
    }

    public struct SpawnValues
    {
        public Vector3? Position;
        public Quaternion? Rotation;
        public Vector2? Position2d;
        public float? Rotation2d;
        public string Name;
        public bool? SelfActive;

        [ThreadStatic]
        private static StringBuilder builder;

        public override string ToString()
        {
            if (builder == null)
                builder = new StringBuilder();
            else
                builder.Clear();

            builder.Append("SpawnValues(");
            var first = true;

            if (Position.HasValue)
                Append(ref first, $"Position={Position.Value}");

            if (Rotation.HasValue)
                Append(ref first, $"Rotation={Rotation.Value}");

            if (!string.IsNullOrEmpty(Name))
                Append(ref first, $"Name={Name}");

            if (SelfActive.HasValue)
                Append(ref first, $"SelfActive={SelfActive.Value}");

            builder.Append(")");
            return builder.ToString();
        }

        private static void Append(ref bool first, string value)
        {
            if (!first) builder.Append(", ");
            first = false;
            builder.Append(value);
        }
    }
}
