using Godot;
using System;
using System.Collections.Generic;

namespace Mirage.Events
{
    public partial class AddLateEventGodot : Node
    {
        [Signal] public delegate void LateEventEventHandler();

        private bool hasInvoked = false;
        private List<Action> listeners = new List<Action>();

        public void AddListener(Action handler)
        {
            if (hasInvoked)
            {
                // Call immediately if already invoked
                handler.Invoke();
            }
            else
            {
                // Store listener for future invocation
                listeners.Add(handler);
            }
        }

        public void Invoke()
        {
            hasInvoked = true;
            EmitSignal(nameof(LateEventEventHandler));

            // Call all stored listeners
            foreach (var listener in listeners)
            {
                listener.Invoke();
            }
            // Clear listeners as they were already invoked
            listeners.Clear();
        }

        public void Reset()
        {
            hasInvoked = false;
            listeners.Clear();
        }
    }
}
