using Godot;
using System.Reflection;
using System;
using Godot.Collections;
//Placeholder
namespace Mirage
{
    public partial class NetworkInspectorCallbacks : Node
    {
        // Define the events/signals for network state changes
        [Signal] public delegate void OnStartServerEventHandler();
        [Signal] public delegate void OnStartClientEventHandler();
        [Signal] public delegate void OnStartLocalPlayerEventHandler();
        [Signal] public delegate void OnAuthorityChangedEventHandler(bool isAuthority);
        [Signal] public delegate void OnOwnerChangedEventHandler(Dictionary newOwner); 
        [Signal] public delegate void OnStopClientEventHandler();
        [Signal] public delegate void OnStopServerEventHandler();

        private NetworkIdentity Identity;

        public override void _Ready()
        {
            Identity = GetParent<NetworkIdentity>(); // Assuming the NetworkIdentity is the parent

            // Connect signals to the NetworkIdentity events
            Identity.OnStartServer.AddListener(() => EmitSignal(nameof(OnStartServerEventHandler)));
            Identity.OnStartClient.AddListener(() => EmitSignal(nameof(OnStartClientEventHandler)));
            Identity.OnStartLocalPlayer.AddListener(() => EmitSignal(nameof(OnStartLocalPlayerEventHandler)));
            Identity.OnAuthorityChanged.AddListener((isAuthority) => EmitSignal(nameof(OnAuthorityChangedEventHandler), isAuthority));
            Identity.OnOwnerChanged.AddListener((newOwner) => 
            {
                // Convert the new owner to a dictionary for the signal
                Dictionary ownerDict = new Dictionary()
                {
                    //{"owner", (Variant)newOwner}
                };
                
                EmitSignal(nameof(OnOwnerChangedEventHandler), ownerDict);
            });
            Identity.OnStopClient.AddListener(() => EmitSignal(nameof(OnStopClientEventHandler)));
            Identity.OnStopServer.AddListener(() => EmitSignal(nameof(OnStopServerEventHandler)));
        }

#if TOOLS
        public void Convert()
        {
            // Reset the events by clearing the previous connections
            /*
                _onStartServer = GetAndClear<Signal>(nameof(OnStartServerEventHandler));
                _onStartClient = GetAndClear<Signal>(nameof(OnStartClientEventHandler));
                _onStartLocalPlayer = GetAndClear<Signal>(nameof(OnStartLocalPlayerEventHandler));
                _onAuthorityChanged = GetAndClear<Signal>(nameof(OnAuthorityChangedEventHandler));
                _onStopClient = GetAndClear<Signal>(nameof(OnStopClientEventHandler));
                _onStopServer = GetAndClear<Signal>(nameof(OnStopServerEventHandler));
            */
            MarkDirty();
        }

        private T GetAndClear<T>(string signalName) where T : new()
        {
            // Assuming we're resetting the signals; disconnect them first
            //  Disconnect(signalName, this, signalName);
            return new T();
        }

        private void MarkDirty()
        {
            var scene = GetTree().EditedSceneRoot;
            if (scene != null)
            {
                // Mark the scene as dirty to ensure changes are saved
                GD.Print($"Scene marked dirty: {scene.Name}");
            }
        }
#endif
    }
}
