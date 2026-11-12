using UnityEngine;

public class HeadTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How fast the player is pushed off the side of the head")]
    public float pushSpeed = 5f;

    private void OnTriggerStay(Collider other)
    {
        // 1. Check if the object entering our head trigger is the Player or Enemy
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            Rigidbody targetRb = other.attachedRigidbody;
            if (targetRb != null)
            {
                // Prevent pushing self
                var myRb = GetComponentInParent<Rigidbody>();
                if (targetRb == myRb) return;

                // Determine street forward direction (World X or World Z)
                Vector3 streetForward = Vector3.right;
                var mover = targetRb.GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
                if (mover != null)
                {
                    streetForward = mover.StreetForward;
                }
                else
                {
                    streetForward = Mathf.Abs(transform.forward.z) > 0.5f ? Vector3.forward : Vector3.right;
                }
                streetForward.y = 0f;
                if (streetForward.sqrMagnitude > 0.001f) streetForward.Normalize();

                // Push along the street forward direction instead of hardcoded X
                Vector3 diff = targetRb.position - transform.position;
                float pushDirection = Vector3.Dot(diff, streetForward) >= 0f ? 1f : -1f;

                Vector3 pushOffset = streetForward * (pushDirection * pushSpeed * Time.fixedDeltaTime);

                // Preserve existing Y position and depth coordinate exactly — only push along street forward
                Vector3 newPos = targetRb.position + pushOffset;
                newPos.y = targetRb.position.y;
                if (Mathf.Abs(streetForward.z) > 0.5f)
                {
                    newPos.x = targetRb.position.x;
                }
                else
                {
                    newPos.z = targetRb.position.z;
                }

                targetRb.MovePosition(newPos);
            }
        }
    }
}
