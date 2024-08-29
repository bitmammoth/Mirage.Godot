using Mirage.Godot.Scripts.Attributes;
namespace Mirage.Godot.Scripts.Authentication;

[NetworkMessage]
public struct AuthSuccessMessage
{
    public string AuthenticatorName;
}

