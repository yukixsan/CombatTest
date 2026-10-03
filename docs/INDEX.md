# Script Index

One line per project script. Paths relative to `Assets/`. Third-party folders ignored
(MagicaCloth2, UMotion*, Trails FX, TutorialInfo).
Entries marked `(?)` are still unverified. **Maintenance:** add one line here whenever a script is added/renamed.

## Scripts/ (root) — Player core, health, data
| File | Purpose | Doc |
|---|---|---|
| `Scripts/PlayerCombat.cs` | Attack/skill/dash state machine, phase events | combat |
| `Scripts/PlayerMovement.cs` | Rigidbody movement, dash, launch presets, flip | player |
| `Scripts/PlayerStateController.cs` | Owns player states + permissions | player |
| `Scripts/PlayerHitbox.cs` | OverlapBox hit polling, payload | combat |
| `Scripts/PlayerHurtbox.cs` | Player-side damage receiver (not read in detail) (?) | health |
| `Scripts/PlayerHitReaction.cs` | Static helper: `ResolveKnockbackVelocity(payload, targetPos)` for player knockback | player |
| `Scripts/PlayerHead.cs` | Head-top trigger; `pushSpeed` slides others off the player's head | player |
| `Scripts/PlayerHealth.cs` | HealthComponent -> PlayerStateController bridge | health |
| `Scripts/HealthComponent.cs` | Damage/armor/stun authority | health |
| `Scripts/IHealth.cs` | Health interface | health |
| `Scripts/UIHealthBar.cs` | Fill bar bound to IHealth | health |
| `Scripts/CombatActionData.cs` | ScriptableObject base for actions + `AttackPhaseVFX` | combat |
| `Scripts/HitFX.cs` | On-hit visual feedback on a target: shake + blink (`shake`, `duration`, `magnitude`, `blinkSpeed`); `PlayEffect()` | vfx-audio |
| `Scripts/OneWayEffector.cs` | One-way platform drop-through (`enableDelay`, `dropDuration`, `dropForce`) | player |
| `Scripts/UICamera.cs` | UI camera helper (not read in detail) (?) | — |

## Scripts/Data
| File | Purpose |
|---|---|
| `Scripts/Data/AttackData.cs` | Combo attack data (comboIndex, directionVariant) |
| `Scripts/Data/PlayerSkillData.cs` | Skill data (cooldown, prefab, offset) |
| `Scripts/Data/HitboxPayload.cs` | Hit payload struct |

## Scripts/Enemy (EnemyStateController architecture; doc: enemy)
| File | Purpose |
|---|---|
| `Enemy/EnemyStates/EnemyStateController.cs` | Owns states, SwitchState, TriggerDamaged |
| `Enemy/EnemyStates/EnemyBaseState.cs` | State base class |
| `Enemy/EnemyStates/EnemyIdleState.cs` | Wait, then Chase/Attack by distance |
| `Enemy/EnemyStates/EnemyChaseState.cs` | Move toward target |
| `Enemy/EnemyStates/EnemyAttackState.cs` | Duration-based hitbox window |
| `Enemy/EnemyStates/EnemyDamagedState.cs` | Grounded hitstun/knockback |
| `Enemy/EnemyStates/EnemyAIrborneState.cs` | Neutral airborne (filename casing: `AIrborne`) |
| `Enemy/EnemyStates/EnemyAirborneDamagedState.cs` | Airborne hitstun/juggle |
| `Enemy/EnemyStates/EnemyDieState.cs` | Death state; has `OnEnter`/`OnExit()` (no `nextState` arg) |
| `Enemy/EnemyStates/EnemyHitReaction.cs` | Static `ApplyKnockback` helper |
| `Enemy/EnemyStates/EnemyMovement.cs` | Rigidbody enemy mover: `SetMoveVelocity`, `StopMovement`, `IsGrounded` (`groundCheckDistance`), airborne fall (`BeginAirborneFall`/`StopAirborneFall`/`InterruptFall`/`SetFallMult`) |
| `Enemy/EnemyCombat.cs` | `TakeHit(HitboxPayload)`, `GetDamage()` — hit intake / damage value |
| `Enemy/EnemyHitBox.cs` | `Active()`/`Deactive()` hitbox |
| `Enemy/EnemyHurtbox.cs` | `TryTakeHit` receiver |
| `Enemy/EnemyHealth.cs` | HealthComponent -> enemy bridge |
| `Enemy/EnemyHead.cs` | Head-top trigger; `pushBackForce`/`pushDownForce` push the player off |
| `Enemy/EnemyStateAI.cs` | **LEGACY — ignore unless asked** |

## Scripts/SkillObjects
| File | Purpose |
|---|---|
| `SkillObject.cs` | Base for spawned skill objects (`Initialize(PlayerSkillData, Transform)`); uses `PlayerHitbox.SetPayload` |
| `SkillObjectPool.cs` / `SkillObjectReturn.cs` | Pool + auto-return |
| `BurstSkill.cs` | `SkillObject` subclass — burst skill |
| `JCSkill.cs` | `SkillObject` subclass — spawns `JCHitObject`(s) |
| `JCHitObject.cs` | Hit object: `Initialize(HitboxPayload, facing)` |

## Scripts/VFXHandling & AudioHandling (doc: vfx-audio)
| File | Purpose |
|---|---|
| `VFXHandling/AttackVFXManager.cs` | Per-phase attack VFX pool |
| `VFXHandling/VfxReturnPool.cs` | `VFXPoolReturn` auto-return |
| `VFXHandling/ComboCountManager.cs` | Combo HUD singleton; `RegisterHit()`; DOTween panel/fill/text pop animation |
| `VFXHandling/PlayerLocomotionFX.cs` | Dedicated dust particle systems: `PlayJumpDust(pos)`, `PlayLandDust`, `PlayDashDust`, `StartRunDust`/stop (delayed stop coroutine) |
| `AudioHandling/SFXManager.cs` | Pooled AudioSource SFX |

## Scripts/MenuHandlers
`PauseMenu.cs` — `PauseMenu.Instance.IsPaused` (read by `HitStopManager`)

## Projects/ — Input, commands, player states, managers
| File | Purpose | Doc |
|---|---|---|
| `Projects/PlayerInputReader.cs` | Binds actions -> CommandBuffer/Interpreter | combat |
| `Projects/PlayerInputHub.cs` | Singleton (`DefaultExecutionOrder -100`), wraps `PlayerInput`; caches `InputAction`s (Move, Jump, Direction, Attack, Dash, Skill01–04, Crouch, UICancel) and current map name (`IsGameplayMap`). Intended single shared owner of input state (fixes pause-map desync from multiple `PlayerInputActions` instances) | combat |
| `Projects/PlayerInputActions.cs` / `.inputactions` | Generated wrapper + asset — **the only input asset in use** | combat |
| `Projects/PlayerInputActions 1.*`, `Projects/TAPlayerInputActions.*`, `InputSystem_Actions.inputactions` | Duplicates/testing — **not used, ignore** | — |
| `Projects/PlayerCommand/CommandBuffer.cs` | Buffered command queue | combat |
| `Projects/PlayerCommand/CommandInterpreter.cs` | Routes commands to PlayerCombat | combat |
| `Projects/PlayerCommand/AttackCommand.cs`, `SkillCommand.cs`, `IPlayerCommandd.cs` | Command types (typo `Commandd`) | combat |
| `Projects/PlayerStates/BasePlayerState.cs` | Player state base | player |
| `Projects/PlayerStates/GroundState.cs`, `AirborneState.cs` | Composite states | player |
| `Projects/PlayerStates/SubStates.cs` | Idle/Moving/Crouching/Air* substates | player |
| `Projects/HitStopManager.cs` | Singleton; `StartHitstop(duration)` sets `Time.timeScale=0`, waits realtime, restores unless `PauseMenu.IsPaused`; ignores calls while active (no stacking) | vfx-audio |
| `Projects/HitVFXManager.cs` | Pooled on-hit VFX singleton | vfx-audio |
| `Projects/HeadTrigger.cs` | Trigger on head: pushes Player/Enemy (tags) sideways off via `MovePosition` + `pushSpeed` | player |
| `Projects/OneWayPlatform.cs` | Class `OneWayPlatformHandler`: per-FixedUpdate `IgnoreCollision` vs platforms by feet height/velocity; `DropThrough()` | player |
| `Projects/TA Assets/TutorialStep/*` | Tutorial system (`TutorialManager/PanelView/Step/UI`) | — |
