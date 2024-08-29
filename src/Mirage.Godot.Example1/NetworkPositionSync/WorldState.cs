using System.Collections.Generic;

namespace Mirage.Godot.Example1.NetworkPositionSync;

public class WorldStateBuffer
{
    private readonly uint _head;
    private readonly WorldState[] _ring;
}

public class WorldState
{
    private struct StateWithId
    {
        public uint Netid;
        public TransformState State;
    }

    private readonly List<StateWithId> _state = [];

    public void CollectState(IEnumerable<KeyValuePair<uint, SyncPositionBehaviour>> behaviours)
    {
        foreach (var keyValuePair in behaviours)
        {
            var behaviour = keyValuePair.Value;
            var netid = keyValuePair.Key;

            _state.Add(new StateWithId
            {
                Netid = netid,
                State = behaviour.TransformState
            });
        }
    }

}
