using Godot;
using System;
using Godot.Collections; // Godot's own collections

namespace Mirage.Events
{
    public partial class NetworkPlayerAddLateEventGodot : Node
    {
        [Signal] public delegate void NetworkPlayerSignalEventHandler(Variant playerData);

        private bool hasInvoked = false;
        private Variant lastPlayerData;

        // List to store listeners for the network player event
        private System.Collections.Generic.List<Action<Variant>> listeners = new System.Collections.Generic.List<Action<Variant>>();

        // Add listener method to handle late listeners
        public void AddListener(Action<Variant> handler)
        {
            if (hasInvoked)
            {
                // Immediately invoke handler with the last player data if already invoked
                handler.Invoke(lastPlayerData);
            }
            else
            {
                // Store listener for future invocation
                listeners.Add(handler);
            }
        }

        // Method to emit the signal and store the last player as a Variant
        public void Invoke(INetworkPlayer player)
        {
            hasInvoked = true;

            // Convert the network player data to a Variant-compatible Dictionary
            lastPlayerData = NetworkPlayerToVariant(player);

            // Emit the signal with the converted player data
            EmitSignal(nameof(NetworkPlayerSignal), lastPlayerData);

            // Invoke all stored listeners
            foreach (var listener in listeners)
            {
                listener.Invoke(lastPlayerData);
            }

            // Clear listeners as they were already invoked
            listeners.Clear();
        }

        // Reset the event
        public void Reset()
        {
            hasInvoked = false;
            lastPlayerData = new Variant();
            listeners.Clear();
        }

        // Convert the INetworkPlayer to a Variant-compatible Dictionary
        private Variant NetworkPlayerToVariant(INetworkPlayer player)
        {
            // Convert player data to a Dictionary
            var playerData = new Dictionary
            {
                //{ "PlayerId", player.PlayerId }, // Assuming player has an ID or similar property
                //{ "PlayerName", player.PlayerName } // Assuming player has a name or similar property
            };

            return playerData;
        }
    }
}
