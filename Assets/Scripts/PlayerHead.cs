using UnityEngine;

public class PlayerHead : MonoBehaviour
{[Header("Settings")]
    [Tooltip("How fast the player is pushed off the side of the head")]
    public float pushSpeed = 5f;

    private void OnTriggerStay(Collider other)
    {
        // 1. Check if the object entering our head trigger is an Enemy
        if (other.CompareTag("Enemy"))
        {
            Rigidbody enemyRb = other.attachedRigidbody;
            if (enemyRb != null)
            {
                // Prevent self-triggering
                var myRb = GetComponentInParent<Rigidbody>();
                if (enemyRb == myRb) return;

                // Determine street forward direction (World X or World Z)
                Vector3 streetForward = Vector3.right;
                var mover = GetComponentInParent<PlayerMovement>();
                if (mover != null)
                {
                    streetForward = mover.StreetForward;
                }
                streetForward.y = 0f;
                if (streetForward.sqrMagnitude > 0.001f) streetForward.Normalize();

                // Push along the street forward direction instead of hardcoded X
                Vector3 diff = enemyRb.position - transform.position;
                float pushDirection = Vector3.Dot(diff, streetForward) >= 0f ? 1f : -1f;

                Vector3 pushOffset = streetForward * (pushDirection * pushSpeed * Time.fixedDeltaTime);

                // Preserve Y and depth coordinate strictly
                Vector3 newPos = enemyRb.position + pushOffset;
                newPos.y = enemyRb.position.y;
                if (Mathf.Abs(streetForward.z) > 0.5f)
                {
                    newPos.x = enemyRb.position.x;
                }
                else
                {
                    newPos.z = enemyRb.position.z;
                }

                enemyRb.MovePosition(newPos);
            }
        }
    }
}
