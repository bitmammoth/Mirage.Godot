using Mirage.Godot.Scripts.Attributes;

namespace Mirage.Godot.Example1.Scripts
{
    [NetworkMessage]
    public struct ChatMessage
    {
        public string Sender;
        public string Message;
    }
}