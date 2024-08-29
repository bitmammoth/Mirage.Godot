using System;
using Mirage.Godot.Scripts.Attributes;

namespace Mirage.Godot.Scripts.Messages
{
    [NetworkMessage]
    public struct RpcMessage
    {
        public uint NetId;
        public int FunctionIndex;
        public ArraySegment<byte> Payload;
    }
}
