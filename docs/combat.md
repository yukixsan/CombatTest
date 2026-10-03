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
- Polls in `FixedUpdate` with `Physics.OverlapBox` on `_enemyLayer`.
- `_alreadyHit: HashSet<EnemyHurtbox>` prevents multi-hit per activation.
- `ActivateHitbox(payload)` — sets payload, clears hit set.
- `DeactivateHitbox()` — clears payload and hit set.
- `SetPayload(payload)` — legacy path; also immediate overlap check (used by `SkillObject`).

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
