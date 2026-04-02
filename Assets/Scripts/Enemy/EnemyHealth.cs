using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyHealth : MonoBehaviour
{
    private HealthComponent health => GetComponent<HealthComponent>();

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
    }

    void OnHeal(float heal)
    {
        Debug.Log("Heal: " + heal);
    }

    void OnDie()
    {
        Debug.Log("Die");
    }

    void OnStun()
    {
        Debug.Log("Stun!");
    }
}