using UnityEngine;
using UnityEngine.Events;

[RequireComponent (typeof(HealthComponent))]
public class PlayerHealth : MonoBehaviour
{
    private HealthComponent health => GetComponent<HealthComponent>();
    private PlayerStateController playerStateController => GetComponent<PlayerStateController>();
    private UIHealthBar healthBar => GetComponent<UIHealthBar>();

    [Header("EVENT")]
    public UnityEvent OnDamageEvent;
    public UnityEvent OnHealEvent;
    public UnityEvent OnDieEvent;
    public UnityEvent OnStunEvent;

    private void Awake()
    {
        health.OnDamage += OnTakeDamage;
        health.OnHeal += OnHeal;
        health.OnDie += OnDie;
        health.OnStun += OnStun;
    }

    private void OnDestroy()
    {
        health.OnDamage -= OnTakeDamage;
        health.OnHeal -= OnHeal;
        health.OnDie -= OnDie;
        health.OnStun -= OnStun;
    }

    private void Start()
    {
        healthBar.SetTarget(health);
    }

    void OnTakeDamage(float dmg)
    {
        Debug.Log("Damage: " + dmg);
        //playerStateController.TriggerDamaged();
        OnDamageEvent?.Invoke();
    }

    void OnHeal(float heal)
    {
        Debug.Log("Heal: " + heal);
        OnHealEvent?.Invoke();
    }

    void OnDie()
    {
        Debug.Log("Die");
        playerStateController.TriggerDeath();
        OnDieEvent?.Invoke();
    }

    void OnStun()
    {
        Debug.Log("Stun!");
        OnStunEvent?.Invoke();
    }
}