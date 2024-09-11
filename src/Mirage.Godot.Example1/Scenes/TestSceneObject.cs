using Godot;
using Mirage;

public partial class TestSceneObject : NetworkBehaviour
{
	[ServerRpc(requireAuthority = false)]
	public void ServerRpcTest()
	{
		// This is a server rpc that can be called by any client on this scene object
		GD.Print("ServerRpcTest called");
	}
	[ServerRpc(requireAuthority = true)]
	public void ServerRpcTestRequireAuthority()
	{
		// This is a server rpc that can only be called by the owner of this scene object
		GD.Print("ServerRpcTestRequireAuthority called");
	}
	[ClientRpc]
	public void ClientRpcTest()
	{
		// This is a client rpc that can be called by the server to all clients
		GD.Print("ClientRpcTest called");
	}
	[ClientRpc]
	public void ClientRpcTestOwner()
	{
		// This is a client rpc that can be called by the server to the owner of this scene object
		GD.Print("ClientRpcTestOwner called");
	}
}
