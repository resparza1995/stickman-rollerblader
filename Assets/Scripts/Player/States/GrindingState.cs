using UnityEngine;

namespace PlayerSystem
{
    public class GrindingState : IPlayerState
    {
        private readonly PlayerMovement player;

        public GrindingState(PlayerMovement player)
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
