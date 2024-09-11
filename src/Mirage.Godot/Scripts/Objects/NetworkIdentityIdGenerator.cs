using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Godot;
#if GODOT_EDITOR
namespace Mirage
{
    internal static class NetworkIdentityIdGenerator
    {
        /// <summary>
        /// Keep track of all sceneIds to detect scene duplicates
        /// <para>We only need to check the ID part here.</para>
        /// </summary>
        internal static readonly Dictionary<int, NetworkIdentity> _sceneIds = new Dictionary<int, NetworkIdentity>();

        /// <summary>
        /// Sets the scene hash on the NetworkIdentity
        /// <para>This will stop duplicate ID if the scene is duplicated</para>
        /// <para>NOTE: Only call this from NetworkScenePostProcess</para>
        /// </summary>
        /// <param name="identity"></param>
        internal static void SetSceneHash(NetworkIdentity identity)
        {
            var wrapper = new IdentityWrapper(identity);

            // get deterministic scene hash
            var pathHash = GetSceneHash(identity);

            wrapper.SceneHash = pathHash;

            // log it for debugging sceneId issues.
            GD.Print($"{identity.Name} in scene path hash({pathHash:X}) scene id: {wrapper.SceneId:X}");
        }

        private static int GetSceneHash(NetworkIdentity identity)
        {
            // In Godot, use the path to the resource or the node path.
            var nodePath = identity.Root.GetPath().ToString();
            return nodePath.GetStableHashCode();
        }

        internal static void SetupIDs(NetworkIdentity identity)
        {
            var wrapper = new IdentityWrapper(identity);
            GD.Print($"Setting up {identity.Name}");
            // Handling scene objects in Godot
            if (identity.IsSceneObject)
            {
                GD.Print($"Setting up scene object {identity.Name}");
                AssignSceneID(identity);
            }
            else
            {
                GD.Print($"Setting up prefab {identity.Name}");
                AssignAssetID(identity);
            }
        }

        private static void AssignAssetID(NetworkIdentity identity)
        {
            // In Godot, you can use the resource path or node path for asset identifiers.
            var path = identity.Root.GetPath().ToString();
            AssignAssetID(identity, path);
        }

        private static void AssignAssetID(NetworkIdentity identity, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // Don't log a warning here, sometimes a node might not have an asset path
                return;
            }

            var wrapper = new IdentityWrapper(identity);
            wrapper.PrefabHash = path.GetStableHashCode();
            GD.Print($"Creating PrefabHash:{wrapper.PrefabHash:X} from '{path}'");
        }

        private static void AssignSceneID(NetworkIdentity identity)
        {
            // Only generate at edit time, skipping if the game is playing
            if (Engine.IsEditorHint())
                return;

            var wrapper = new IdentityWrapper(identity);

            if (wrapper.SceneId == 0 || IsDuplicate(identity, wrapper.SceneId))
            {
                // clear in any case, because it might have been a duplicate
                wrapper.ClearSceneId();

                var randomId = GetRandomUInt();

                // only assign if not a duplicate of an existing scene id
                if (!IsDuplicate(identity, randomId))
                {
                    wrapper.SceneId = randomId;
                }
            }

            // Add to dictionary so we can keep track of ID for duplicates
            _sceneIds[wrapper.SceneId] = identity;
        }

        private static bool IsDuplicate(NetworkIdentity identity, int sceneId)
        {
            if (_sceneIds.TryGetValue(sceneId, out var existing))
            {
                return identity != existing;
            }
            else
            {
                return false;
            }
        }

        private static int GetRandomUInt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                return BitConverter.ToInt32(bytes, 0);
            }
        }

        private sealed class IdentityWrapper
        {
            private const ulong ID_MASK = 0x0000_0000_FFFF_FFFFul;
            private const ulong HASH_MASK = 0xFFFF_FFFF_0000_0000ul;
            private readonly NetworkIdentity _identity;

            public IdentityWrapper(NetworkIdentity identity)
            {
                _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            }

            public int PrefabHash
            {
                get => _identity.PrefabHash;
                set
                {
                    if (PrefabHash == value)
                        return;

                    _identity.PrefabHash = value;
                }
            }

            public int SceneId
            {
                get => (int)(_identity.SceneId & ID_MASK);
                set
                {
                    if (SceneId == value)
                        return;

                    _identity.Editor_SceneId = (_identity.SceneId & HASH_MASK) | ((ulong)value & ID_MASK);
                }
            }

            public int SceneHash
            {
                get => (int)((_identity.SceneId & HASH_MASK) >> 32);
                set
                {
                    if (SceneHash == value)
                        return;

                    _identity.Editor_SceneId = (((ulong)value) << 32) | (_identity.SceneId & ID_MASK);
                }
            }

            public void ClearSceneId()
            {
                SceneId = 0;
                SceneHash = 0;
            }
        }
    }
}
#endif