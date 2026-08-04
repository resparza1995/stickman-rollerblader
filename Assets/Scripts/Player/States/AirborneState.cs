using UnityEngine;

namespace PlayerSystem
{
    public class AirborneState : IPlayerState
    {
        private readonly PlayerMovement player;

        public AirborneState(PlayerMovement player)
        {
            this.player = player;
        }

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        public void LogicUpdate()
        {
        }

        public void PhysicsUpdate()
        {
        }
    }
}
