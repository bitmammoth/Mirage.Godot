using System;
using Mirage.Godot.Scripts.Objects;
using Mirage.Godot.Scripts.Serialization.Packers;
using Mirage.Logging;

namespace Mirage.Godot.Scripts.Serialization;

public static class MirageTypesExtensions
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MirageTypesExtensions));

    public static void WriteNetworkIdentity(this NetworkWriter writer, NetworkIdentity value)
    {
        if (value == null)
        {
            writer.WritePackedUInt32(0);
            return;
        }
        writer.WritePackedUInt32(value.NetId);
    }
    public static void WriteNetworkBehaviour(this NetworkWriter writer, INetworkNode value)
    {
        if (value == null)
        {
            writer.WriteNetworkIdentity(null);
            return;
        }
        writer.WriteNetworkIdentity(value.Identity);
        writer.WriteByte((byte)value.ComponentIndex);
    }

    /// <summary>
    /// Casts reader to <see cref="MirageNetworkReader"/>, throw if cast is invalid
    /// </summary>
    /// <param name=""></param>
    public static MirageNetworkReader ToMirageReader(this NetworkReader reader)
    {
        return reader is MirageNetworkReader mirageReader
            ? mirageReader
            : throw new InvalidOperationException("Reader is not MirageNetworkReader");
    }

    public static NetworkIdentity ReadNetworkIdentity(this NetworkReader reader)
    {
        var mirageReader = reader.ToMirageReader();

        var netId = reader.ReadPackedUInt32();
        return netId == 0 ? null : FindNetworkIdentity(mirageReader.ObjectLocator, netId);
    }

    private static NetworkIdentity FindNetworkIdentity(IObjectLocator noTypeobjectLocator, uint netId)
    {
        var objectLocator = noTypeobjectLocator;
        if (objectLocator == null)
        {
            if (logger.WarnEnabled()) logger.LogWarning($"Could not find NetworkIdentity because ObjectLocator is null");
            return null;
        }

        // if not found return c# null
        return objectLocator.TryGetIdentity(netId, out var identity)
            ? identity
            : null;
    }

    public static INetworkNode ReadNetworkBehaviour(this NetworkReader reader)
    {
        var mirageReader = reader.ToMirageReader();

        // we can't use ReadNetworkIdentity here, because we need to know if netid was 0 or not
        // if it is not 0 we need to read component index even if NI is null, or it'll fail to deserilize next part
        var netId = reader.ReadPackedUInt32();
        if (netId == 0)
            return null;

        // always read index if netid is not 0
        var componentIndex = reader.ReadByte();

        var identity = FindNetworkIdentity(mirageReader.ObjectLocator, netId);
        return identity is null ? null : identity.NetworkBehaviours[componentIndex];
    }

    public static T ReadNetworkBehaviour<T>(this NetworkReader reader) where T : class, INetworkNode
    {
        return reader.ReadNetworkBehaviour() as T;
    }
}
