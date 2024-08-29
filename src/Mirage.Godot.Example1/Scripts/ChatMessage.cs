using Mirage;

[NetworkMessage]
public struct ChatMessage
{
    public string sender;
    public string message;
}