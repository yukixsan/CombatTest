using UnityEngine;

public class GroundState : BasePlayerState
{
    private BasePlayerState currentSubState;

    public GroundState(PlayerStateController controller) : base(controller) { }
    
     public override void OnEnter()
    {
        controller.SetMovePermission(true);
        controller.SetJumpPermission(true);

        currentSubState = new IdleState(controller);
        currentSubState.OnEnter();
    }

    public override void OnUpdate()
    {
        // check for airborne
        if (!movement.IsGrounded)
        {
            controller.SwitchState(controller.AirborneState);
            return;
        }

        currentSubState.OnUpdate();

        // substate transitions
        if (combat.isAttacking)
            SwitchSubState(new GroundAttackState(controller));
        
        else if (movement.IsMoving)
            SwitchSubState(new MovingState(controller));
        else
            SwitchSubState(new IdleState(controller));
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
        controller.SetJumpPermission(false);

        currentSubState?.OnExit();
    }
}
