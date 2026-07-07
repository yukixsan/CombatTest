using UnityEngine;

public class PlayerHead : MonoBehaviour
{
    [SerializeField] private float sidewaysNudgeForce = 2f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        Rigidbody enemyRb = other.attachedRigidbody;
        if (enemyRb == null) return;

        // Only react if the enemy is actively falling onto the player —
        // ignores enemies merely standing adjacent or moving upward.
        if (enemyRb.linearVelocity.y >= 0f) return;

        // One-shot horizontal nudge so the enemy doesn't visually rest
        // inside the player model — does not block or repeatedly push.
        float dirX = Mathf.Sign(other.transform.position.x - transform.position.x);
        if (dirX == 0f) dirX = 1f;

        enemyRb.AddForce(new Vector3(dirX * sidewaysNudgeForce, 0f, 0f), ForceMode.VelocityChange);
    }
}
