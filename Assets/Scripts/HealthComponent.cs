using System;
using System.Collections;
using UnityEngine;
public class HealthComponent : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100;
    public float currentHealth;

    [Header("Poise / Armor")]
    public float maxArmor = 50;
    public float currentArmor;
    public float armorRecoveryRate = 5f;
    public float armorRecoveryDelay = 3f;

    [Header("Stun")]
    public float stunDuration = 1.5f;
    private bool isStunned = false;

    private float lastDamageTime;

    public event Action<float> OnDamage;
    public event Action<float> OnHeal;
    public event Action OnDamaged;
    public event Action OnDie;
    public event Action OnStun;
    public event Action OnArmorBreak;
    public event Action OnArmorRecover;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentArmor = maxArmor;
    }

    private void Update()
    {
        HandleArmorRecovery();
    }

    #region DAMAGE SYSTEM

    public void TakeDamage(float damage, float poiseDamage = 0)
    {
        if (currentHealth <= 0) return;

        lastDamageTime = Time.time;

        if (currentArmor > 0)
        {
            currentArmor -= poiseDamage;

            if (currentArmor <= 0)
            {
                currentArmor = 0;
                ArmorBreak();
            }
        }

        currentHealth -= damage;

        OnDamage?.Invoke(damage);

        OnDamaged?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    #endregion

    #region HEAL SYSTEM

    public void Heal(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHeal?.Invoke(amount);
    }

    #endregion

    #region ARMOR SYSTEM

    private void ArmorBreak()
    {
        OnArmorBreak?.Invoke();
        Stun();
    }

    private void HandleArmorRecovery()
    {
        if (currentArmor >= maxArmor) return;

        if (Time.time - lastDamageTime < armorRecoveryDelay) return;

        currentArmor += armorRecoveryRate * Time.deltaTime;

        if (currentArmor >= maxArmor)
        {
            currentArmor = maxArmor;
            OnArmorRecover?.Invoke();
        }
    }

    #endregion

    #region STUN SYSTEM

    private void Stun()
    {
        if (isStunned) return;

        isStunned = true;
        OnStun?.Invoke();

        StartCoroutine(StunCoroutine());
    }

    private IEnumerator StunCoroutine()
    {
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    public bool IsStunned()
    {
        return isStunned;
    }

    #endregion

    #region DIE

    private void Die()
    {
        OnDie?.Invoke();
    }

    #endregion
}