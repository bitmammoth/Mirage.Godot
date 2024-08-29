using System;
using Mirage.Godot.Scripts.Attributes;

namespace Mirage.Godot.Scripts.Components.Authenticators.SessionId;

[NetworkMessage]
public struct SessionKeyMessage
{
    public ArraySegment<byte> SessionKey;
}

[NetworkMessage]
public struct RequestSessionMessage
{
}
