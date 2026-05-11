using UnityEngine;

public class JCHitObject : MonoBehaviour
{
    [Header("Hit Object Settings")]
    [SerializeField] private PlayerHitbox hitbox;
    [SerializeField] private float activeDuration = 0.1f;
    [SerializeField] private float lifeTime = 0.5f;

    private float _timer = 0f;

    public void Initialize(HitboxPayload payload, float facing)
    {
        // Position and orient hitbox based on facing direction
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facing, transform.localScale.y, transform.localScale.z);
        if (hitbox != null)
        {
            hitbox.Initialize(payload.attacker);
            hitbox.SetPayload(payload);
        }
        _timer = lifeTime;

        //Activate hitbox
        Invoke(nameof(DeactivateHitbox), activeDuration);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)        {
            Destroy(gameObject);
        }
    }
    private void DeactivateHitbox()
    {
        if (hitbox != null)
        {
            hitbox.ClearPayload();
        }
    }
}
