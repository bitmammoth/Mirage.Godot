using Godot;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Components;
using System;

public partial class ChainNetwork : Node2D
{
	[Export] public DualNetworkManager _gameClientManager;
	[Export] public NetworkManager _gatewayManager;
	[Export] public DualNetworkManager _authenticationManager;
	[Export] public NetworkManager _gameServerManager;
	[Export] public MyNetworkBehaviour _myNetworkBehaviour;
	public override void _Ready()
	{
		try
		{
			GeneratedCode.Init();
		}
		catch (Exception e)
		{
			GD.PrintErr(e.ToString());
		}

		// Subscribe to events before starting servers and clients
		Subscribe();

		// Start servers first
		_gatewayManager.StartServer();
		_authenticationManager.StartServer();
		_gameServerManager.StartServer();

		// Start clients afterwards
		_gatewayManager.StartClient();
		_gameServerManager.StartClient();

		_gameClientManager.StartClient();
	}

	public void Subscribe()
	{
		if (_gameClientManager == null || _gatewayManager == null || _authenticationManager == null || _gameServerManager == null)
		{
			GD.PrintErr("One or more network managers are null");
			return;
		}
		if (_gameClientManager.Client1 == null || _gameClientManager.Client2 == null || _gatewayManager.Server == null || _gatewayManager.Client == null || _authenticationManager.Server1 == null || _authenticationManager.Server2 == null || _gameServerManager.Server == null || _gameServerManager.Client == null)
		{
			GD.PrintErr("One or more network managers' server or client is null");
			return;
		}

		// Server connection and disconnection
		_gatewayManager.Server.Connected += HandleServerGatewayClientConnected;
		_authenticationManager.Server1.Connected += HandleServerGatewayAuthClientConnected;
		_authenticationManager.Server2.Connected += HandleServerGameAuthClientConnected;
		_gameServerManager.Server.Connected += HandleServerGameClientConnected;

		_gatewayManager.Server.Disconnected += HandleServerGatewayClientDisconnected;
		_authenticationManager.Server1.Disconnected += HandleServerGatewayAuthClientDisconnected;
		_authenticationManager.Server2.Disconnected += HandleServerGameAuthClientDisconnected;
		_gameServerManager.Server.Disconnected += HandleServerGameClientDisconnected;

		// Client connection and disconnection
		_gameClientManager.Client1.Connected.AddListener(HandleClientConnectedToGatewayServer);
		_gameClientManager.Client2.Connected.AddListener(HandleClientConnectedToGameServer);
		_gatewayManager.Client.Connected.AddListener(HandleGatewayAuthClientConnectedToAuthServer);
		_gameServerManager.Client.Connected.AddListener(HandleGameServerAuthClientConnectedToAuthServer);

		_gameClientManager.Client1.Disconnected.AddListener(HandleGatewayClientDisconnectedFromServer);
		_gameClientManager.Client2.Disconnected.AddListener(HandleGameClientDisconnectedFromServer);
		_gatewayManager.Client.Disconnected.AddListener(HandleGatewayAuthClientDisconnectedFromServer);
		_gameServerManager.Client.Disconnected.AddListener(HandleGameServerAuthClientDisconnectedFromServer);
	}

	// Server Connection Handlers
	private void HandleServerGatewayClientConnected(NetworkPlayer player)
	{
		GD.Print($"Client connected to Gateway Server: {player}");
		// Register handler for the TestMessage
		_gatewayManager.Server.MessageHandler.RegisterHandler<TestMessage>(OnTestMessageReceived);
	}
	private void OnTestMessageReceived(TestMessage message)
	{
		GD.Print($"Received message from client: {message.content}");
	}
	private void HandleServerGatewayAuthClientConnected(NetworkPlayer player)
	{
		GD.Print($"Gateway Auth Client connected: {player}");
		// Additional logic here
	}

	private void HandleServerGameAuthClientConnected(NetworkPlayer player)
	{
		GD.Print($"Game Auth Client connected: {player}");
		// Additional logic here
	}

	private void HandleServerGameClientConnected(NetworkPlayer player)
	{
		GD.Print($"Game Client connected to Game Server: {player}");
		// Additional logic here
	}

	// Server Disconnection Handlers
	private void HandleServerGatewayClientDisconnected(NetworkPlayer player)
	{
		GD.Print($"Client disconnected from Gateway Server: {player}");
		// Additional logic here
	}

	private void HandleServerGatewayAuthClientDisconnected(NetworkPlayer player)
	{
		GD.Print($"Gateway Auth Client disconnected: {player}");
		// Additional logic here
	}

	private void HandleServerGameAuthClientDisconnected(NetworkPlayer player)
	{
		GD.Print($"Game Auth Client disconnected fom Auth Game Server: {player}");
		// Additional logic here
	}

	private void HandleServerGameClientDisconnected(NetworkPlayer player)
	{
		GD.Print($"Game Client disconnected from Game Server: {player}");
		// Additional logic here
	}

	// Client Connection Handlers
	private void HandleClientConnectedToGatewayServer(NetworkPlayer player)
	{
		GD.Print($"Client connected to Gateway Server: {player}");
		// Additional logic here
		// Create and send the test message
		var testMessage = new TestMessage
		{
			content = "Hello from the client!"
		};
		player.Send(testMessage);

		GD.Print($"Client connected to Gateway Server: {player}");

		// Assuming MyNetworkBehaviour is attached to a Node in the scene
		_myNetworkBehaviour.TestServerRpc("Hello, Server!", player);
	}
	private void HandleClientConnectedToGameServer(NetworkPlayer player)
	{
		GD.Print($"Client connected to Game Server: {player}");
		// Additional logic here
	}

	private void HandleGatewayAuthClientConnectedToAuthServer(NetworkPlayer player)
	{
		GD.Print($"Gateway Auth Client connected to Auth Server: {player}");
		// Additional logic here
	}

	private void HandleGameServerAuthClientConnectedToAuthServer(NetworkPlayer player)
	{
		GD.Print($"Game Server Auth Client connected to Auth Server: {player}");
		// Additional logic here
	}

	// Client Disconnection Handlers
	private void HandleGatewayClientDisconnectedFromServer(ClientStoppedReason reason)
	{
		GD.Print($"Client disconnected from Gateway Server: {reason}");
		// Additional logic here
	}

	private void HandleGameClientDisconnectedFromServer(ClientStoppedReason reason)
	{
		GD.Print($"Client disconnected from Game Server: {reason}");
		// Additional logic here
	}

	private void HandleGatewayAuthClientDisconnectedFromServer(ClientStoppedReason reason)
	{
		GD.Print($"Gateway Auth Client disconnected from Auth Gateway Server: {reason}");
		// Additional logic here
	}

	private void HandleGameServerAuthClientDisconnectedFromServer(ClientStoppedReason reason)
	{
		GD.Print($"Game Server Auth Client disconnected from Server: {reason}");
		// Additional logic here
	}

	public override void _Process(double delta)
	{
		// Optional: Add any continuous processing logic here if needed
	}
}
