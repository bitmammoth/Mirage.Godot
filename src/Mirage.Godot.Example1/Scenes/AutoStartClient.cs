using Godot;
using Mirage.Godot.Scripts.Components;
using Mirage.Godot.Scripts.Udp;

namespace Mirage.Godot.Scripts;

[GlobalClass]
public partial class AutoStartClient : Node
{
    [Export] private NetworkManager _manager;
    [Export] private UdpSocketFactory _socketFactory;
    [Export] private string _address = "127.0.0.1";
    [Export] private int _port = 7777;

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
		StartClient();
    }
    private void StartClient()
    {
        _socketFactory.Address = _address;
        _socketFactory.Port = _port;
        //_manager.StartClient();
    }

}
