using UnityEngine;

public class EnemyHead : MonoBehaviour
{
    public float pushBackForce = 5f;
    public float pushDownForce = 5f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector3 dir = (other.transform.position - transform.position).normalized;

        Vector3 force = new Vector3(
            dir.x * pushBackForce,
            -pushDownForce,
            0
        );

        rb.AddForce(force, ForceMode.VelocityChange);
    }
}