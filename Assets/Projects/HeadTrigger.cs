using UnityEngine;

public class HeadTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How fast the player is pushed off the side of the head")]
    public float pushSpeed = 5f;

    private void OnTriggerStay(Collider other)
    {
        // 1. Check if the object entering our head trigger is the Player
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            Rigidbody playerRb = other.attachedRigidbody;
            if (playerRb != null)
            {
                // 2. Figure out which side of the head center the player is closer to
                float pushDirection = (playerRb.transform.position.x >= transform.position.x) ? 1f : -1f;

                // 3. Manually translate their position horizontally on the 2D plane
                Vector3 currentPosition = playerRb.transform.position;
                currentPosition.x += pushDirection * pushSpeed * Time.deltaTime;
                
                // Use MovePosition so the Rigidbody updates cleanly without breaking physics interpolation
                playerRb.MovePosition(currentPosition);

                // 4. Zero out downward velocity so they don't awkwardly "sink" into the head while sliding
                Vector3 velocity = playerRb.linearVelocity; // Note: Use .velocity if on Unity 2022 or older
                // if (velocity.y < 0)
                // {
                //     velocity.y = 0;
                //     playerRb.linearVelocity = velocity;
                // }
            }
        }
    }
}
