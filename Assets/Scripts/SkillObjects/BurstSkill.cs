using Unity.VisualScripting;
using UnityEngine;

public class BurstSkill : SkillObject
{

    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private Rigidbody2D rb;

    public override void Initialize(PlayerSkillData data, Transform player)
    {
        base.Initialize(data, player);
        
        //_hitboxPos.transform.localScale = flip;
        // Give it a forward velocity (assuming right = forward in 2D)
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Move along local x-axis (forward), accounting for object's z-rotation
            Vector2 dir = (Vector2)transform.right * _facing;
            rb.linearVelocity = dir * speed;
        }

        // Use pool return 
        var returnComp = GetComponent<SkillObjectReturn>();
        if (returnComp != null)
            returnComp.Setup(data.skillPrefab, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }

        
    }
}
