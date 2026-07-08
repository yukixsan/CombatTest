# claude.md — Project Architecture Reference

## Project Context

Unity 6 combat game. 2.5D (movement on X/Y, Z frozen). All physics via `rb.linearVelocity`
(never `transform.position +=` alongside Rigidbody). Output convention: additions/diffs only,
markdown format, Unity 6 API throughout.

---

## 1. Combat System

### 1.1 Input Pipeline
PlayerInputReader

└─ PlayerInputActions (generated)

├─ Attack.performed       → CommandBuffer.Enqueue(CommandType.Attack)

├─ Skill01–04.performed   → CommandBuffer.Enqueue(CommandType.Skill, 0–3)

├─ Dash.performed         → CommandBuffer.Enqueue(CommandType.Dash)

└─ Direction.performed/canceled → CommandInterpreter.UpdateDirection(dir)

- `CommandBuffer` holds a `Queue<BufferedCommand>` (max size = `maxBufferSize`, default 1).
- Debounce: duplicate same-type command within `minEnqueueInterval` (0.06s) is dropped.
- Buffer window: commands older than `BUFFER_DURATION` (0.25s) are expired on `Process()`.
- `CommandBuffer.Process(interpreter, allowed)` is called every `FixedUpdate` from `PlayerCombat`.
  - `allowed = !isAttacking || cancelWindowOpen`
  - Dequeues one command per frame when `allowed`.

### 1.2 CommandInterpreter

Routes `BufferedCommand` to the appropriate `PlayerCombat` method:

| CommandType | Target |
|---|---|
| `Attack` | `PlayerCombat.ExecuteAttack(lastDirection)` |
| `Skill` | `PlayerCombat.ExecuteSkill(cmd.skillIndex)` |
| `Dash` | `PlayerCombat.ExecuteDash()` |

`lastDirection` is a `Vector2` updated continuously by `Direction` input, not buffered.

### 1.3 PlayerCombat — Core State Machine

**Key fields:**

| Field | Purpose |
|---|---|
| `isAttacking` | True from `StartAttack/StartSkill` until `OnAttackEnd` |
| `isInRecovery` | Set true at `OnRecoveryStart`, false at `OnAttackEnd` |
| `cancelWindowOpen` | Per-phase flag; gates buffer processing and interrupt logic |
| `currentAttack` / `currentSkill` | Exactly one is non-null during an action |
| `queuedAttack` / `queuedSkill` | At most one queued; FIFO, duplicates dropped |
| `currentComboIndex` | Last combo slot started (1..N); resets after `comboResetTime` |

**ExecuteAttack flow:**

Compute DirectionVariant from Vector2 (Up / Down / Neutral)
Reset combo if Time.time - lastAttackStartTime > comboResetTime
nextCombo = currentComboIndex + 1
FindMatchingAttack(nextCombo, variant)

├─ Exact match (comboIndex + directionVariant)

├─ Direction-only fallback (comboIndex == 0)

└─ Neutral fallback (same comboIndex, Neutral variant)

5a. !isAttacking          → StartAttack immediately

5b. cancelWindowOpen      → interrupt current, StartAttack immediately

5c. queuedAttack == null  → queue it (FIFO; same data = ignored, different = ignored if slot full)


**ExecuteSkill / ExecuteDash follow same interrupt/queue pattern.**
Dash resolves via `FindMatchingDash()`: index 0 = ground, index 1 = airborne, sourced from `dashSkills` list.

**Skill cooldown:** tracked in `PlayerCombat` via `Dictionary<PlayerSkillData, float>`
(lazy check on attempt, not per-frame tick). Gate lives inside `FindMatchingSkill`.

**Air limits:** `airAttackUsage` and `airSkillUsage` dictionaries count uses per-asset per air
session. `ResetAttack()` clears both (called on `IdleState.OnEnter` and `DamagedState.Reset`).

### 1.4 Animation-Event Phase Pipeline

Animation clips must fire these events in order:
OnWindupStart  →  OnActiveStart  →  OnRecoveryStart  →  OnAttackEnd

| Event | Side effects |
|---|---|
| `OnWindupStart` | `cancelWindowOpen` per data flags; PlayVFX+SFX windup; lock Move/Jump/Flip |
| `OnActiveStart` | `cancelWindowOpen` per data; PlayVFX+SFX active; `_hitbox.ActivateHitbox(payload)`; spawn skill object if skill |
| `OnRecoveryStart` | `isInRecovery=true`; deactivate hitbox; PlayVFX+SFX recovery; `cancelWindowOpen` per data; `TryQueuedAttack()` |
| `OnAttackEnd` | Full reset; restore Move/Jump/Flip; consume `queuedAttack` or `queuedSkill` if present |

**Permission matrix by phase:**

| Phase | CanMove | CanJump | CanFlip |
|---|---|---|---|
| Windup | false | false | false |
| Active | false | false | false |
| Recovery | false | false | false* |
| End | true | true | true |

*Flip re-enabled during recovery only when `commandBuffer.HasBufferedCommands`.

### 1.5 Hitbox / Hurtbox

**PlayerHitbox** (`Assets/Scripts/PlayerHitbox.cs`):
- Polls via `FixedUpdate` using `Physics.OverlapBox` on `_enemyLayer`.
- `_alreadyHit: HashSet<EnemyHurtbox>` prevents multi-hit per activation.
- `ActivateHitbox(payload)` — sets payload, clears hit set.
- `DeactivateHitbox()` — clears payload and hit set.
- `SetPayload(payload)` — legacy path; also does an immediate overlap check (used by `SkillObject`).

**HitboxPayload** (struct):
Damage, KnockbackForce, LaunchForce, LaunchDir, HitstopDuration, attacker (Transform), VFXindex

**EnemyHurtbox.TryTakeHit(hitbox):**

Guard: hitbox.HasPayload
Compute knockback direction from attacker position
EnemyStateAI.ApplyKnockback(knockback, moveLockDuration)
HitVFXManager.Instance.SpawnVFX(payload.VFXindex, hitPoint, Quaternion.identity)
HitStopManager.Instance.StartHitstop(payload.HitstopDuration)
healthComponent.TakeDamage(payload.Damage, poiseDamage=20)


---

## 2. Player Movement

**Script:** `PlayerMovement.cs`
**Physics:** Rigidbody-driven; direct `rb.linearVelocity` assignment every `FixedUpdate`.
**Z-axis:** Always 0 (2.5D).

### FixedUpdate Priority Stack

Ground check   — Physics.CheckSphere
PendingLaunch  — overrides velocity for 1 frame, then returns
Dash state     — externalVelocity.x = dashFacing * dashSpeed; gravity off
Base movement  — moveInput.x * moveSpeed if CanMove && !isDashing
Velocity write — rb.linearVelocity = (baseX + externalVelocity.x, currentY, 0)
Jump           — if _jumpPressed && isGrounded && CanJump: velocity.y = jumpForce
Fall mult      — AddForce(Vector3.down * _fallMult) when airborne and falling


**Model flip** runs in `LateUpdate` via `HandleModelFlip`; guarded by `_stateController.CanFlip`.
Flip uses `_model.localScale.x = ±1`. Facing sign = `Mathf.Sign(_model.localScale.x)`.

**Dash activation:** `ForceDashForward()` called by animation event from dash skill.
Sets `isDashing=true`, `dashTimer`, captures `dashFacing`, disables gravity.

**Launch presets** (animation event targets):
`LaunchUp/Forward/Back/Down(float)` — set `pendingLaunchVelocity`, respected facing.
`ForceUp/Forward/Back/Down(float)` — `rb.AddForce(ForceMode.VelocityChange)`.

---

## 3. Player State System

### 3.1 PlayerStateController

Owns state instances (created once in `Awake`). `SwitchState` calls `OnExit → OnEnter`.
Guards: dead state cannot be overwritten except by death trigger.

**Permission properties:**

| Property | Setter | Who locks/unlocks |
|---|---|---|
| `CanMove` | `SetMovePermission` | PlayerCombat phases, DamagedState, DeadState |
| `CanJump` | `SetJumpPermission` | PlayerCombat phases, AirborneState.OnExit, DamagedState |
| `CanFlip` | `SetFlipPermission` | PlayerCombat phases, DamagedState |
| `IsCrouching` | `SetCrouching` | PlayerMovement input binding |

**Special triggers:**
- `TriggerDamaged()` — switches to `DamagedState`; if already there, calls `DamagedState.Reset()`.
- `TriggerDeath()` — unconditionally switches to `DeadState`.

### 3.2 State Hierarchy
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

**Substate switching** (both composites): only switches if `GetType() != newType` (no redundant re-entry).

**GroundState ↔ AirborneState transitions:**
- Ground → Air: `!movement.IsGrounded` checked at top of `GroundState.OnUpdate`
- Air → Ground: `movement.IsGrounded` checked at top of `AirborneState.OnUpdate`

**AirborneState.OnExit** restores Move/Jump/Flip permissions.
**GroundState.OnExit** locks Jump (re-locked when leaving ground).

---

## 4. VFX Systems

### 4.1 AttackVFXManager (per-attack phase VFX)

- Pool keyed by `GameObject` prefab reference: `Dictionary<GameObject, Queue<GameObject>>`.
- `Play(AttackPhaseVFX, attachTo, facing)`:
  - Parents to `attachTo` (player transform).
  - Applies `localOffset.x * facing` for directional offset.
  - Flip: either rotate Y=180 (`flipByRotation=true`) or negate `localScale.x`.
  - Stops and replays `ParticleSystem` for clean state.
- `StopAll()` — called at start of every new attack/skill; returns all active to pool.
- `ReturnToPool(prefab, instance)` — called by `VFXPoolReturn` when particle expires.

**VFXPoolReturn:** `RequireComponent(ParticleSystem)`; checks `!ps.IsAlive(true)` each `Update`;
auto-returns to pool.

**AttackPhaseVFX struct** (defined in `CombatActionData`):
prefab, localOffset (Vector3), flipByRotation (bool), sfx (AudioClip), sfxVolume (float)

### 4.2 HitVFXManager (on-hit VFX)

- Array-indexed pool: `vfxPrefabs[]` + `List<Queue<GameObject>> _pools`.
- `SpawnVFX(index, position, rotation)` — world-space, no parent.
- `DespawnVFX(index, obj)` — manual return (caller responsible for timing).

### 4.3 SFXManager

- Object pool: `Queue<AudioSource>`.
- `PlaySFX(clip, volume)` — gets pooled source, sets clip/volume, plays, auto-returns.
- Called from `PlayerCombat.PlayPhaseSFX` at each phase transition.

---

## 5. Health & Damage

### 5.1 HealthComponent

Central damage/heal/armor/stun authority. Implements `IHealth`.

**Events:** `OnDamage(float)`, `OnHeal(float)`, `OnDie`, `OnStun`, `OnStunEnd`,
`OnArmorBreak`, `OnArmorRecover`, `OnHealthChanged(float current, float max)`.

**TakeDamage(damage, poiseDamage):**

Guard: currentHealth > 0
Record lastDamageTime
currentArmor -= poiseDamage → if ≤ 0: ArmorBreak() → Stun()
currentHealth -= damage
Fire OnHealthChanged, OnDamage
if currentHealth ≤ 0: Die() → OnDie


**Armor recovery:** runs in `Update`; delayed by `armorRecoveryDelay` after last hit;
recovers at `armorRecoveryRate * deltaTime`.

**Stun:** coroutine-based duration (`stunDuration`). Fires `OnStun` → `OnStunEnd`.

### 5.2 PlayerHealth

`RequireComponent(HealthComponent)`. Bridges events to `PlayerStateController`:
- `OnDamage` → `playerStateController.TriggerDamaged()`
- `OnDie` → `playerStateController.TriggerDeath()`
- `OnStun` → fires `OnStunEvent` (UnityEvent)
- On `Start`: `healthBar.SetTarget(health)` — wires `UIHealthBar` to `IHealth`.

### 5.3 EnemyHealth

`RequireComponent(HealthComponent, EnemyStateAI)`. Bridges events to `EnemyStateAI`:
- `OnDamage` → `enemyStateAI.PlayDamage()`
- `OnDie` → `anim.SetBool("dead", true)`
- On `Start`: `healthBar.SetTarget(health)`.

### 5.4 UIHealthBar

Subscribes to `IHealth.OnHealthChanged`; sets `fillImage.fillAmount = current / max`.
Properly unsubscribes on `OnDestroy` and on `SetTarget` replacement.

---

## 6. Enemy AI
## claude.md — Section 6 replacement (Enemy AI)

Replace section 6 (6.1–6.6) with the following updated version reflecting the
new EnemyStateController-based architecture, which has superseded the legacy
EnemyStateAI monolithic script for state/knockback handling.

---

## 6. Enemy AI (EnemyStateController-based)

**Note:** `EnemyStateAI.cs` is a legacy/earlier implementation kept in the
project but superseded by `EnemyStateController` + modular `EnemyBaseState`
subclasses for all new work.

### 6.1 State Hierarchy

EnemyStateController

├─ EnemyIdleState — waits chaseCD, checks distance to switch to Chase/Attack

├─ EnemyChaseState — moves toward target in OnFixedUpdate

├─ EnemyAttackState — duration-based hitbox window (see 6.5)

├─ EnemyDamagedState — grounded hitstun/knockback reaction

├─ EnemyAirborneState — neutral "falling/rising, not reacting to damage" state

└─ EnemyAirborneDamagedState — airborne hitstun/knockback reaction (juggle)

**Ownership principle:** there is NO centralized reactive airborne check in
`EnemyStateController.Update()`. Each state detects its own exit condition
in its own `OnUpdate`/`OnFixedUpdate` and calls `SwitchState` itself:
- `EnemyIdleState.OnUpdate` / `EnemyChaseState.OnFixedUpdate` → switch to
  `AirborneState` if `!movement.IsGrounded`.
- `EnemyDamagedState.OnUpdate` → switch to `AirborneDamagedState` if
  `!movement.IsGrounded`.
- `EnemyAirborneState.OnUpdate` → switch to `IdleState` if `movement.IsGrounded`.
- `EnemyAirborneDamagedState.OnUpdate` → switch to `AirborneState` (never
  directly to Idle) when `damageTimer` expires — `AirborneState` alone owns
  the landing check, giving a natural "falling but not reacting" visual window.

This mirrors the proven `PlayerStateController` pattern (`GroundState`/
`AirborneState` each check their own transition condition) and was adopted
after a centralized `Update()` guard was found to hijack transitions
mid-flight, both blocking valid ones (state-guard swallowing) and forcing
unwanted ones (interrupting an in-progress knockback impulse).

### 6.2 EnemyStateController.SwitchState

```csharp
public void SwitchState(EnemyBaseState newState)
{
    if (currentState == newState && newState != DamagedState && newState != AirborneDamagedState) return;
    currentState?.OnExit(newState);
    currentState = newState;
    currentState.OnEnter();
}
```

`OnExit` takes the incoming state (`EnemyBaseState nextState`) so a state can
decide whether to reset physics based on where it's handing off to — critical
for `DamagedState`/`AirborneDamagedState` handoffs (see 6.3).

### 6.3 Rigidbody Ownership Model

| State | isKinematic | useGravity | excludeLayers |
|---|---|---|---|
| Idle / Chase / Attack | true | false | default |
| Damaged / AirborneDamaged | false | true | Player (excluded) |
| Airborne | false | true | default |

- **Set explicitly at every physics-driven entry point** (`ApplyKnockbackImpulse`
  in both Damaged and AirborneDamaged) — never assumed left over from a
  previous state.
- `rb.excludeLayers = LayerMask.GetMask("Player")` is set during knockback
  states and restored (`= 0`) on exit to a grounded state. This prevents the
  player's own Rigidbody/movement from physically shoving an airborne/damaged
  enemy — a separate issue from intentional knockback, caused by solid
  collision between Player/Enemy body layers.
- `EnemyDamagedState.OnExit(nextState)` / equivalent skip the kinematic/gravity
  reset entirely when `nextState` is `AirborneState` or `AirborneDamagedState`
  — that reset only happens when landing in `IdleState`. Resetting on every
  exit (including airborne handoffs) was the root cause of knockback impulses
  being killed almost immediately after being applied.

### 6.4 Knockback & Juggle Resolution

**Script:** `EnemyHitReaction.ApplyKnockback` (static helper, called by both
`EnemyDamagedState` and `EnemyAirborneDamagedState`).

**Current model: direct `linearVelocity` assignment, not delta/`AddForce`.**
An earlier delta-based approach (`desiredVelocity - targetRb.linearVelocity`
via `ForceMode.VelocityChange`) was replaced after causing:
- Direction-flip on chained hits (stale/reversed leftover X velocity produced
  a negative delta, reversing knockback direction).
- Juggle height collapsing to near-zero (delta against decayed Y velocity
  canceled out intended height caps).

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
        // Already airborne/rising — cap toward a reduced fraction of a full
        // launch instead of stacking or re-launching at full strength.
        float reducedTarget = launchY * juggleHeightScale;
        finalY = Mathf.Max(currentVelY, reducedTarget);
    }
    else
    {
        // Falling, grounded, or at rest — fresh full launch.
        finalY = launchY;
    }

    // X always fully overwritten — no delta/reversal risk from stale velocity.
    targetRb.linearVelocity = new Vector3(payload.KnockbackForce * facingX, finalY, 0f);
}

private const float juggleHeightScale = 0.5f; // tune 0.4–0.7 by feel
```

**Key invariants:**
- **X (horizontal) is never delta'd** — always a hard overwrite each hit.
  Prevents direction-flip artifacts entirely.
- **Y juggle detection is velocity-based, not state-based** — `currentVelY > 0.1f`
  is the sole signal for "treat as juggle," not a controller-tracked
  "was mid-juggle" flag. This removes the need for an `isJuggle` parameter
  threaded through `TriggerDamaged` → `SetPendingKnockback`/`Reset` →
  `ApplyKnockbackImpulse` — that plumbing was tried and reverted as
  overcomplicated for what the velocity check already captures naturally.
- If juggle hits still feel weak/non-launching, check attack windup delay vs.
  fall speed before assuming the formula direction is wrong — a long delay
  between hits can let `currentVelY` decay far below `reducedTarget` before
  the next hit lands, which `Mathf.Max` will correctly top up, but may still
  read as weak if `juggleHeightScale` itself is tuned too low.

### 6.5 EnemyAttackState

Duration-based hitbox control (animation events proved unreliable for timing,
same lesson as player-side): `attackDuration` timer on `EnemyStateController`.
`EnemyHitBox.Active()`/`Deactive()` called directly from state enter/exit,
not from animation event callbacks.

### 6.6 EnemyDamagedState / EnemyAirborneDamagedState — Reset Pattern

Both support in-place re-entry (repeated hits before the stun timer expires)
via a `Reset(payload)` method, avoiding a full `OnExit`/`OnEnter` cycle:

```csharp
public void Reset(HitboxPayload payload)
{
    damageTimer = controller.damagedDuration;
    if (anim != null) anim.SetTrigger("damage");
    ApplyKnockbackImpulse(payload);
}
```

`EnemyStateController.TriggerDamaged` routes to `Reset` when already in the
relevant damaged state, or `SetPendingKnockback` + `SwitchState` otherwise:

```csharp
public void TriggerDamaged(HitboxPayload payload)
{
    if (!_movement.IsGrounded || IsAirborne || IsAirborneDamaged)
    {
        if (IsAirborneDamaged)
        {
            AirborneDamagedState.Reset(payload);
            return;
        }
        AirborneDamagedState.SetPendingKnockback(payload);
        SwitchState(AirborneDamagedState);
        return;
    }

    DamagedState.SetPendingKnockback(payload);
    SwitchState(DamagedState);
}
```

### 6.7 Known Follow-ups / Not Yet Implemented

- `EnemyChaseState`/`EnemyAttackState` do not currently reset kinematic/gravity
  themselves — they rely on `IdleState`/landing paths having already restored
  the grounded baseline. Confirmed sufficient for current transition graph,
  but worth re-checking if a new state is added that can reach Chase/Attack
  without passing through Idle first.
- `Player ↔ Enemy` layer collision matrix disabling was attempted but did not
  take effect as expected; `rb.excludeLayers = LayerMask.GetMask("Player")`
  during knockback states was used instead as the working fix (see 6.3).
- Migration of duplicate/legacy knockback logic in `EnemyStateAI` (old script)
  into this centralized model is still pending — `EnemyStateAI` is left
  as-is/unused for new enemies but not yet deleted.

---

## 7. Data Structures

### CombatActionData (ScriptableObject base)
Name, animationClip, VFXindex

damage, knockbackForce, launchForce, launchDir, hitstunDuration

Airborne (bool), airLimit (int)

canBeCancelledWindup/Active/Recovery (bool)

windupVFX, activeVFX, recoveryVFX (AttackPhaseVFX)

useHandWeapon (bool)

### AttackData : CombatActionData
nextCombo (AttackData)   — unused in current combo resolution

comboIndex (int)         — slot this attack fills (1, 2, 3…)

directionVariant         — Neutral / Up / Down

absoluteRecovery (bool)  — if true, cannot cancel during recovery (not yet enforced in code)

### PlayerSkillData : CombatActionData
cooldown (float), skillPrefab (GameObject), spawnOffset (Vector3), attachToPlayer (bool)

### HitboxPayload (struct)
Damage, KnockbackForce, LaunchForce, LaunchDir, HitstopDuration, attacker (Transform), VFXindex

### AttackPhaseVFX (struct, in CombatActionData)
prefab (GameObject), localOffset (Vector3), flipByRotation (bool), sfx (AudioClip), sfxVolume (float)

---

## 8. Key Invariants & Rules

1. **Never mix `transform.position +=` with Rigidbody** — always drive via `rb.linearVelocity`
   or `rb.MovePosition`. Direct transform writes silently overwrite physics impulses next frame.

2. **Knockback lockout** — `isKnockedBack` blocks `ApplyMovement` for `moveLockDuration` seconds
   after a hit, preventing AI position from overwriting impulse.

3. **Hitbox deactivation** — always call `DeactivateHitbox()` at `OnRecoveryStart` and
   `OnAttackEnd`. `_alreadyHit` cleared on each `ActivateHitbox` call.

4. **One active action** — `currentAttack` and `currentSkill` are mutually exclusive;
   starting one nulls the other.

5. **Cooldown ownership** — skill cooldowns live in `PlayerCombat` (lazy, checked on attempt).
   No per-frame countdown coroutines.

6. **Air usage reset** — `airAttackUsage` and `airSkillUsage` clear in `ResetAttack()`,
   which is called on `IdleState.OnEnter` (landing) and `DamagedState.Reset`.

7. **Buffer gate** — `commandBuffer.Process` is called from `PlayerCombat.FixedUpdate`,
   not from `PlayerInputReader`. The `allowed` flag is computed from combat state, not input state.

8. **VFX StopAll** — called at the start of every `StartAttack` and `StartSkill` to prevent
   orphaned phase VFX from a prior interrupted action.

9. **Facing sign** — always `Mathf.Sign(_model.localScale.x)`. Never cache separately.

10. **Unity 6 API** — use `rb.linearVelocity` (not `rb.velocity`).