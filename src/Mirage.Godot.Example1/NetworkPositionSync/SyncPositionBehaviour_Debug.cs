/*
MIT License

Copyright (c) 2021 James Frowen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;
using Mirage;
using Godot;

/*
namespace JamesFrowen.PositionSync
{
    public partial class SyncPositionBehaviour_Debug : NetworkBehaviour
    {
        private SyncPositionBehaviour behaviour;
        private List<Node> markers = new List<Node>();
        private SyncPositionSystem _system;
        public float maxTime = 5;
        public float MaxScale = 1;

        private void Awake()
        {
            behaviour = GetNode<SyncPositionBehaviour>("SyncPositionBehavior");
        }
        private void Update()
        {
            if (!Identity.IsClient) return;
            if (_system == null)
                _system = Identity.ClientObjectManager.GetNode<SyncPositionSystem>("SyncPositionSystem");

            foreach (var marker in markers)
                marker.SetActive(false);

            var buffer = behaviour.snapshotBuffer.DebugBuffer;
            for (var i = 0; i < buffer.Count; i++)
            {
                var snapshot = buffer[i];
                if (markers.Count <= i)
                    markers.Add(CreateMarker());

                markers[i].SetActive(true);
                markers[i].transform.SetPositionAndRotation(snapshot.state.position, snapshot.state.rotation);
                var pos = snapshot.state.position;
                var hash = (pos.x * 501) + pos.z;
                markers[i].GetComponent<Renderer>().material.color = Color.HSVToRGB(hash * 20 % 1, 1, 1);
                var snapshotTime = _system.TimeSync.Time;

                var absTimeDiff = Mathf.Abs((float)(snapshotTime - snapshot.time));
                var sizeFromDiff = Mathf.Clamp01((maxTime - absTimeDiff) / maxTime);
                var scale = sizeFromDiff * MaxScale;
                markers[i].transform.localScale = Vector3.one * scale;
            }
        }

        private Node CreateMarker()
        {
            var marker = Node3D.CreatePrimitive(PrimitiveType.Sphere);

            return marker;
        }
    }
}
*/
