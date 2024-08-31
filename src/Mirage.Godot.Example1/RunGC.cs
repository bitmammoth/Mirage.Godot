using Godot;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Components;
using System;

public partial class RunGC : Node
{
	[Export] public NetworkManager _manager;
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
		_manager.StartClient();
	}
}
