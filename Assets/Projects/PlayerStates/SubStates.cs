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
                    //Debug.Log($"[State Enter] {GetType().Name}");

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

public class CrouchingState : BasePlayerState
{
    public CrouchingState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {        
        Debug.Log($"[State Enter] {GetType().Name}");
        controller.SetMovePermission(false);
        animator.SetBool("isCrouching", true);

    }
    public override void OnUpdate()
    {
       if(combat.isAttacking) return; // don't allow movement state changes while attacking
    }
    public override void OnExit()
    {
        controller.SetMovePermission(true);
        animator.SetBool("isCrouching", false);
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
        //Debug.Log($"[State Enter] {GetType().Name}");

        animator.Play("Fall");
    }
}

public class AirAttackState : BasePlayerState
{
    public AirAttackState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        controller.Movement.SetFallMult(1f); // reset fall mult in case it was modified by a previous attack
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
        controller.Movement.ResetFallMult(); // ensure fall mult reset when exiting air attack
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
public class DamagedState : BasePlayerState
{
    private float _timer;
 
    public DamagedState(PlayerStateController controller) : base(controller) { }
 
    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");
        Reset();
    }
 
    // Called by PlayerStateController.TriggerDamaged() when already in this state
    // so repeated hits restart the stun without a full state switch
    public void Reset()
    {
        _timer = controller.Health.stunDuration;
 
        // Clear any lingering attack hitboxes — prevents player's own
        // hitbox staying active and dealing damage mid-stun
        combat.ResetAttack();
 
        controller.SetMovePermission(false);
        controller.SetJumpPermission(false);
        controller.SetFlipPermission(false);
 
        // Force-interrupt whatever animation is playing, including mid-recovery
        animator.Play("hit", 0, 0f);
    }
 
    public override void OnUpdate()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            controller.SwitchState(
                movement.IsGrounded
                    ? (BasePlayerState)controller.GroundedState
                    : controller.AirborneState
            );
        }
    }
 
    public override void OnExit()
    {
        controller.SetMovePermission(true);
        controller.SetJumpPermission(true);
        controller.SetFlipPermission(true);
    }
}
 
public class DeadState : BasePlayerState
{
    public DeadState(PlayerStateController controller) : base(controller) { }
 
    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");
        combat.ResetAttack();
        controller.SetMovePermission(false);
        controller.SetJumpPermission(false);
        controller.SetFlipPermission(false);
        animator.Play("dead", 0, 0f);
    }
 
    // No OnUpdate exit — dead is permanent until scene reload / respawn
    public override void OnExit()
    {
        // Restore permissions if a respawn system calls SwitchState later
        controller.SetMovePermission(true);
        controller.SetJumpPermission(true);
        controller.SetFlipPermission(true);
    }
}
