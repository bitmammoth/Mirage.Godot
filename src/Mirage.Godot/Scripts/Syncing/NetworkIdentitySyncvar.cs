using System;
using Mirage.Serialization;

namespace Mirage
{

    /// <summary>
    /// backing struct for a NetworkIdentity when used as a syncvar
    /// the weaver will replace the syncvar with this struct.
    /// </summary>
    public struct NetworkIdentitySyncVar : IEquatable<NetworkIdentitySyncVar>
    {
        /// <summary>
        /// The network client that spawned the parent object
        /// used to lookup the identity if it exists
        /// </summary>
        internal IObjectLocator _objectLocator;
        internal uint _netId;

        internal NetworkIdentity _identity;

        internal uint NetId => _identity != null ? _identity.NetId : _netId;

        public NetworkIdentity Value
        {
            get
            {
                if (_identity != null)
                    return _identity;

                if (_objectLocator != null && _objectLocator.TryGetIdentity(NetId, out var result))
                {
                    return result;
                }

                return null;
            }

            set
            {
                if (value == null)
                    _netId = 0;
                _identity = value;
            }
        }

        public bool Equals(NetworkIdentitySyncVar other)
        {
            // NetId is the current ID or the saved ID
            // so we can just compare that to see if values are equal
            return NetId == other.NetId;
        }
    }


    public static class NetworkIdentitySerializers
    {
        public static void WriteNetworkIdentitySyncVar(this NetworkWriter writer, NetworkIdentitySyncVar id)
        {
            writer.WritePackedUInt32(id.NetId);
        }

        public static NetworkIdentitySyncVar ReadNetworkIdentitySyncVar(this NetworkReader reader)
        {
            var mirageReader = reader.ToMirageReader();

            var netId = reader.ReadPackedUInt32();

            NetworkIdentity identity = null;
            mirageReader.ObjectLocator?.TryGetIdentity(netId, out identity);

            return new NetworkIdentitySyncVar
            {
                _objectLocator = mirageReader.ObjectLocator,
                _netId = netId,
                _identity = identity
            };
        }
    }
}