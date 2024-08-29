using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Mirage.Godot.Scripts.Authentication;
using Mirage.Godot.Scripts.Events;
using Mirage.Godot.Scripts.Objects;
using Mirage.Godot.Scripts.Serialization;
using Mirage.Godot.Scripts.Syncing;
using Mirage.Godot.Scripts.Utils;
using Mirage.Logging;
using Mirage.SocketLayer;

namespace Mirage.Godot.Scripts;

/// <summary>
/// The NetworkServer.
/// </summary>
/// <remarks>
/// <para>NetworkServer handles remote connections from remote clients, and also has a local connection for a local client.</para>
/// </remarks>
[GlobalClass]
public partial class NetworkServer : Node
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkServer));

    [Export]
    public bool EnablePeerMetrics;
    [Export(hintString: "Sequence size of buffer in bits. 10 => array size 1024 => ~17 seconds at 60hz")]
    public int MetricsSize = 10;
    [Export]
    public bool DisconnectOnException = true;
    [Export(hintString: "Should the message handler rethrow the exception after logging. This should only be used when deubgging as it may stop other Mirage functions from running after messages handling")]
    public bool RethrowException = false;
    [Export(hintString: "If disabled the server will not create a Network Peer to listen. This can be used to run server single player mode")]
    public bool Listening = true;
    [Export(hintString: "Creates Socket for Peer to use")]
    public SocketFactory SocketFactory;

    public Metrics Metrics { get; private set; }

    /// <summary>
    /// Config for peer, if not set will use default settings
    /// </summary>
    public Config PeerConfig { get; set; }

    [Export]
    public ServerObjectManager ObjectManager;
    [Export(hintString: "Authentication component attached to this object")]
    public AuthenticatorSettings Authenticator;

    private Peer _peer;


    private AddLateEvent _started = new AddLateEvent();
    /// <summary>
    /// This is invoked when a server is started - including when a host is started.
    /// </summary>
    public IAddLateEvent Started => _started;

    /// <summary>
    /// Is invoked after a connection fully connects to the server
    /// <para>For most use cases use <see cref="Authenticated"/> instead.</para>
    /// </summary>
    public event Action<NetworkPlayer> Connected;

    /// <summary>
    /// Event fires once a new Client has passed Authentication to the Server.
    /// </summary>
    public event Action<NetworkPlayer> Authenticated;

    /// <summary>
    /// Event fires once a Client has Disconnected from the Server.
    /// </summary>
    public event Action<NetworkPlayer> Disconnected;

    private AddLateEvent _stopped = new AddLateEvent();
    public IAddLateEvent Stopped => _stopped;

    /// <summary>
    /// This is invoked when a host is started.
    /// <para>StartHost has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    private AddLateEvent _onStartHost = new AddLateEvent();
    public IAddLateEvent OnStartHost => _onStartHost;

    /// <summary>
    /// This is called when a host is stopped.
    /// </summary>
    private AddLateEvent _onStopHost = new AddLateEvent();
    public IAddLateEvent OnStopHost => _onStopHost;

    /// <summary>
    /// The connection to the host mode client (if any).
    /// </summary>
    // original HLAPI has .localConnections list with only m_LocalConnection in it
    // (for backwards compatibility because they removed the real localConnections list a while ago)
    // => removed it for easier code. use .localConnection now!
    public NetworkPlayer LocalPlayer { get; private set; }

    /// <summary>
    /// The host client for this server 
    /// </summary>
    public NetworkClient LocalClient { get; private set; }

    /// <summary>
    /// True if there is a local client connected to this server (host mode)
    /// </summary>
    public bool LocalClientActive => LocalClient != null && LocalClient.Active;

    /// <summary>
    /// A list of local connections on the server.
    /// </summary>
    public IReadOnlyCollection<NetworkPlayer> Players => _connections.Values;

    private readonly Dictionary<IConnection, NetworkPlayer> _connections = [];

    /// <summary>
    /// <para>Checks if the server has been started.</para>
    /// <para>This will be true after NetworkServer.Listen() has been called.</para>
    /// </summary>
    public bool Active { get; private set; }

    public NetworkWorld World { get; private set; }
    // todo move syncVarsender, it doesn't need to be a public fields on network server any more
    public SyncVarSender SyncVarSender { get; private set; }

    private SyncVarReceiver _syncVarReceiver;
    public MessageHandler MessageHandler { get; private set; }

    /// <summary>
    /// Set to true if you want to manually call <see cref="UpdateReceive"/> and <see cref="UpdateSent"/> and stop mirage from automatically calling them
    /// </summary>
    public bool ManualUpdate = false;

    public override void _ExitTree()
    {
        // if gameobject with server on is destroyed, stop the server
        if (Active)
            Stop();
    }

    /// <summary>
    /// This shuts down the server and disconnects all clients.
    /// <para>If In host mode, this will also stop the local client</para>
    /// </summary>
    public void Stop()
    {
        if (!Active)
        {
            logger.LogWarning("Can't stop server because it is not active");
            return;
        }

        if (LocalClient != null)
        {
            _onStopHost?.Invoke();
            LocalClient.Disconnect();
        }

        // just clear list, connections will be disconnected when peer is closed
        _connections.Clear();
        LocalClient = null;
        LocalPlayer = null;

        _stopped?.Invoke();
        Active = false;

        _started.Reset();
        _onStartHost.Reset();
        _onStopHost.Reset();
        _stopped.Reset();

        World = null;
        SyncVarSender = null;

        if (_peer != null)
        {
            //remove handlers first to stop loop
            _peer.OnConnected -= Peer_OnConnected;
            _peer.OnDisconnected -= Peer_OnDisconnected;
            _peer.Close();
            _peer = null;
        }
    }

    /// <summary>
    /// Start the server
    /// <para>If <paramref name="localClient"/> is given then will start in host mode</para>
    /// </summary>
    /// <param name="config">Config for <see cref="Peer"/></param>
    /// <param name="localClient">if not null then start the server and client in hostmode</param>
    // Has to be called "StartServer" to stop unity complaining about "Start" method
    public void StartServer(NetworkClient localClient = null)
    {
        ThrowIfActive();
        ThrowIfSocketIsMissing();

        if (logger.LogEnabled()) logger.Log($"NetworkServer created, Mirage version: {Version.Current}");

        logger.Assert(Players.Count == 0, "Player should have been reset since previous session");
        logger.Assert(_connections.Count == 0, "Connections should have been reset since previous session");

        World = new NetworkWorld();
        SyncVarSender = new SyncVarSender();

        LocalClient = localClient;
        MessageHandler = new MessageHandler(World, DisconnectOnException, RethrowException);
        MessageHandler.RegisterHandler<NetworkPingMessage>(World.Time.OnServerPing, allowUnauthenticated: true);

        // create after MessageHandler, SyncVarReceiver uses it 
        _syncVarReceiver = new SyncVarReceiver(World, MessageHandler);

        var dataHandler = new DataHandler(MessageHandler, _connections);
        Metrics = EnablePeerMetrics ? new Metrics(MetricsSize) : null;

        var config = PeerConfig ?? new Config();
        var maxPacketSize = SocketFactory.MaxPacketSize;
        NetworkWriterPool.Configure(maxPacketSize);

        // Are we listening for incoming connections?
        // If yes, set up a socket for incoming connections (we're a multiplayer game).
        // If not, that's okay. Some games use a non-listening server for their single player game mode (Battlefield, Call of Duty...)
        if (Listening)
        {
            // Create a server specific socket.
            var socket = SocketFactory.CreateServerSocket();

            // Tell the peer to use that newly created socket.
            _peer = new Peer(socket, maxPacketSize, dataHandler, config, LogFactory.GetLogger<Peer>(), Metrics);
            _peer.OnConnected += Peer_OnConnected;
            _peer.OnDisconnected += Peer_OnDisconnected;
            // Bind it to the endpoint.
            _peer.Bind(SocketFactory.GetBindEndPoint());

            if (logger.LogEnabled()) logger.Log($"Server started, listening for connections. Using socket {socket.GetType()}");
        }
        else
        {
            // Nicely mention that we're going live, but not listening for connections.
            if (logger.LogEnabled()) logger.Log("Server started, but not listening for connections: Attempts to connect to this instance will fail!");
        }

        Authenticator?.Setup(this);

        Active = true;
        // make sure to call ServerObjectManager start before started event
        // this is too stop any race conditions where other scripts add their started event before SOM is setup
        ObjectManager?.ServerStarted(this);
        _started?.Invoke();

        if (LocalClient != null)
        {
            // we should call onStartHost after transport is ready to be used
            // this allows server methods like ServerObjectManager.Spawn to be called in there
            _onStartHost?.Invoke();

            localClient.ConnectHost(this, dataHandler);
            Connected?.Invoke(LocalPlayer);

            if (logger.LogEnabled()) logger.Log("NetworkServer StartHost");
            Authenticate(LocalPlayer);
        }
    }

    private void ThrowIfActive()
    {
        if (Active) throw new InvalidOperationException("Server is already active");
    }

    private void ThrowIfSocketIsMissing()
    {
        SocketFactory ??= this.GetSibling<SocketFactory>();
        if (SocketFactory == null)
            throw new InvalidOperationException($"{nameof(SocketFactory)} could not be found for {nameof(NetworkServer)}");
    }

    public override void _Process(double delta)
    {
        if (!Active)
            return;

        World.Time.UpdateFrameTime();

        if (ManualUpdate)
            return;

        UpdateReceive();
        UpdateSent();
    }

    public void UpdateReceive() => _peer?.UpdateReceive();
    public void UpdateSent()
    {
        SyncVarSender?.Update();
        _peer?.UpdateSent();
    }

    private void Peer_OnConnected(IConnection conn)
    {
        var player = new NetworkPlayer(conn, false);
        if (logger.LogEnabled()) logger.Log($"Server new player {player}");

        // add connection
        _connections[player.Connection] = player;

        // let everyone know we just accepted a connection
        Connected?.Invoke(player);

        Authenticate(player);
    }

    private void Authenticate(NetworkPlayer player)
    {
        // authenticate player
        if (Authenticator != null)
            AuthenticateAsync(player).Forget();
        else
            AuthenticationSuccess(player, AuthenticationResult.CreateSuccess("No Authenticators"));
    }

    private async Task AuthenticateAsync(NetworkPlayer player)
    {
        var result = await Authenticator.ServerAuthenticate(player);

        // process results
        if (result.Success)
        {
            AuthenticationSuccess(player, result);
        }
        else
        {
            // todo use reason
            player.Disconnect();
        }
    }

    private void AuthenticationSuccess(NetworkPlayer player, AuthenticationResult result)
    {
        player.SetAuthentication(new PlayerAuthentication(result.Authenticator, result.Data));

        // send message to let client know
        //     we want to send this even if host, or no Authenticators
        //     this makes host logic a lot easier,
        //     because we need to call SetAuthentication on both server/client before Authenticated
        player.Send(new AuthSuccessMessage { AuthenticatorName = result.Authenticator?.AuthenticatorName });

        // add connection
        Authenticated?.Invoke(player);
    }

    private void Peer_OnDisconnected(IConnection conn, DisconnectReason reason)
    {
        if (logger.LogEnabled()) logger.Log($"Client {conn} disconnected with reason: {reason}");

        if (_connections.TryGetValue(conn, out var player))
        {
            OnDisconnected(player);
        }
        else
        {
            // todo remove or replace with assert
            if (logger.WarnEnabled()) logger.LogWarning($"No handler found for disconnected client {conn}");
        }
    }

    /// <summary>
    /// This removes an external connection.
    /// </summary>
    /// <param name="connectionId">The id of the connection to remove.</param>
    private void RemoveConnection(NetworkPlayer player)
    {
        _connections.Remove(player.Connection);
    }

    /// <summary>
    /// Create Player on Server for hostmode and adds it to collections
    /// <para>Does not invoke <see cref="Connected"/> event, use <see cref="InvokeLocalConnected"/> instead at the correct time</para>
    /// </summary>
    internal void AddLocalConnection(NetworkClient client, IConnection connection)
    {
        if (LocalPlayer != null)
        {
            throw new InvalidOperationException("Local client connection already exists");
        }

        var player = new NetworkPlayer(connection, true);
        LocalPlayer = player;
        LocalClient = client;

        if (logger.LogEnabled()) logger.Log($"Server accepted local client connection: {player}");

        _connections[player.Connection] = player;

        // we need to add host player to auth early, so that Client.Connected, can be used to send auth message
        // if we want for server to add it then we will be too late 
        Authenticator?.PreAddHostPlayer(player);
    }

    public void SendToAll<T>(T msg, bool excludeLocalPlayer, Channel channelId = Channel.Reliable)
    {
        var enumerator = _connections.Values.GetEnumerator();
        SendToMany(enumerator, msg, excludeLocalPlayer, channelId);
    }

    public void SendToMany<T>(IReadOnlyList<NetworkPlayer> players, T msg, bool excludeLocalPlayer, Channel channelId = Channel.Reliable)
    {
        if (excludeLocalPlayer)
        {
            using (var list = AutoPool<List<NetworkPlayer>>.Take())
            {
                ListHelper.AddToList(list, players, LocalPlayer);
                NetworkServer.SendToMany(list, msg, channelId);
            }
        }
        else
        {
            // we are not removing any objects from the list, so we can skip the AddToList
            NetworkServer.SendToMany(players, msg, channelId);
        }
    }
    /// <summary>
    /// Warning: this will allocate, Use <see cref="SendToMany{T}(IReadOnlyList{NetworkPlayer}, T, bool, Channel)"/> or <see cref="SendToMany{T, TEnumerator}(TEnumerator, T, bool, Channel)"/> instead
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="players"></param>
    /// <param name="msg"></param>
    /// <param name="excludeLocalPlayer"></param>
    /// <param name="channelId"></param>
    public void SendToMany<T>(IEnumerable<NetworkPlayer> players, T msg, bool excludeLocalPlayer, Channel channelId = Channel.Reliable)
    {
        using (var list = AutoPool<List<NetworkPlayer>>.Take())
        {
            ListHelper.AddToList(list, players, excludeLocalPlayer ? LocalPlayer : null);
            NetworkServer.SendToMany(list, msg, channelId);
        }
    }
    /// <summary>
    /// use to avoid allocation of IEnumerator
    /// </summary>
    public void SendToMany<T, TEnumerator>(TEnumerator playerEnumerator, T msg, bool excludeLocalPlayer, Channel channelId = Channel.Reliable)
        where TEnumerator : struct, IEnumerator<NetworkPlayer>
    {
        using (var list = AutoPool<List<NetworkPlayer>>.Take())
        {
            ListHelper.AddToList(list, playerEnumerator, excludeLocalPlayer ? LocalPlayer : null);
            NetworkServer.SendToMany(list, msg, channelId);
        }
    }

    public void SendToObservers<T>(NetworkIdentity identity, T msg, bool excludeLocalPlayer, bool excludeOwner, Channel channelId = Channel.Reliable)
    {
        var observers = identity.observers;
        if (observers.Count == 0)
            return;

        using (var list = AutoPool<List<NetworkPlayer>>.Take())
        {
            var enumerator = observers.GetEnumerator();
            ListHelper.AddToList(list, enumerator, excludeLocalPlayer ? LocalPlayer : null, excludeOwner ? identity.Owner : null);
            NetworkServer.SendToMany(list, msg, channelId);
        }
    }

    /// <summary>
    /// Sends to list of players.
    /// <para>All other SendTo... functions call this, it dooes not do any extra checks, just serializes message if not empty, then sends it</para>
    /// </summary>
    // need explicity List function here, so that implicit casts to List from wrapper works
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendToMany<T>(List<NetworkPlayer> players, T msg, Channel channelId = Channel.Reliable)
        => SendToMany((IReadOnlyList<NetworkPlayer>)players, msg, channelId);

    /// <summary>
    /// Sends to list of players.
    /// <para>All other SendTo... functions call this, it dooes not do any extra checks, just serializes message if not empty, then sends it</para>
    /// </summary>
    public static void SendToMany<T>(IReadOnlyList<NetworkPlayer> players, T msg, Channel channelId = Channel.Reliable)
    {
        // avoid serializing when list is empty
        if (players.Count == 0)
            return;

        using (var writer = NetworkWriterPool.GetWriter())
        {
            if (logger.LogEnabled()) logger.Log($"Sending {typeof(T)} to {players.Count} players, channel:{channelId}");

            // pack message into byte[] once
            MessagePacker.Pack(msg, writer);
            var segment = writer.ToArraySegment();
            var count = players.Count;

            for (var i = 0; i < count; i++)
            {
                players[i].Send(segment, channelId);
            }

            NetworkDiagnostics.OnSend(msg, segment.Count, count);
        }
    }

    //called once a client disconnects from the server
    private void OnDisconnected(NetworkPlayer player)
    {
        if (logger.LogEnabled()) logger.Log("Server disconnect client:" + player);

        // set the flag first so we dont try to send any messages to the disconnected
        // connection as they wouldn't get them
        player.MarkAsDisconnected();

        RemoveConnection(player);

        Disconnected?.Invoke(player);

        player.DestroyOwnedObjects();
        player.Identity = null;

        if (player == LocalPlayer)
            LocalPlayer = null;
    }

    /// <summary>
    /// This class will later be removed when we have a better implementation for IDataHandler
    /// </summary>
    private sealed class DataHandler(IMessageReceiver messageHandler, Dictionary<IConnection, NetworkPlayer> connections) : IDataHandler
    {
        private readonly IMessageReceiver _messageHandler = messageHandler;
        private readonly Dictionary<IConnection, NetworkPlayer> _players = connections;

        public void ReceiveMessage(IConnection connection, ArraySegment<byte> message)
        {
            if (_players.TryGetValue(connection, out var player))
            {
                _messageHandler.HandleMessage(player, message);
            }
            else
            {
                // todo remove or replace with assert
                if (logger.WarnEnabled()) logger.LogWarning($"No player found for message received from client {connection}");
            }
        }
    }
}

public static class NetworkExtensions
{
    /// <summary>
    /// Send a message to all the remote observers
    /// </summary>
    /// <typeparam name="T">The message type to dispatch.</typeparam>
    /// <param name="msg">The message to deliver to clients.</param>
    /// <param name="includeOwner">Should the owner should receive this message too?</param>
    /// <param name="channelId">The transport channel that should be used to deliver the message. Default is the Reliable channel.</param>
    internal static void SendToRemoteObservers<T>(this NetworkIdentity identity, T msg, bool includeOwner = true, Channel channelId = Channel.Reliable)
    {
        identity.Server.SendToObservers(identity, msg, excludeLocalPlayer: true, excludeOwner: !includeOwner, channelId: channelId);
    }
}
