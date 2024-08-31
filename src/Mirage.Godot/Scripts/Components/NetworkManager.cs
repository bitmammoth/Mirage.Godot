using Godot;
using Mirage.Godot.Scripts.Objects;
using Mirage.Logging;
using Mirage.SocketLayer;

namespace Mirage.Godot.Scripts.Components;

[GlobalClass]
public partial class NetworkManager : Node
{
    private static readonly ILogger logger = LogFactory.GetLogger<NetworkManager>();

    [Export] public NetworkServer? Server { get; set; }
    [Export] public ServerObjectManager? ServerObjectManager { get; set; }
    [Export] public int MaxConnections { get; set; }

    [Export] public NetworkClient? Client { get; set; }
    [Export] public ClientObjectManager? ClientObjectManager { get; set; }

    [Export] public required SocketFactory SocketFactory { get; set; }
    [Export] public bool EnableAllLogs { get; set; }
    [Export] public NetworkScene? NetworkScene { get; set; }

    public override void _Ready()
    {
        base._Ready();
        GeneratedCode.Init();
    }

    public virtual void StartServer()
    {
        logger.Log("Starting Server Mode");
        if ((Server?.PeerConfig) == null)
        {
            Server!.PeerConfig = new Config { MaxConnections = MaxConnections };
        }
        Server?.StartServer();
    }

    public virtual void StartClient()
    {
        logger.Log("Starting Client Mode");
        Client?.Connect();
    }

    public virtual void StartHost()
    {
        logger.Log("Starting Host Mode");
        Server?.StartServer(Client ?? new NetworkClient());
    }

    public void Stop()
    {
        if (Server is { Active: true })
        {
            Server.Stop();
        }
        if (Client is { Active: true })
        {
            Client.Disconnect();
        }
    }

    public override void _Process(double delta)
    {
        if (Server is { Active: true })
        {
            Server.UpdateReceive();
            Server.UpdateSent();
        }
        if (Client is { Active: true })
        {
            Client.UpdateReceive();
            Client.UpdateSent();
        }
    }
}
