using Godot;
using Mirage;
public partial class NetworkResourcePreloader : ResourcePreloader
{
	public override void _Ready()
	{
		base._Ready();
		RegisterResources();
	}
	private void RegisterResources()
	{
		var resourceList = GetResourceList();
		foreach (var resource in resourceList)
		{
			var res = GetResource(resource);
			var packedScene = res as PackedScene;
			var playerObject = packedScene.Instantiate();
			var _prefabHash = PrefabHashHelper.GetPrefabHash(packedScene);
			var netIdentity = NodeHelper.GetNetworkIdentity(playerObject);
			if (netIdentity != null)
			{
				GD.Print($"Registering {packedScene.ResourceName} with hash {_prefabHash}");
				netIdentity.PrefabHash = _prefabHash;
				GD.Print($"PrefabHash: {netIdentity.PrefabHash}");
			}
		}
	}
}