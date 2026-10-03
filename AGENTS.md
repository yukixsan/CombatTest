# AGENTS.md — Always-loaded project rules

## Project Context
Unity 6 combat game. 2.5D (movement on X/Y, Z frozen). All physics via `rb.linearVelocity`
(never `transform.position +=` alongside a Rigidbody). Use Unity 6 API and current Unity 6 docs.

## Output Rules
- Deliver scripts/files as **markdown only**.
- **Additions/diffs only** — never resend an entire existing script.
- Don't read the whole project for context. Read `docs/INDEX.md` to locate files, then only the
  topic doc(s) and scripts the task actually touches (or that the prompted script references).

## Routing — read only what the task needs
| Task touches | Read |
|---|---|
| Finding a script / unsure where something lives | `docs/INDEX.md` |
| Input, CommandBuffer, PlayerCombat, hitbox/hurtbox, attack/skill data, payload | `docs/combat.md` |
| PlayerMovement, player states, permissions | `docs/player.md` |
| Enemy states, knockback, juggle, enemy rigidbody | `docs/enemy.md` |
| AttackVFX, HitVFX, SFX, hitstop | `docs/vfx-audio.md` |
| HealthComponent, PlayerHealth, EnemyHealth, health bar | `docs/health.md` |
| Full invariant list | `docs/invariants.md` |

## Core Invariants (always apply)
1. Never mix `transform.position +=` with Rigidbody; use `rb.linearVelocity` / `rb.MovePosition`.
2. Facing sign is always `Mathf.Sign(_model.localScale.x)`; never cache it.
3. `DeactivateHitbox()` at `OnRecoveryStart` and `OnAttackEnd`.
4. `currentAttack` / `currentSkill` are mutually exclusive.
5. `commandBuffer.Process` runs from `PlayerCombat.FixedUpdate` only.
6. Enemy states own their own exit conditions; no centralized airborne check.
7. `EnemyStateAI.cs` is legacy — ignore unless asked.

## Maintenance
- New/renamed script → add a line to `docs/INDEX.md`.
- Behavior/contract change → update the matching topic doc in the same task.
- Entries marked `(?)` in INDEX are unverified; fix them when first touched.