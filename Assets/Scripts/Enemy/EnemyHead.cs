using UnityEngine;

public class EnemyHead : MonoBehaviour
{
    public float pushBackForce = 5f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // Push strictly along the 2.5D street axis without altering Y or depth (Z/X)
        var mover = rb.GetComponent<PlayerMovement>();
        Vector3 streetForward = mover != null ? mover.StreetForward : Vector3.right;
        streetForward.y = 0f;
        if (streetForward.sqrMagnitude > 0.001f) streetForward.Normalize();

        Vector3 diff = other.transform.position - transform.position;
        float pushDirection = Vector3.Dot(diff, streetForward) >= 0f ? 1f : -1f;

        Vector3 force = streetForward * (pushDirection * pushBackForce);
        rb.AddForce(force, ForceMode.VelocityChange);
    }
}