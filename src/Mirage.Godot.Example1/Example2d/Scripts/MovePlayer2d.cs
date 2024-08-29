using Godot;
using Mirage.Godot.Scripts.Objects;

namespace Example2d
{
    [GlobalClass]
    public partial class MovePlayer2d : NetworkBehaviour
    {
        [Export] private float _speed = 1;
        [Export] private float _moveRadius = 20;
        [Export] private Node2D _root;

        public override void _Process(double delta)
        {
            if (!Identity.HasAuthority)
                return;

            var x = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            var z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");

            var deltaVector = new Vector2(x, z) * _speed * (float)delta;
            var newPos = _root.Position + deltaVector;

            var inBoundsPos = ClampPositionInBounds(newPos, _moveRadius);
            _root.Position = inBoundsPos;
        }

        private static Vector2 ClampPositionInBounds(Vector2 newPos, float maxRadius)
        {
            var fromCenter = newPos.DistanceTo(Vector2.Zero);
            if (fromCenter > maxRadius)
            {
                return newPos.Normalized() * maxRadius;
            }
            else
            {
                return newPos;
            }
        }
    }
}

