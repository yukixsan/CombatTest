using UnityEngine;
public class IdleState : BasePlayerState
{
    public IdleState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
         controller.SetMovePermission(true);
        controller.SetJumpPermission(true);
        combat.ResetAttack();
        animator.Play("Idle");
                    Debug.Log($"[State Enter] {GetType().Name}");

    }
}

public class MovingState : BasePlayerState
{
    public MovingState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        animator.Play("Move");
        Debug.Log($"[State Enter] {GetType().Name}");

    }
}

public class GroundAttackState : BasePlayerState
{
    public GroundAttackState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        
        // controller.SetMovePermission(false);
        // controller.SetJumpPermission(false);
        //animator.Play("GroundAttack");
                    Debug.Log($"[State Enter] {GetType().Name}");

    }

    public override void OnUpdate()
    {
        // stay until animation ends or IsAttacking false
        if (!combat.isAttacking)
        {
            controller.SwitchState(controller.GroundedState);
            return;
        }
        if(combat.isInRecovery && movement.IsMoving )
        {
            combat.CancelRecovery();
            controller.SwitchState(controller.GroundedState);
            return;
        } 
    }
      public override void OnExit()
    {
        // controller.SetMovePermission(true);
        // controller.SetJumpPermission(true);
    }
}

public class GroundRecoveryState : BasePlayerState
{
    public GroundRecoveryState(PlayerStateController controller) : base(controller) { }
    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");

        //animator.Play("Fall");
    }
    public override void OnUpdate()
    {
        if (movement.IsMoving)
            controller.SwitchState(controller.GroundedState);
    }
}

public class AirRisingState : BasePlayerState
{
    public AirRisingState(PlayerStateController controller) : base(controller) { }
    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");
        if (!combat.isAttacking)
        {
            combat.SetWeaponVisual(true); // reset to default weapon visual when entering air state
            animator.Play("Jump");

        }
    }
}

public class FallingState : BasePlayerState
{
    public FallingState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");

        animator.Play("Fall");
    }
}

public class AirAttackState : BasePlayerState
{
    public AirAttackState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        
        Debug.Log($"[State Enter] {GetType().Name}");

    }

    public override void OnUpdate()
    {
        if (!combat.isAttacking)
        {
            controller.SwitchState(controller.AirborneState);
        }
    }
      
}

public class AirRecoveryState : BasePlayerState
{
    public AirRecoveryState(PlayerStateController controller) : base(controller) { }
    public override void OnEnter()
    {
                    Debug.Log($"[State Enter] {GetType().Name}");

        //animator.Play("Fall");
    }
}
