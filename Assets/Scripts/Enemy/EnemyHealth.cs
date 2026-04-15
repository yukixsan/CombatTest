using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EnemyStateAI))]
[RequireComponent(typeof(HealthComponent))]
public class EnemyHealth : MonoBehaviour
{
    private HealthComponent health => GetComponent<HealthComponent>();
    private EnemyStateAI enemyStateAI => GetComponent<EnemyStateAI>();

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

    void OnTakeDamage(float dmg)
    {
        Debug.Log("Damage: " + dmg);
        enemyStateAI.anim.SetTrigger("damage");
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
        enemyStateAI.anim.SetBool("dead", true);
        OnDieEvent?.Invoke();
    }

    void OnStun()
    {
        Debug.Log("Stun!");
        enemyStateAI.anim.SetBool("stun", true);
        OnStunEvent?.Invoke();
    }
}