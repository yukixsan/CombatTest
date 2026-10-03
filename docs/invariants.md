# Key Invariants & Rules

1. **Never mix `transform.position +=` with Rigidbody** — drive via `rb.linearVelocity` or `rb.MovePosition`. Direct transform writes overwrite physics impulses next frame.
2. **Knockback lockout** (legacy `EnemyStateAI`) — `isKnockedBack` blocks `ApplyMovement` for `moveLockDuration` after a hit. New enemies use state ownership instead (`enemy.md`).
3. **Hitbox deactivation** — always call `DeactivateHitbox()` at `OnRecoveryStart` and `OnAttackEnd`. `_alreadyHit` cleared on each `ActivateHitbox`.
4. **One active action** — `currentAttack` and `currentSkill` are mutually exclusive; starting one nulls the other.
5. **Cooldown ownership** — skill cooldowns live in `PlayerCombat` (lazy check on attempt). No per-frame countdown coroutines.
6. **Air usage reset** — `airAttackUsage`/`airSkillUsage` clear in `ResetAttack()` (called on `IdleState.OnEnter` and `DamagedState.Reset`).
7. **Buffer gate** — `commandBuffer.Process` is called from `PlayerCombat.FixedUpdate`, not `PlayerInputReader`. `allowed` is computed from combat state.
8. **VFX StopAll** — called at the start of every `StartAttack`/`StartSkill`.
9. **Facing sign** — always `Mathf.Sign(_model.localScale.x)`. Never cache separately.
10. **Unity 6 API** — `rb.linearVelocity`, not `rb.velocity`.
