# Combat System & Data Structures

## 1.1 Input Pipeline
> `PlayerInputActions.inputactions` is the only input asset in use (other copies are test duplicates).
> `PlayerInputHub` (singleton wrapping `PlayerInput`) is the intended single owner of action lookups/map state, shared by `PlayerMovement`/`PlayerInputReader`/pause. The diagram below shows the logical flow; check `PlayerInputReader` to confirm whether it reads via the Hub or the generated wrapper before editing bindings.
```
PlayerInputReader
└─ PlayerInputActions (generated; Projects/PlayerInputActions.cs)
   ├─ Attack.performed       → CommandBuffer.Enqueue(CommandType.Attack)
   ├─ Skill01–04.performed   → CommandBuffer.Enqueue(CommandType.Skill, 0–3)
   ├─ Dash.performed         → CommandBuffer.Enqueue(CommandType.Dash)
   ├─ Burst.performed        → CommandBuffer.Enqueue(CommandType.Burst)
   ├─ Awaken.performed       → CommandBuffer.Enqueue(CommandType.Awaken)
   └─ Direction.performed/canceled → CommandInterpreter.UpdateDirection(dir)
```
- `CommandBuffer` holds a `Queue<BufferedCommand>` (max size = `maxBufferSize`, default 1).
- Debounce: duplicate same-type command within `minEnqueueInterval` (0.06s) is dropped.
- Buffer window: commands older than `BUFFER_DURATION` (0.25s) are expired on `Process()`.
- `CommandBuffer.Process(interpreter, allowed)` is called every `FixedUpdate` from `PlayerCombat`.
  - `allowed = !isAttacking || cancelWindowOpen`
  - Dequeues one command per frame when `allowed`.

## 1.2 CommandInterpreter
| CommandType | Target |
|---|---|
| `Attack` | `PlayerCombat.ExecuteAttack(lastDirection)` |
| `Skill` | `PlayerCombat.ExecuteSkill(cmd.skillIndex)` |
| `Dash` | `PlayerCombat.ExecuteDash()` |
| `Burst` | `PlayerCombat.ExecuteBurst(lastDirection)` |
| `Awaken` | `PlayerCombat.ExecuteAwakening()` |

`lastDirection` is a `Vector2` updated continuously by `Direction` input, not buffered.

## 1.3 PlayerCombat — Core State Machine
| Field | Purpose |
|---|---|
| `isAttacking` | True from `StartAttack/StartSkill` until `OnAttackEnd` |
| `isInRecovery` | Set true at `OnRecoveryStart`, false at `OnAttackEnd` |
| `cancelWindowOpen` | Per-phase flag; gates buffer processing and interrupt logic |
| `currentAttack` / `currentSkill` | Exactly one is non-null during an action |
| `queuedAttack` / `queuedSkill` | At most one queued; FIFO, duplicates dropped |
| `currentComboIndex` | Last combo slot started (1..N); resets after `comboResetTime` |

**ExecuteAttack flow:**
1. Compute DirectionVariant from Vector2 (Up / Down / Neutral)
2. Reset combo if `Time.time - lastAttackStartTime > comboResetTime`
3. `nextCombo = currentComboIndex + 1`
4. `FindMatchingAttack(nextCombo, variant)`: exact match (comboIndex + directionVariant) → direction-only fallback (comboIndex == 0) → neutral fallback (same comboIndex, Neutral)
5. a) `!isAttacking` → `StartAttack` immediately; b) `cancelWindowOpen` → interrupt current, `StartAttack`; c) `queuedAttack == null` → queue it (same data ignored, different ignored if slot full)

**ExecuteSkill / ExecuteDash follow same interrupt/queue pattern.**
Dash resolves via `FindMatchingDash()`: index 0 = ground, index 1 = airborne, from `dashSkills` list.

**Skill cooldown:** `Dictionary<PlayerSkillData, float>` in `PlayerCombat` (lazy check on attempt). Gate lives inside `FindMatchingSkill`.

**Air limits:** `airAttackUsage` / `airSkillUsage` count uses per-asset per air session. `ResetAttack()` clears both (called on `IdleState.OnEnter` and `DamagedState.Reset`).

## 1.4 Animation-Event Phase Pipeline
Events fire in order: `OnWindupStart → OnActiveStart → OnRecoveryStart → OnAttackEnd`

| Event | Side effects |
|---|---|
| `OnWindupStart` | `cancelWindowOpen` per data flags; PlayVFX+SFX windup; lock Move/Jump/Flip |
| `OnActiveStart` | `cancelWindowOpen` per data; PlayVFX+SFX active; `_hitbox.ActivateHitbox(payload)`; spawn skill object if skill |
| `OnRecoveryStart` | `isInRecovery=true`; deactivate hitbox; PlayVFX+SFX recovery; `cancelWindowOpen` per data; `TryQueuedAttack()` |
| `OnAttackEnd` | Full reset; restore Move/Jump/Flip; consume `queuedAttack` or `queuedSkill` if present |

| Phase | CanMove | CanJump | CanFlip |
|---|---|---|---|
| Windup | false | false | false |
| Active | false | false | false |
| Recovery | false | false | false* |
| End | true | true | true |

*Flip re-enabled during recovery only when `commandBuffer.HasBufferedCommands`.

## 1.5 Hitbox / Hurtbox
**PlayerHitbox** (`Scripts/PlayerHitbox.cs`):
- Polls in `FixedUpdate` (and immediately on `SetPayload`) with `Physics.OverlapBox` on `_enemyLayer` using true `BoxCollider` geometry (`TransformPoint` + `size * lossyScale`).
- Resolves hurtbox via `col.GetComponent<EnemyHurtbox>() ?? col.GetComponentInParent<EnemyHurtbox>() ?? col.GetComponentInChildren<EnemyHurtbox>()` so hits register on any part of the enemy collider hierarchy.
- `_alreadyHit: HashSet<EnemyHurtbox>` prevents multi-hit per activation.
- `ActivateHitbox(payload)` — sets payload, clears hit set.
- `DeactivateHitbox()` — clears payload and hit set.
- `SetPayload(payload)` — sets payload and immediately performs overlap check (used by spawned skill objects like `BurstMark`).

**EnemyHurtbox.TryTakeHit(hitbox):**
1. Guard: `hitbox.HasPayload`
2. Compute knockback direction from attacker position
3. Apply knockback (see `enemy.md`; the old `EnemyStateAI.ApplyKnockback` call is legacy)
4. `HitVFXManager.Instance.SpawnVFX(payload.VFXindex, hitPoint, Quaternion.identity)`
5. `HitStopManager.Instance.StartHitstop(payload.HitstopDuration)`
6. `healthComponent.TakeDamage(payload.Damage, poiseDamage=20)`

---

# 7. Data Structures

### CombatActionData (ScriptableObject base)
Name, animationClip, VFXindex; damage, knockbackForce, launchForce, launchDir, hitstunDuration; Airborne (bool), airLimit (int); canBeCancelledWindup/Active/Recovery (bool); windupVFX, activeVFX, recoveryVFX (AttackPhaseVFX); useHandWeapon (bool)

### AttackData : CombatActionData
- nextCombo (AttackData) — unused in current combo resolution
- comboIndex (int) — slot this attack fills (1, 2, 3…)
- directionVariant — Neutral / Up / Down
- absoluteRecovery (bool) — if true, cannot cancel during recovery (not yet enforced)

### PlayerSkillData : CombatActionData
cooldown (float), skillPrefab (GameObject), spawnOffset (Vector3), attachToPlayer (bool)

### HitboxPayload (struct)
Damage, KnockbackForce, LaunchForce, LaunchDir, HitstopDuration, attacker (Transform), VFXindex

### AttackPhaseVFX (struct, in CombatActionData)
prefab (GameObject), localOffset (Vector3), flipByRotation (bool), sfx (AudioClip), sfxVolume (float)

---

# 8. Burst Mark Mechanic (Design — Not Yet Implemented)

## 8.1 Burst Gauge (Resource) — Implemented (Phase 1)
- Lives on the player GameObject as `BurstGauge` (`Scripts/BurstGauge.cs`).
- Max value = **5** (`maxGauge`, configurable in inspector).
- Event `OnGaugeChanged(int current, int max)` notifies listeners (UI, combat logic).
- Subscribes to `ComboCountManager.Instance.OnComboMilestone` (fired every 10 hits by default via `milestoneInterval`). Each milestone gives `+1` gauge (capped at `maxGauge`).
- Persistence: hit counter resets on combo drop, but burst gauge persists.
- API: `AddGauge(int)`, `TryConsume()`, `ResetGauge()`.
- UI: `UIBurstGauge` (`Scripts/VFXHandling/UIBurstGauge.cs`) displays current/max via `TMP_Text` ("Burst: X/5"). Supports inspector wire or `SetTarget(BurstGauge)`.

## 8.2 Burst Dash (Usage) — Implemented (Phase 2)
- **Input:** `Gameplay/Burst` action bound to `<Mouse>/middleButton`. Handled by `PlayerInputReader` -> `CommandBuffer.Enqueue(CommandType.Burst)`.
- **Resource check:** Consumes 1 gauge from `BurstGauge` on successful execution; blocked if gauge is 0.
- **Model flip:** If `|input.x| > 0.1f`, flips `_model.localScale.x = Mathf.Sign(input.x)` immediately so movement aligns with input.
- **Movement execution:** Driven via `PlayerMovement.ForceDirectionalDash(direction.normalized, profile.speed, profile.duration)`:
  - Applies directional `DashProfile` (`horizontalProfile`, `verticalProfile`, `diagonalProfile` via `BurstDashData.GetProfile`) specifying independent `(speed, duration)` per axis type.
  - True 2D slant motion: simultaneous X and Y motion with no frame delays or launch priority conflicts.
  - Dash exit hard-brake: zeros `velocity` upon dash completion so vertical velocity does not float after dash finishes.
  - Ground check guard: skips downward dash if grounded to prevent floor clipping.
  - Neutral (no WASD): no dash or launch.
- **Mark spawn (Phase 3):** Spawns `markPrefab` at dash origin in `StartBurstDash` before movement begins.
- **Direction resolution (`BurstDirection` enum):**
  - Values: `Neutral`, `Horizontal`, `Up`, `Down`, `SlantUp`, `SlantDown`.
  - Resolved via `burstDashData.ResolveDirection(direction)`.
  - Unified across animation selection (`GetAnimationClip`), movement profile lookup (`GetProfile`), and mark payloads (`CreateMarkPayload`).

## 8.3 Burst Mark (Object) — Implemented (Phase 3)
- **Component:** `BurstMark` (`Scripts/SkillObjects/BurstMark.cs`).
- **Spawning:** Instantiated/pooled via `SkillObjectPool.Instance.Spawn` at player origin upon starting a burst dash.
- **Physics/Collision:** No solid Rigidbody. Sits inert with idle visuals until detonation. Does not block player or enemy movement.
- **Lifetime & Detonation:**
  - Configurable `markLifetime` (default 2.5s).
  - Automatically detonates upon lifetime expiry, or can be force-detonated via `BurstMark.DetonateAll()`.
  - On spawn: plays `spawnVFXPrefab` and `spawnSFX`.
  - On detonation: disables idle visual, spawns `detonationVFXPrefab`, plays `detonationSFX`, activates `PlayerHitbox` for 2 physics frames using its stored directional `HitboxPayload`, and returns to `SkillObjectPool`.
- **Phase 4 Registry:** Maintains static `BurstMark.ActiveMarks` and `BurstMark.DetonateAll()` for skill-triggered detonations.

## 8.4 Direction → Payload Mapping — Implemented (Phase 3)
Built via `burstDashData.CreateMarkPayload(BurstDirection, attackerTransform)`:

| Burst Direction | Knockback Force | Launch Force | Launch Dir | Description |
|---|---|---|---|---|
| **Neutral** | 0 | 0 | 0 | Pure damage |
| **Horizontal** | `horizontalPayload.knockbackForce` | 0 | 0 | Horizontal push away |
| **Up** / **SlantUp** | `upPayload.knockbackForce` | `upPayload.launchForce` | +1 | Pop-up launch |
| **Down** / **SlantDown** | `downPayload.knockbackForce` | `downPayload.launchForce` | -1 | Spike / slam down |

All values, base damage, hitstop duration, hit VFX index, spawn VFX/SFX, detonation VFX, and SFX are configurable in `BurstDashData`.

## 8.5 Sequential Skill Detonation — Implemented (Phase 4)
- **Skill Flag:** `PlayerSkillData.detonatesBurstMarks` (`bool`) flags any skill to trigger detonation of active burst marks.
- **Detonation Phase:** Fires at `PlayerCombat.OnActiveStart()` during the skill's active strike frame.
- **Sequencing Order:** Detonates marks in FIFO order (from oldest spawned mark to newest).
- **Sequencing Interval:** Controlled by `BurstDashData.sequentialDetonationInterval` (default `0.08s`), creating a chain-pop effect along the player's previous burst dash path.
- **API:** `BurstMark.DetonateSequentially(MonoBehaviour runner, float interval)`. Fallback `BurstMark.DetonateAll()` available for instantaneous detonation.

---

# 9. Awakening System

## 9.1 Input & Activation
- **Input:** `Gameplay/Awaken` action bound via `PlayerInputHub` -> `PlayerInputReader` -> `CommandBuffer.Enqueue(CommandType.Awaken)`.
- **Command Dispatch:** `CommandInterpreter.Receive` -> `PlayerCombat.ExecuteAwakening()`.
- **Cancel Windows:** Can activate from neutral or cancel out of attacks/skills when `cancelWindowOpen == true`.
- **Activation Flow:** `PlayerCombat.StartAwakeningActivation()` plays ground or airborne activation clip from `AwakeningData`. At `OnActiveStart()`, `AwakeningManager.TriggerAwakening()` begins the awakening session.

## 9.2 Stats & Animation Speed Tuning
- **Damage Multiplier:** `AwakeningManager.DamageMultiplier` multiplies attack and skill damage in `PlayerCombat.OnActiveStart()`.
- **Movement Speed:** `PlayerMovement.MoveSpeedMultiplier` multiplies base horizontal input velocity.
- **Animation Speed:** `AwakeningData.animSpeedMultiplier` scales `animator.speed` during awakening and restores `1.0f` on deactivation.

## 9.3 Awakened Skill Swapping
- **Data Configuration:** `AwakeningData.awakenedGroundSkills` and `awakenedAirSkills` lists (0..3) mirror `PlayerCombat`'s skill lists.
- **Slot Override Flow:** When `PlayerCombat.FindMatchingSkill(index)` is queried during awakening, it checks the corresponding slot index in `awakenedGroundSkills` (or `awakenedAirSkills`).
- **Graceful Fallback:** If a slot in the awakened list is left empty/null, it automatically falls back to executing the standard equipped skill for that slot. Base lists are never mutated in memory.

