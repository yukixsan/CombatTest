# VFX, Audio & Hitstop

## 4.1 AttackVFXManager (`Scripts/VFXHandling/AttackVFXManager.cs`)
- Pool keyed by prefab: `Dictionary<GameObject, Queue<GameObject>>`.
- `Play(AttackPhaseVFX, attachTo, facing)`: parents to `attachTo`; applies `localOffset.x * facing`; flip via Y=180 rotation (`flipByRotation=true`) or negated `localScale.x`; stops and replays `ParticleSystem`.
- `StopAll()` — called at start of every new attack/skill; returns all active to pool.
- `ReturnToPool(prefab, instance)` — called by `VFXPoolReturn` when particle expires.

**VFXPoolReturn** (`VfxReturnPool.cs`): `RequireComponent(ParticleSystem)`; checks `!ps.IsAlive(true)` each `Update`; auto-returns.

**AttackPhaseVFX** (struct in `CombatActionData`): prefab, localOffset, flipByRotation, sfx, sfxVolume.

## 4.2 HitVFXManager (`Projects/HitVFXManager.cs`)
- Array-indexed pool: `vfxPrefabs[]` + `List<Queue<GameObject>> _pools`.
- `SpawnVFX(index, position, rotation)` — world-space, no parent.
- `DespawnVFX(index, obj)` — manual return (caller handles timing).

## 4.3 SFXManager (`Scripts/AudioHandling/SFXManager.cs`)
- Pool: `Queue<AudioSource>`. `PlaySFX(clip, volume)` gets pooled source, plays, auto-returns.
- Called from `PlayerCombat.PlayPhaseSFX` at each phase transition.

## 4.4 HitStopManager (`Projects/HitStopManager.cs`)
- `HitStopManager.Instance.StartHitstop(duration)` — called from `EnemyHurtbox.TryTakeHit` with `payload.HitstopDuration`.
- Sets `Time.timeScale = 0`, waits `WaitForSecondsRealtime(duration)`, then restores the original scale — unless `PauseMenu.Instance.IsPaused` (stays paused).
- Calls while `IsHitstopActive` are ignored (no stacking).
- Because timeScale is 0, anything on scaled time freezes; use unscaled time for UI during hitstop.

## 4.5 Other VFX scripts
- `HitFX` (`Scripts/HitFX.cs`): shake + blink feedback on a hit target; `PlayEffect()`.
- `PlayerLocomotionFX`: dedicated reused particle systems (jump/land/dash/run dust). Not pooled.
- `ComboCountManager`: combo HUD; `RegisterHit()` from hits; DOTween animations.
