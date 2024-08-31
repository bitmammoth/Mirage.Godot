
using Mirage.Godot.Scripts.Attributes;

[NetworkMessage]
public struct CryptoLoginMessage
{
    public string Signature { get; set; }
    public string CryptoAddress { get; set; }
    public string Hash { get; set; }
    public string Timestamp { get; set; }
    public string TokenString { get; set; }
}
