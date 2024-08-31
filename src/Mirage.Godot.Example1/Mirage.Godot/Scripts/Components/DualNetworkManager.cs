using Godot;
using Mirage.Godot.Scripts.Objects;
using Mirage.SocketLayer;

namespace Mirage.Godot.Scripts.Components;

public partial class DualNetworkManager : NetworkManager
{
    [Export] public NetworkServer? Server1 { get; set; }
    [Export] public NetworkServer? Server2 { get; set; }
    [Export] public ServerObjectManager? ObjectManager1 { get; set; }
    [Export] public ServerObjectManager? ObjectManager2 { get; set; }
    [Export] public NetworkClient? Client1 { get; set; }
    [Export] public NetworkClient? Client2 { get; set; }
    [Export] public ClientObjectManager? ClientObjectManager1 { get; set; }
    [Export] public ClientObjectManager? ClientObjectManager2 { get; set; }
    [Export] public SocketFactory? SocketFactory1 { get; set; }
    [Export] public SocketFactory? SocketFactory2 { get; set; }
    [Export] public bool IsServer { get; set; }
    public override void StartServer()
    {
        if ((Server1?.PeerConfig) == null)
        {
            Server1!.PeerConfig = new Config { MaxConnections = MaxConnections };
        }
        if ((Server2?.PeerConfig) == null)
        {
            Server2!.PeerConfig = new Config { MaxConnections = MaxConnections };
        }
        Server1?.StartServer();
        Server2?.StartServer();
    }
    public override void StartClient()
    {
        Client1?.Connect();
        Client2?.Connect();
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (IsServer)
        {

            if (Server1 is { Active: true })
            {
                Server1.UpdateReceive();
                Server1.UpdateSent();
            }
            if (Server2 is { Active: true })
            {
                Server2.UpdateReceive();
                Server2.UpdateSent();
            }
        }
        else
        {
            if (Client1 is { Active: true })
            {
                Client1.UpdateReceive();
                Client1.UpdateSent();
            }
            if (Client2 is { Active: true })
            {
                Client2.UpdateReceive();
                Client2.UpdateSent();
            }
        }
    }
}
