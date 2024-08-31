using Godot;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Components;

public partial class RunGWS : Node
{
	[Export] public NetworkManager _manager;
	public override void _Ready()
	{
		try
		{
			//GeneratedCode.Init();
		}
		catch (Exception e)
		{
			GD.PrintErr(e.ToString());
		}
		//_manager.StartServer();
	}
}
