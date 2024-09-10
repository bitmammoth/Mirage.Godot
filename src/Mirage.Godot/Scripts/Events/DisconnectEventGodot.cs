using Godot;
using System;


namespace Mirage.Events
{
    public partial class DisconnectEventGodot : Node
    {
        [Signal] public delegate void DisconnectEventHandler(ClientStoppedReason reason); // Define the signal

        private bool _hasInvoked = false;
        private ClientStoppedReason _lastReason;
        private event Action<ClientStoppedReason> Listeners;

        public void AddListener(Action<ClientStoppedReason> handler)
        {
            if (_hasInvoked)
            {
                // Immediately invoke handler with the last reason if already invoked
                handler.Invoke(_lastReason);
            }
            else
            {
                // Store listener for future invocation
                Listeners += handler;
            }
        }

        public void Invoke(ClientStoppedReason reason)
        {
            _hasInvoked = true;
            _lastReason = reason;

            // Emit the signal with a string or integer instead of the enum directly
            EmitSignal(nameof(DisconnectEventHandler), reason.ToString());

            // Invoke all stored listeners
            Listeners?.Invoke(reason);
        }

        public void Reset()
        {
            _hasInvoked = false;
            Listeners = null; // Reset the event listeners
        }
    }
}
