using Godot;
using System;
using System.Collections.Generic;

namespace Mirage.Events
{
    public partial class AddLateEventGodot<T> : Node, IAddLateEventGodot<T>
    {
        [Signal] public delegate void AddLateGodotEventHandler(Variant newValue);

        private bool _hasInvoked = false;
        private Variant _variantValue;

        // Event for managing handlers
        public event Action<Variant> VariantChanged;

        public Variant VariantValue
        {
            get => _variantValue;
            set
            {
                _variantValue = value;
                EmitSignal(nameof(AddLateGodotEventHandler), _variantValue);
                VariantChanged?.Invoke(_variantValue); // Invoke the delegate event
            }
        }

        public void AddListener(Action<Variant> handler)
        {
            if (_hasInvoked)
            {
                // Immediately invoke handler with the last value if already invoked
                handler.Invoke(_variantValue);
            }
            else
            {
                // Connect handler to the event for future invocation
                VariantChanged += handler; // Use += to subscribe the handler
            }
        }

        public void Invoke(Variant value)
        {
            _hasInvoked = true;
            _variantValue = value;
            EmitSignal(nameof(AddLateGodotEventHandler), _variantValue);

            // Trigger the event for all listeners
            VariantChanged?.Invoke(value);
        }

        public void Reset()
        {
            _hasInvoked = false;
            VariantChanged = null; // Reset the event handlers
        }

        public void RemoveListener(Action<Variant> handler)
        {
            VariantChanged -= handler;
        }
    }
}
