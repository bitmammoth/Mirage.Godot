using Godot;
using Mirage.Godot.Scripts.Authentication;
using Mirage.Godot.Scripts.Attributes;

namespace Mirage.Godot.Scripts.Components.Authenticators;

public partial class BasicAuthenticator : NetworkAuthenticator<BasicAuthenticator.JoinMessage>
{
    [Export] public string ServerCode { get; set; } = "";

    // called on server to validate
    protected override AuthenticationResult Authenticate(NetworkPlayer player, JoinMessage message)
    {
        GD.Print("Authenticate");
        return ServerCode == message.ServerCode
            ? AuthenticationResult.CreateSuccess(this, null)
            : AuthenticationResult.CreateFail("Server code invalid", this);
    }

    // called on client to create message to send to server
    public void SendCode(NetworkClient client, string? serverCode = null)
    {
        GD.Print("SendCode");
        var message = new JoinMessage
        {
            // use the argument or field if null
            ServerCode = serverCode ?? ServerCode
        };

        SendAuthentication(client, message);
    }

    [NetworkMessage]
    public struct JoinMessage
    {
        public string ServerCode;
    }
}
