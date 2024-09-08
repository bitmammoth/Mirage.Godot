using System;
using Godot;
using Mirage;
using Mirage.Logging;

namespace Example1
{
    public partial class PlayerController : NetworkBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger<NetworkTransform3D>();

        [Export]
        public int Speed { get; set; } = 14;
        [Export]
        public int FallAcceleration { get; set; } = 75;

        [SyncVar] private float lookAngle;


        [SyncVar]
        // used to check generic dictionary writer works
        private Godot.Collections.Dictionary<string, int> example_dictionary;

        [SyncVar(hook = nameof(OnTestVarChanged))]
        private int testVar = 0;

        private void OnTestVarChanged()
        {
            GD.Print("testVar changed to: " + testVar);
        }


        private Vector3 _targetVelocity = Vector3.Zero;
        private CharacterBody3D _body;

        public override void _Ready()
        {
            _body = GetParent<CharacterBody3D>();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (this.Identity.HasAuthority)
            {
                try{
                var direction = GetInput();

                // update direction even if not authority
                if (direction != Vector3.Zero)
                {
                    GD.Print("direction: " + direction);
                    GD.Print("Speed: " + Speed);
                    GD.Print("FallAcceleration: " + FallAcceleration);
                    GD.Print("lookAngle: " + lookAngle);
                    lookAngle = GetAngle(direction);
                    testVar++;

                }
                _targetVelocity.X = direction.X * Speed;
                _targetVelocity.Z = direction.Z * Speed;
                }catch(System.Exception e){
                    logger.LogError(e.ToString());
                    //get stack trace
                    logger.LogError(e.StackTrace);
                }
                if (!_body.IsOnFloor())
                {
                    _targetVelocity.Y -= FallAcceleration * (float)delta;
                }

                // set values on CharacterBody3D to move
                _body.Velocity = _targetVelocity;
                _body.Quaternion = new Quaternion(Vector3.Up, lookAngle);
                _body.MoveAndSlide();
            }
            else
            {
                _body.Quaternion = new Quaternion(Vector3.Up, lookAngle);
                _body.MoveAndSlide();
            }
        }

        private static float GetAngle(Vector3 direction)
        {
            direction.Y = 0;
            direction = direction.Normalized();
            var angle = direction.SignedAngleTo(Vector3.Forward, Vector3.Down);
            GD.Print("angle: " + angle);
            return angle;
        }

        private static Vector3 GetInput()
        {
            var direction = Vector3.Zero;

            if (Input.IsActionPressed("move_right"))
            {
                direction.X += 1.0f;
            }
            if (Input.IsActionPressed("move_left"))
            {
                direction.X -= 1.0f;
            }
            if (Input.IsActionPressed("move_back"))
            {
                direction.Z += 1.0f;
            }
            if (Input.IsActionPressed("move_forward"))
            {
                direction.Z -= 1.0f;
            }

            return direction;
        }
    }
}
