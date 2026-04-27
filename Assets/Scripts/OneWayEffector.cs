using UnityEngine;

public class OneWayEffector : MonoBehaviour
{
    public Collider platformCollider;
    public Transform player;

    public float enableDelay = 0.2f;
    public float dropDuration = 0.3f;
    public float dropForce = 5f;

    private float timer = 0f;
    private float dropTimer = 0f;

    private Collider playerCol;
    private Rigidbody rb;

    void Start()
    {
        playerCol = player.GetComponent<Collider>();
        rb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (player == null) return;

        float playerTop = playerCol.bounds.max.y;
        float platformY = platformCollider.bounds.center.y;

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            dropTimer = dropDuration;

            if (rb != null)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -dropForce, rb.linearVelocity.z);
            }
        }

        if (dropTimer > 0)
        {
            dropTimer -= Time.deltaTime;
            platformCollider.enabled = false;
            return;
        }

        if (playerTop < platformY)
        {
            platformCollider.enabled = false;
            timer = enableDelay;
        }
        else
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                platformCollider.enabled = false;
            }
            else
            {
                platformCollider.enabled = true;
            }
        }
    }
}