using UnityEngine;

public class AirborneState : BasePlayerState
{
    private BasePlayerState currentSubState;

    public AirborneState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        currentSubState = new FallingState(controller);
        currentSubState.OnEnter();
    }

    public override void OnUpdate()
    {
        if (movement.IsGrounded)
        {
            controller.SwitchState(controller.GroundedState);
            return;
        }

        currentSubState.OnUpdate();

        if (combat.isAttacking)
            SwitchSubState(new AirAttackState(controller));
         else 
            SwitchSubState(new FallingState(controller));
    }

    private void SwitchSubState(BasePlayerState newSubState)
    {
        if (currentSubState?.GetType() == newSubState.GetType()) return;
        currentSubState?.OnExit();
        currentSubState = newSubState;
        currentSubState.OnEnter();
    }

    public override void OnExit()
    {
        currentSubState?.OnExit();
          controller.SetMovePermission(true);
        controller.SetJumpPermission(true);
    }
}
