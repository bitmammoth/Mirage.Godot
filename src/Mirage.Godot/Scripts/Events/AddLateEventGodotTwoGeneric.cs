using Godot;
using System;

namespace Mirage.Events
{
    public partial class AddLateEventGodot<T0, T1> : Node, IAddLateEventGodot<T0, T1>
    {
        [Signal] public delegate void AddLateGodotEventHandler(Variant arg0, Variant arg1);

        private bool _hasInvoked = false;
        private Variant _variantValue0;
        private Variant _variantValue1;

        // Event for managing handlers
        public event Action<Variant, Variant> VariantChanged;

        public Variant Value0
        {
            get => _variantValue0;
            set
            {
                _variantValue0 = value;
                EmitSignal(nameof(AddLateGodotEventHandler), _variantValue0, _variantValue1);
                VariantChanged?.Invoke(_variantValue0, _variantValue1); // Invoke the delegate event
            }
        }

        public Variant Value1
        {
            get => _variantValue1;
            set
            {
                _variantValue1 = value;
                EmitSignal(nameof(AddLateGodotEventHandler), _variantValue0, _variantValue1);
                VariantChanged?.Invoke(_variantValue0, _variantValue1); // Invoke the delegate event
            }
        }

        public void AddListener(Action<Variant, Variant> handler)
        {
            if (_hasInvoked)
            {
                // Immediately invoke handler with the last values if already invoked
                handler.Invoke(_variantValue0, _variantValue1);
            }
            else
            {
                // Connect handler to the event for future invocation
                VariantChanged += handler; // Use += to subscribe the handler
            }
        }

        public void Invoke(Variant value0, Variant value1)
        {
            _hasInvoked = true;
            _variantValue0 = value0;
            _variantValue1 = value1;
            EmitSignal(nameof(AddLateGodotEventHandler), _variantValue0, _variantValue1);

            // Trigger the event for all listeners
            VariantChanged?.Invoke(value0, value1);
        }

        public void Reset()
        {
            _hasInvoked = false;
            VariantChanged = null; // Reset the event handlers
        }

        public void RemoveListener(Action<Variant, Variant> handler)
        {
            VariantChanged -= handler;
        }
    }
}


