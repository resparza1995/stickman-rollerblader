using UnityEngine;

namespace PlayerSystem
{
    public class GroundedState : IPlayerState
    {
        private readonly PlayerMovement player;

        public GroundedState(PlayerMovement player)
        {
            this.player = player;
        }

        public void Enter()
        {
            // Transition into grounded state
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
