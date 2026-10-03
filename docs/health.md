# Health & Damage

## 5.1 HealthComponent (`Scripts/HealthComponent.cs`, implements `IHealth`)
Central damage/heal/armor/stun authority.

**Events:** `OnDamage(float)`, `OnHeal(float)`, `OnDie`, `OnStun`, `OnStunEnd`, `OnArmorBreak`, `OnArmorRecover`, `OnHealthChanged(float current, float max)`.

**TakeDamage(damage, poiseDamage):**
1. Guard: `currentHealth > 0`
2. Record `lastDamageTime`
3. `currentArmor -= poiseDamage` → if ≤ 0: `ArmorBreak()` → `Stun()`
4. `currentHealth -= damage`
5. Fire `OnHealthChanged`, `OnDamage`
6. If `currentHealth ≤ 0`: `Die()` → `OnDie`

**Armor recovery:** in `Update`; delayed by `armorRecoveryDelay` after last hit; recovers at `armorRecoveryRate * deltaTime`.
**Stun:** coroutine-based (`stunDuration`). Fires `OnStun` → `OnStunEnd`.

## 5.2 PlayerHealth
`RequireComponent(HealthComponent)`. `OnDamage` → `TriggerDamaged()`; `OnDie` → `TriggerDeath()`; `OnStun` → `OnStunEvent` (UnityEvent). `Start`: `healthBar.SetTarget(health)`.

## 5.3 EnemyHealth
`RequireComponent(HealthComponent, EnemyStateAI)` per original doc — **verify**: the enemy architecture moved to `EnemyStateController` (see `enemy.md`); the file may still reference legacy `EnemyStateAI`.
- `OnDamage` → `enemyStateAI.PlayDamage()` (legacy path)
- `OnDie` → `anim.SetBool("dead", true)`
- `Start`: `healthBar.SetTarget(health)`.

## 5.4 UIHealthBar
Subscribes to `IHealth.OnHealthChanged`; sets `fillImage.fillAmount = current / max`. Unsubscribes on `OnDestroy` and on `SetTarget` replacement.
