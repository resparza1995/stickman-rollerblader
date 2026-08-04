using UnityEngine;
using PlayerSystem;

public class PlayerStateMachine : MonoBehaviour
{
    public IPlayerState CurrentState { get; private set; }

    public void Initialize(IPlayerState startingState)
    {
        CurrentState = startingState;
        CurrentState?.Enter();
    }

    public void ChangeState(IPlayerState newState)
    {
        if (newState == null || newState == CurrentState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    private void Update()
    {
        CurrentState?.LogicUpdate();
    }

    private void FixedUpdate()
    {
        CurrentState?.PhysicsUpdate();
    }
}
