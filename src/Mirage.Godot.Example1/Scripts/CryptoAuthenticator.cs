using Godot;
using Mirage.Godot.Scripts.Authentication;
using Mirage.Godot.Scripts.Attributes;

namespace Mirage.Godot.Scripts.Components.Authenticators;

public partial class CryptoAuthenticator : NetworkAuthenticator<CryptoAuthenticator.JoinMessage>
{
	[Export] public string CryptoLoginData { get; set; } = "";

	// called on server to validate
	protected override AuthenticationResult Authenticate(NetworkPlayer player, JoinMessage message)
	{
		GD.Print("CryptoAuthenticator: Authenticating player");
		
		return CryptoLoginData == message.CryptoLoginData
			? AuthenticationResult.CreateSuccess(this, null)
			: AuthenticationResult.CreateFail("Server code invalid", this);
	}

	// called on client to create message to send to server
	public void SendCryptoLogin(NetworkClient client, string? cryptoLoginData = null)
	{
		var message = new JoinMessage
		{
			// use the argument or field if null
			CryptoLoginData = cryptoLoginData ?? CryptoLoginData
		};

		SendAuthentication(client, message);
	}

	[NetworkMessage]
	public struct JoinMessage
	{
		public string CryptoLoginData;
	}
}