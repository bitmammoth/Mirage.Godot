using Godot;
using System;

namespace Mirage.Events
{
    // Interface for events that can only run once, adding a handler late will invoke it right away.
    public interface IAddLateEventGodot
    {
        void AddListener(Action handler);
        void RemoveListener(Action handler);
    }

    // Interface for events with 1 argument.
    public interface IAddLateEventGodot<T0>
    {
        void AddListener(Action<Variant> handler);
        void RemoveListener(Action<Variant> handler);
    }

    // Interface for events with 2 arguments.
    public interface IAddLateEventGodot<T0, T1>
    {
        void AddListener(Action<Variant, Variant> handler);
        void RemoveListener(Action<Variant, Variant> handler);
    }
}
