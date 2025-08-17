using UnityEngine;

public class SkillObject : MonoBehaviour
{
    protected int damage;
    protected float knockback;

    public virtual void Initialize(PlayerSkillData data, Transform player)
    {
        transform.position = player.position + player.TransformDirection(data.spawnOffset);
        transform.rotation = player.rotation;

        if (data.attachToPlayer)
            transform.SetParent(player);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Hit {other.name} for {damage} damage!");
        // TODO: integrate Health later
    }
}
