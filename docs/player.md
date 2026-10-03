# Player Movement & State System

## 2. Player Movement
**Script:** `PlayerMovement.cs` — Rigidbody-driven; direct `rb.linearVelocity` assignment every `FixedUpdate`. Z always 0.

### FixedUpdate Priority Stack
1. Ground check — `Physics.CheckSphere`
2. PendingLaunch — overrides velocity for 1 frame, then returns
3. Dash state — `externalVelocity.x = dashFacing * dashSpeed`; gravity off
4. Base movement — `moveInput.x * moveSpeed` if `CanMove && !isDashing`
5. Velocity write — `rb.linearVelocity = (baseX + externalVelocity.x, currentY, 0)`
6. Jump — if `_jumpPressed && isGrounded && CanJump`: `velocity.y = jumpForce`
7. Fall mult — `AddForce(Vector3.down * _fallMult)` when airborne and falling

**Model flip** runs in `LateUpdate` via `HandleModelFlip`; guarded by `_stateController.CanFlip`. Uses `_model.localScale.x = ±1`. Facing sign = `Mathf.Sign(_model.localScale.x)`.

**Dash activation:** `ForceDashForward()` called by animation event from dash skill. Sets `isDashing=true`, `dashTimer`, captures `dashFacing`, disables gravity.

**Launch presets** (animation event targets):
- `LaunchUp/Forward/Back/Down(float)` — set `pendingLaunchVelocity`, respect facing.
- `ForceUp/Forward/Back/Down(float)` — `rb.AddForce(ForceMode.VelocityChange)`.

## 3. Player State System

### 3.1 PlayerStateController
Owns state instances (created once in `Awake`). `SwitchState` calls `OnExit → OnEnter`. Dead state cannot be overwritten except by death trigger.

| Property | Setter | Who locks/unlocks |
|---|---|---|
| `CanMove` | `SetMovePermission` | PlayerCombat phases, DamagedState, DeadState |
| `CanJump` | `SetJumpPermission` | PlayerCombat phases, AirborneState.OnExit, DamagedState |
| `CanFlip` | `SetFlipPermission` | PlayerCombat phases, DamagedState |
| `IsCrouching` | `SetCrouching` | PlayerMovement input binding |

- `TriggerDamaged()` — switches to `DamagedState`; if already there, calls `DamagedState.Reset()`.
- `TriggerDeath()` — unconditionally switches to `DeadState`.

### 3.2 State Hierarchy
```
PlayerStateController
├─ GroundState (composite)
│   ├─ IdleState          — ResetAttack(); play "Idle"
│   ├─ MovingState        — play "Move"
│   ├─ CrouchingState     — locks CanMove; sets isCrouching anim bool
│   └─ GroundAttackState  — holds until !isAttacking, then → GroundedState
├─ AirborneState (composite)
│   ├─ AirRisingState     — play "Jump" if !isAttacking
│   ├─ FallingState       — play "Fall"
│   └─ AirAttackState     — holds until !isAttacking, then → AirborneState
├─ DamagedState           — timer-based (stunDuration); ResetAttack on enter
└─ DeadState              — permanent until external respawn
```
- Substate switching (both composites): only if `GetType() != newType`.
- Ground → Air: `!movement.IsGrounded` at top of `GroundState.OnUpdate`. Air → Ground: `movement.IsGrounded` at top of `AirborneState.OnUpdate`.
- `AirborneState.OnExit` restores Move/Jump/Flip. `GroundState.OnExit` locks Jump.

Files: `Projects/PlayerStates/{BasePlayerState,GroundState,AirborneState,SubStates}.cs`, `Scripts/PlayerStateController.cs`.
