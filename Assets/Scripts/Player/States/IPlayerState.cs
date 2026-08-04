namespace PlayerSystem
{
    public interface IPlayerState
    {
        void Enter();
        void Exit();
        void LogicUpdate();
        void PhysicsUpdate();
    }
}
