using System;

namespace Mirage.Godot.Scripts.Attributes;

/// <summary>
/// Call this from a client to run this function on the server.
/// <para>Make sure to validate input etc. It's not possible to call this from a server.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ServerRpcAttribute : Attribute
{
    public Channel Channel = Channel.Reliable;
    public bool RequireAuthority = true;
}
