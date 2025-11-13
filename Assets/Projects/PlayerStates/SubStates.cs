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
}

public class FallingState : BasePlayerState
{
    public FallingState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");

        //animator.Play("Fall");
    }
}

public class AirAttackState : BasePlayerState
{
    public AirAttackState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        // controller.SetMovePermission(false);
        //controller.SetJumpPermission(false);
        //animator.Play("AirAttack");
        Debug.Log($"[State Enter] {GetType().Name}");

    }

    public override void OnUpdate()
    {
        if (!combat.isAttacking)
        {
            controller.SwitchState(controller.AirborneState);
        }
    }
      public override void OnExit()
    {
        // controller.SetMovePermission(true);

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
