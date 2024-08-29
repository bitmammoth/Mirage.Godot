using Flecs.NET.Core;
using Godot;
public partial class World1 : Node3D
{
	World World;
    public override void _EnterTree()
    {
        base._EnterTree();
		World = World.Create();
	}
	public override void _Ready()
	{
		var test = World.Entity("Test");
	}
	public override void _Process(double delta)
	{
		World.Progress();
		if(World.Entity("Test").IsAlive())
		{
			GD.Print("Test entity is alive");
		}
	}
}
