using UnityEngine;

public class OneWayPlatformHandler : MonoBehaviour
{
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float platformScanRadius = 2f;
    [SerializeField] private float dropDuration = 0.3f;

    // How far above the surface feet must be — increase if high jump force causes slip
    [SerializeField] private float surfaceBuffer = 0.15f;

    private bool _isDropping;
    private float _dropTimer;

    private void FixedUpdate()
    {
        if (_isDropping)
        {
            _dropTimer -= Time.fixedDeltaTime;
            if (_dropTimer <= 0f)
                _isDropping = false;
        }

        Collider[] platforms = Physics.OverlapSphere(
            transform.position, platformScanRadius, platformLayer);

        float velocityY = rb.linearVelocity.y;
        // Only consider landing when moving downward or stationary.
        // Rising player (velocityY > 0) always passes through — eliminates flicker on jump.
        bool isFallingOrGrounded = velocityY <= 0.05f;

        foreach (var platform in platforms)
        {
            float surfaceTop = platform.bounds.max.y;
            bool feetAboveSurface = groundCheck.position.y >= surfaceTop - surfaceBuffer;

            bool shouldCollide = feetAboveSurface && isFallingOrGrounded && !_isDropping;
            Physics.IgnoreCollision(playerCollider, platform, !shouldCollide);
        }
    }

    public void DropThrough()
    {
        _isDropping = true;
        _dropTimer = dropDuration;
    }
}