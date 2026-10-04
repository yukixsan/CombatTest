# Enemy AI (EnemyStateController-based)

**Note:** `EnemyStateAI.cs` is legacy, superseded by `EnemyStateController` + `EnemyBaseState` subclasses. **Ignore it unless asked.**
Files: `Scripts/Enemy/EnemyStates/*`, `EnemyHitBox/EnemyHurtbox/EnemyHealth/EnemyCombat`.

## 6.1 State Hierarchy
```
EnemyStateController
├─ EnemyIdleState — waits chaseCD, checks distance to switch to Chase/Attack
├─ EnemyChaseState — moves toward target in OnFixedUpdate
├─ EnemyAttackState — duration-based hitbox window (see 6.5)
├─ EnemyDamagedState — grounded hitstun/knockback reaction
├─ EnemyAirborneState — neutral "falling/rising, not reacting to damage"
└─ EnemyAirborneDamagedState — airborne hitstun/knockback (juggle)
```
(`EnemyDieState` also exists — not yet documented.)

**Ownership principle:** NO centralized airborne check in `EnemyStateController.Update()`. Each state detects its own exit condition and calls `SwitchState`:
- `EnemyIdleState.OnUpdate` / `EnemyChaseState.OnFixedUpdate` → `AirborneState` if `!movement.IsGrounded`.
- `EnemyDamagedState.OnUpdate` → `AirborneDamagedState` if `!movement.IsGrounded`.
- `EnemyAirborneState.OnUpdate` → `IdleState` if `movement.IsGrounded`.
- `EnemyAirborneDamagedState.OnUpdate` → `AirborneState` (never directly Idle) when `damageTimer` expires — `AirborneState` alone owns the landing check.

Rationale: mirrors `PlayerStateController`; a centralized guard hijacked transitions mid-flight.

## 6.2 SwitchState
```csharp
public void SwitchState(EnemyBaseState newState)
{
    if (currentState == newState && newState != DamagedState && newState != AirborneDamagedState) return;
    currentState?.OnExit(newState);
    currentState = newState;
    currentState.OnEnter();
}
```
`OnExit` receives the incoming state so a state can decide whether to reset physics (critical for Damaged/AirborneDamaged handoffs).

## 6.3 Rigidbody Ownership Model
| State | isKinematic | useGravity | excludeLayers |
|---|---|---|---|
| Idle / Chase / Attack | true | false | default |
| Damaged / AirborneDamaged | false | true | Player (excluded) |
| Airborne | false | true | default |

- Set explicitly at every physics-driven entry point (`ApplyKnockbackImpulse` in both damaged states) — never assume leftovers.
- `rb.excludeLayers = LayerMask.GetMask("Player")` during knockback states; restored (`= 0`) on exit to grounded. Stops player body from shoving the enemy.
- `EnemyDamagedState.OnExit(nextState)` skips kinematic/gravity reset when `nextState` is `AirborneState` or `AirborneDamagedState`; reset only happens when landing in `IdleState`. Resetting on every exit killed knockback impulses.

## 6.4 Knockback & Juggle (`EnemyHitReaction.ApplyKnockback`, static)
Direct `linearVelocity` assignment (delta/`AddForce` approach was removed: caused direction flips on chained hits and collapsed juggle height).
```csharp
public static void ApplyKnockback(HitboxPayload payload, Rigidbody targetRb)
{
    float facingX = Mathf.Sign(targetRb.transform.position.x - payload.attacker.position.x);
    if (facingX == 0f) facingX = 1f;

    float launchY = payload.LaunchForce * payload.LaunchDir;
    float currentVelY = targetRb.linearVelocity.y;

    float finalY;
    if (currentVelY > 0.1f)
    {
        float reducedTarget = launchY * juggleHeightScale;
        finalY = Mathf.Max(currentVelY, reducedTarget);
    }
    else finalY = launchY;

    targetRb.linearVelocity = new Vector3(payload.KnockbackForce * facingX, finalY, 0f);
}
private const float juggleHeightScale = 0.5f; // tune 0.4–0.7 by feel
```
**Invariants:**
- X is always hard-overwritten, never delta'd.
- Juggle detection is velocity-based (`currentVelY > 0.1f`), not state-based; no `isJuggle` parameter plumbing (tried and reverted).
- If juggles feel weak, check attack windup delay vs. fall speed before blaming the formula; also check `juggleHeightScale`.
- No-launch hits (`LaunchForce == 0`) in `AirborneDamagedState` get a forced fixed lift: `finalY = Max(currentVelY, controller.juggleLiftVelocity)` (passed as `noLaunchLift`). `DamagedState` (grounded) passes none, so no-launch attacks don't pop grounded enemies. Gravity is never disabled.

## 6.5 EnemyAttackState
Duration-based hitbox control (animation events were unreliable): `attackDuration` timer on `EnemyStateController`. `EnemyHitBox.Active()`/`Deactive()` called from state enter/exit.

## 6.6 Reset Pattern (Damaged / AirborneDamaged)
In-place re-entry on repeated hits via `Reset(payload)`:
```csharp
public void Reset(HitboxPayload payload)
{
    damageTimer = controller.damagedDuration;
    if (anim != null) anim.SetTrigger("damage");
    ApplyKnockbackImpulse(payload);
}
```
```csharp
public void TriggerDamaged(HitboxPayload payload)
{
    if (!_movement.IsGrounded || IsAirborne || IsAirborneDamaged)
    {
        if (IsAirborneDamaged) { AirborneDamagedState.Reset(payload); return; }
        AirborneDamagedState.SetPendingKnockback(payload);
        SwitchState(AirborneDamagedState);
        return;
    }
    DamagedState.SetPendingKnockback(payload);
    SwitchState(DamagedState);
}
```

## 6.7 Known Follow-ups
- Chase/Attack don't reset kinematic/gravity themselves; they rely on Idle/landing having restored the baseline. Recheck if a new state can reach Chase/Attack without passing Idle.
- Player↔Enemy layer-matrix disabling didn't work; `excludeLayers` during knockback is the working fix.
- Legacy knockback logic in `EnemyStateAI` not migrated/deleted.
