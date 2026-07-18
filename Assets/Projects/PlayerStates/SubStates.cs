using UnityEngine;
#region  Idle
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
#endregion
#region Moving
public class MovingState : BasePlayerState
{
    public MovingState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        animator.Play("Move");
        controller.LocomotionFX.StartRunDust();
        Debug.Log($"[State Enter] {GetType().Name}");

    }
    public override void OnExit()
    {
        Debug.Log($"[State Exit] {GetType().Name}");
        controller.LocomotionFX.StopRunDust();  
    }
}
#endregion
#region Crouching
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
#endregion
#region  Ground Attack
public class GroundAttackState : BasePlayerState
{
    public GroundAttackState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        
        Debug.Log($"[State Enter] {GetType().Name}");
        controller.Health.superArmor = 2;

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
        controller.Health.superArmor = 0;

        // controller.SetMovePermission(true);
        // controller.SetJumpPermission(true);
    }
}
#endregion
#region  Ground Recovery
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
#endregion
#region  Air rising
public class AirRisingState : BasePlayerState
{
    public AirRisingState(PlayerStateController controller) : base(controller) { }
    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");
        controller.LocomotionFX.PlayJumpDust();
        if (!combat.isAttacking)
        {
            combat.SetWeaponVisual(true); // reset to default weapon visual when entering air state
            animator.Play("Jump");

        }
    }
}
#endregion
#region Falling
public class FallingState : BasePlayerState
{
    public FallingState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");

        animator.Play("Fall");
    }
}
#endregion
#region  Air Attack
public class AirAttackState : BasePlayerState
{
    public AirAttackState(PlayerStateController controller) : base(controller) { }

    public override void OnEnter()
    {
        controller.Movement.SetFallMult(1f); // reset fall mult in case it was modified by a previous attack
        Debug.Log($"[State Enter] {GetType().Name}");
        controller.Health.superArmor = 2;

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
        controller.Health.superArmor = 0;
    }
      
}
#endregion
#region Air Recovery
public class AirRecoveryState : BasePlayerState
{
    public AirRecoveryState(PlayerStateController controller) : base(controller) { }
    public override void OnEnter()
    {
                    Debug.Log($"[State Enter] {GetType().Name}");

        //animator.Play("Fall");
    }
}
#endregion
#region Damaged
public class DamagedState : BasePlayerState
{private float _timer;
    private HitboxPayload pendingPayload;
    private bool hasPendingPayload;

    public DamagedState(PlayerStateController controller) : base(controller) { }

    public void SetPendingKnockback(HitboxPayload payload)
    {
        pendingPayload = payload;
        hasPendingPayload = true;
    }

    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");
        ResetInternal();

        if (hasPendingPayload)
        {
            ApplyKnockbackImpulse(pendingPayload);
            hasPendingPayload = false;
        }
    }

    // Called by PlayerStateController.TriggerDamaged() when already in this state
    public void Reset(HitboxPayload payload)
    {
        ResetInternal();
        ApplyKnockbackImpulse(payload);
    }

    private void ResetInternal()
    {
        _timer = controller.Health.stunDuration;
        combat.ResetAttack();
        controller.SetMovePermission(false);
        controller.SetJumpPermission(false);
        controller.SetFlipPermission(false);
        animator.Play("hit", 0, 0f);
    }

    private void ApplyKnockbackImpulse(HitboxPayload payload)
    {
        Vector3 velocity = PlayerHitReaction.ResolveKnockbackVelocity(payload, controller.transform.position);
        movement.SetKnockbackVelocity(velocity);
    }

    public override void OnUpdate()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            movement.ClearKnockback();
            controller.SwitchState(
                movement.IsGrounded
                    ? (BasePlayerState)controller.GroundedState
                    : controller.AirborneState
            );
        }
    }

    public override void OnExit()
    {
        movement.ClearKnockback();
        controller.SetMovePermission(true);
        controller.SetJumpPermission(true);
        controller.SetFlipPermission(true);
    }
}
#endregion
#region  Air Damaged
public class PlayerAirborneDamagedState : BasePlayerState
{
    private float _timer;
    private HitboxPayload pendingPayload;
    private bool hasPendingPayload;

    public PlayerAirborneDamagedState(PlayerStateController controller) : base(controller) { }

    public void SetPendingKnockback(HitboxPayload payload)
    {
        pendingPayload = payload;
        hasPendingPayload = true;
    }

    public override void OnEnter()
    {
        Debug.Log($"[State Enter] {GetType().Name}");
        _timer = controller.Health.stunDuration;

        combat.ResetAttack();

        controller.SetMovePermission(false);
        controller.SetJumpPermission(false);
        controller.SetFlipPermission(false);

        animator.Play("hitAir", 0, 0f);

        if (hasPendingPayload)
        {
            ApplyKnockbackImpulse(pendingPayload);
            hasPendingPayload = false;
        }
    }

    // Re-entry path for repeated hits while already airborne-damaged —
    // mirrors EnemyAirborneDamagedState.Reset(payload).
    public void Reset(HitboxPayload payload)
    {
        _timer = controller.Health.stunDuration;
        animator.Play("hitAir", 0, 0f);
        ApplyKnockbackImpulse(payload);
    }

    private void ApplyKnockbackImpulse(HitboxPayload payload)
    {
        Vector3 velocity = PlayerHitReaction.ResolveKnockbackVelocity(payload, controller.transform.position);
        movement.SetKnockbackVelocity(velocity);
    }

    public override void OnUpdate()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            movement.ClearKnockback();
            controller.SwitchState(controller.AirborneState);
        }
    }

    public override void OnExit()
    {
        movement.ClearKnockback();
        controller.SetMovePermission(true);
        controller.SetJumpPermission(true);
        controller.SetFlipPermission(true);
    }
}
#endregion
#region  Dead
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
#endregion