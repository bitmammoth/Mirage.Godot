using Mirage.Godot.Scripts.Objects;
using Mirage.Godot.Scripts.Serialization;
using Mirage.Godot.Scripts.Serialization.Packers;

namespace Mirage.Godot.Scripts.Syncing;


/// <summary>
/// backing struct for a NetworkIdentity when used as a syncvar
/// the weaver will replace the syncvar with this struct.
/// </summary>
public struct NetworkIdentitySyncvar
{
    /// <summary>
    /// The network client that spawned the parent object
    /// used to lookup the identity if it exists
    /// </summary>
    internal IObjectLocator _objectLocator;
    internal uint _netId;

    internal NetworkIdentity _identity;

    internal readonly uint NetId => _identity != null ? _identity.NetId : _netId;

    public NetworkIdentity Value
    {
        get
        {
            if (_identity != null)
                return _identity;

            return _objectLocator is IObjectLocator locator && locator.TryGetIdentity(NetId, out var result) ? result : null;
        }

        set
        {
            if (value == null)
                _netId = 0;
            _identity = value;
        }
    }
}


public static class NetworkIdentitySerializers
{
    public static void WriteNetworkIdentitySyncVar(this NetworkWriter writer, NetworkIdentitySyncvar id)
    {
        writer.WritePackedUInt32(id.NetId);
    }

    public static NetworkIdentitySyncvar ReadNetworkIdentitySyncVar(this NetworkReader reader)
    {
        var mirageReader = reader.ToMirageReader();

        var netId = reader.ReadPackedUInt32();

        NetworkIdentity identity = null;

        if (mirageReader.ObjectLocator is IObjectLocator locator)
            locator.TryGetIdentity(netId, out identity);

        return new NetworkIdentitySyncvar
        {
            _objectLocator = mirageReader.ObjectLocator,
            _netId = netId,
            _identity = identity
        };
    }
}
