using Mirage.Godot.Scripts.Attributes;

namespace Mirage.Godot.Example1.Scripts
{
    [NetworkMessage]
    public struct ChatMessage
    {
        public string sender;
        public string message;
    }
}