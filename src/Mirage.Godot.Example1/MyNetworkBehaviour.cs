using Godot;
using Mirage;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Attributes;
using Mirage.Godot.Scripts.Objects;
using Org.BouncyCastle.Ocsp;

public partial class MyNetworkBehaviour : NetworkBehaviour
{
    [ServerRpc(RequireAuthority = false)]
    public void TestServerRpc(string message, NetworkPlayer sender = null)
    {
        GD.Print($"Received ServerRpc with message: {message} from {sender}");
        // You can now call a ClientRpc from here
        TestClientRpc($"Echo: {message}");
    }

    [ClientRpc]
    public void TestClientRpc(string message)
    {
        GD.Print($"Received ClientRpc with message: {message}");
    }
}