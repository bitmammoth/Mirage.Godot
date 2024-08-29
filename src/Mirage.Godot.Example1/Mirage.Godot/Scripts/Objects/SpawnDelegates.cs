using System.Threading.Tasks;
using Mirage.Godot.Scripts.Messages;

namespace Mirage.Godot.Scripts.Objects;

public delegate NetworkIdentity SpawnHandlerDelegate(SpawnMessage msg);
public delegate Task<NetworkIdentity> SpawnHandlerAsyncDelegate(SpawnMessage msg);
public delegate SpawnHandler DynamicSpawnHandlerDelegate(int prefabHash);

// Handles requests to unspawn objects on the client
public delegate void UnSpawnDelegate(NetworkIdentity spawned);
