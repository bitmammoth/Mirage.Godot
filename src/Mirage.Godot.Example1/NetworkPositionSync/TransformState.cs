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

using Godot;

namespace JamesFrowen.PositionSync
{
/*
    public struct TransformState
    {
        public readonly Vector3 position;
        public readonly Vector3 rotation;

        public TransformState(Vector3 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public override string ToString()
        {
            return $"[{position}, {rotation}]";
        }

        public static ISnapshotInterpolator<TransformState> CreateInterpolator() => new Interpolator();

        private class Interpolator : ISnapshotInterpolator<TransformState>
        {
            public TransformState Lerp(TransformState a, TransformState b, float alpha)
            {
                var pos = a.position.Slerp(b.position, alpha);
                var rot = a.rotation.Slerp(b.rotation, alpha);
                //var pos = Vector3.Slerp(a.position, b.position, alpha);
                //var rot = Quaternion.Slerp(a.rotation, b.rotation, alpha);
                return new TransformState(pos, rot);
            }
        }
    }
*/

    public struct TransformState
    {
        public readonly Vector2 position;
        public readonly float rotation;

        public TransformState(Vector2 position, float rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public override string ToString()
        {
            return $"[{position}, {rotation}]";
        }

        public static ISnapshotInterpolator<TransformState> CreateInterpolator() => new Interpolator();

        private class Interpolator : ISnapshotInterpolator<TransformState>
        {
            public TransformState Lerp(TransformState a, TransformState b, float alpha)
            {
                var pos = a.position.Slerp(b.position, alpha);
                var rot = Mathf.Lerp(a.rotation, b.rotation, alpha);
                //var pos = Vector3.Slerp(a.position, b.position, alpha);
                //var rot = Quaternion.Slerp(a.rotation, b.rotation, alpha);
                return new TransformState(pos, rot);
            }
        }
    }
}
