using System;
using Godot;
using Mirage.Godot.Scripts.Components;
using Mirage.Godot.Scripts.Udp;

namespace Mirage.Godot.Scripts;

[GlobalClass]
public partial class NetworkHud : Node
{
    [Export] private NetworkManager _manager;
    [Export] private UdpSocketFactory _socketFactory;
    [Export] private string _address = "127.0.0.1";
    [Export] private int _port = 7777;

    private Button _serverButton;
    private Button _clientButton;
    private Button _hostButton;
    private Button _stopButton;

    // Called when the node enters the scene tree for the first time.
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

        _serverButton = new Button
        {
            Text = "Start Server"
        };
        _serverButton.Pressed += StartServerPressed;
        AddChild(_serverButton);

        _clientButton = new Button
        {
            Text = "Start Client"
        };
        _clientButton.Pressed += StartClientPressed;
        AddChild(_clientButton);

        _hostButton = new Button
        {
            Text = "Start Host"
        };
        _hostButton.Pressed += StartHostPressed;
        AddChild(_hostButton);

        _stopButton = new Button
        {
            Text = "Stop"
        };
        _stopButton.Pressed += StopPressed;
        AddChild(_stopButton);

        var pos = new Vector2(20.0f, 20.0f);
        VerticalLayout(_serverButton, ref pos);
        VerticalLayout(_clientButton, ref pos);
        VerticalLayout(_hostButton, ref pos);
        VerticalLayout(_stopButton, ref pos);

        ToggleButtons(false);
    }

    private void VerticalLayout(Button button, ref Vector2 pos, Vector2? sizeNullable = null, int padding = 10)
    {
        var size = sizeNullable ?? new Vector2(200, 40);
        button.Position = pos;
        button.Size = size;
        pos.Y += size.Y + padding;
    }

    private void StartServerPressed()
    {
        _socketFactory.Port = _port;
        _manager.StartServer();
        ToggleButtons(true);
    }

    private void StartClientPressed()
    {
        _socketFactory.Address = _address;
        _socketFactory.Port = _port;
        _manager.StartClient();
        ToggleButtons(true);
    }

    private void StartHostPressed()
    {
        _socketFactory.Port = _port;
        _manager.StartHost();
        ToggleButtons(true);
    }

    private void StopPressed()
    {
        _manager.Stop();
        ToggleButtons(false);
    }

    private void ToggleButtons(bool disabled)
    {
        _serverButton.Disabled = disabled;
        _clientButton.Disabled = disabled;
        _hostButton.Disabled = disabled;
        _stopButton.Disabled = !disabled;
    }
}
