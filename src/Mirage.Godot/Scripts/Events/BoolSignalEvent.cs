using Godot;
using System;


namespace Mirage.Events
{
    public partial class BoolSignalEvent : Node
    {
        [Signal] public delegate void BoolEventHandler(bool value); // Godot signal for bool values

        private bool _hasInvoked = false;
        private bool _lastValue;
        private event Action<bool> Listeners;

        public void AddListener(Action<bool> handler)
        {
            if (_hasInvoked)
            {
                // Immediately invoke handler with last value if already invoked
                handler.Invoke(_lastValue);
            }
            else
            {
                // Add handler to listeners for future invocation
                Listeners += handler;
            }
        }

        public void Invoke(bool value)
        {
            _hasInvoked = true;
            _lastValue = value;

            // Emit Godot signal and invoke all listeners
            EmitSignal(nameof(BoolEventHandler), value);
            Listeners?.Invoke(value);
        }

        public void Reset()
        {
            _hasInvoked = false;
            Listeners = null; // Clear all listeners
        }
    }

    public partial class BoolAddLateEventGodot : BoolSignalEvent
    {
        // Inherits all functionality from BoolSignalEvent
        // Could add specific logic here for this event type if needed
    }
}


namespace Mirage.Events
{

    public partial class DisconnectAddLateEventGodot : DisconnectEventGodot
    {
        // This inherits the behavior of DisconnectEventGodot
        // Additional behavior specific to this event type could be added here if needed
    }
}
