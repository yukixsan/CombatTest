using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider))]
public class CornerPivotTrigger : MonoBehaviour
{
    [Header("Corner Anchors")]
    [Tooltip("Starting point of the corner turn (e.g. edge of Street 1)")]
    [SerializeField] private Transform startAnchor;

    [Tooltip("Optional curve pivot point. If null, automatically calculated from start and end")]
    [SerializeField] private Transform cornerPivotAnchor;

    [Tooltip("Exit point on Street 2")]
    [SerializeField] private Transform endAnchor;

    [Header("Camera Control (Optional)")]
    [Tooltip("Leave empty if using Cinemachine's built-in 'Rotate With Follow Target'. Only assign if you want this script to directly drive camera transform rotation.")]
    [SerializeField] private CinemachineCamera manualCinemachineCamera;

    [Tooltip("Target camera Y angle on the destination street (only used if manualCinemachineCamera is assigned)")]
    [SerializeField] private float targetCameraYAngle = 90f;

    [Header("Settings")]
    [Tooltip("Duration in seconds for the character to traverse the 90-degree corner arc")]
    [SerializeField] private float turnDuration = 1.2f;

    [Tooltip("The StreetTrack component of the destination street")]
    [SerializeField] private StreetTrack targetStreetTrack;

    [SerializeField] private bool triggerOnce = true;
    private bool hasTriggered;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        var player = other.GetComponent<PlayerStateController>() 
                  ?? other.GetComponentInParent<PlayerStateController>();

        if (player == null) return;

        hasTriggered = true;
        StartCoroutine(ExecuteCornerTurnRoutine(player));
    }

    private IEnumerator ExecuteCornerTurnRoutine(PlayerStateController player)
    {
        // 1. Lock combat and player input
        player.LockCombatInput();
        player.Combat?.ResetAttack();
        player.SetMovePermission(false);
        player.SetJumpPermission(false);

        var rb = player.GetComponent<Rigidbody>();
        var mover = player.Movement;

        // 2. Temporarily unfreeze constraints so player can move and rotate freely through the 90° curve
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
        }

        // 3. Play forward locomotion animation
        mover.SetAutomatedMoveInput(1.0f);

        Vector3 p0 = startAnchor != null ? startAnchor.position : player.transform.position;
        Vector3 p2 = endAnchor != null ? endAnchor.position : p0 + Vector3.forward * 4f;
        Vector3 p1 = cornerPivotAnchor != null 
            ? cornerPivotAnchor.position 
            : new Vector3(p2.x, (p0.y + p2.y) * 0.5f, p0.z);

        // Read rotations directly from startAnchor and endAnchor transforms
        Quaternion startRot = startAnchor != null ? startAnchor.rotation : player.transform.rotation;
        Quaternion targetRot = endAnchor != null ? endAnchor.rotation : Quaternion.Euler(0f, 90f, 0f);

        Quaternion startCamRot = manualCinemachineCamera != null ? manualCinemachineCamera.transform.rotation : Quaternion.identity;
        Quaternion targetCamRot = manualCinemachineCamera != null 
            ? Quaternion.Euler(startCamRot.eulerAngles.x, targetCameraYAngle, startCamRot.eulerAngles.z)
            : Quaternion.identity;

        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / turnDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Quadratic Bezier position interpolation
            Vector3 pos = EvaluateBezier(p0, p1, p2, smoothT);
            if (rb != null)
            {
                rb.MovePosition(pos);
            }
            else
            {
                player.transform.position = pos;
            }

            // Smoothly rotate character root transform AND Rigidbody around Y
            Quaternion currentRot = Quaternion.Slerp(startRot, targetRot, smoothT);
            player.transform.rotation = currentRot;
            if (rb != null)
            {
                rb.rotation = currentRot;
                rb.MoveRotation(currentRot);
            }

            // If manual camera rotation is enabled, smoothly orbit it
            if (manualCinemachineCamera != null)
            {
                manualCinemachineCamera.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, smoothT);
            }

            yield return new WaitForFixedUpdate();
        }

        // 4. Conclude turn
        mover.SetAutomatedMoveInput(null);

        if (endAnchor != null)
        {
            if (rb != null) rb.MovePosition(endAnchor.position);
            player.transform.position = endAnchor.position;
        }

        player.transform.rotation = targetRot;
        if (rb != null)
        {
            rb.rotation = targetRot;
        }

        if (manualCinemachineCamera != null)
        {
            manualCinemachineCamera.transform.rotation = targetCamRot;
        }

        // 5. Apply destination street track (Forward direction & 2.5D position constraints)
        if (targetStreetTrack != null)
        {
            targetStreetTrack.ApplyToPlayer(mover);
        }
        else
        {
            // Fallback: freeze X if moving along Z
            Vector3 fallbackForward = endAnchor != null ? endAnchor.forward : Vector3.forward;
            mover.SetStreetTrack(fallbackForward, RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation, targetRot);
        }

        // 6. Restore player permissions
        player.SetMovePermission(true);
        player.SetJumpPermission(true);
        player.ReleaseCombatInput();
    }

    private Vector3 EvaluateBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    public void ResetTrigger() => hasTriggered = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(col.center, col.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
        if (startAnchor != null && endAnchor != null)
        {
            Vector3 p0 = startAnchor.position;
            Vector3 p2 = endAnchor.position;
            Vector3 p1 = cornerPivotAnchor != null 
                ? cornerPivotAnchor.position 
                : new Vector3(p2.x, (p0.y + p2.y) * 0.5f, p0.z);

            Gizmos.color = Color.magenta;
            Vector3 prev = p0;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                Vector3 pt = EvaluateBezier(p0, p1, p2, t);
                Gizmos.DrawLine(prev, pt);
                prev = pt;
            }

            Gizmos.DrawWireSphere(p0, 0.3f);
            Gizmos.DrawWireSphere(p2, 0.3f);
            if (cornerPivotAnchor != null) Gizmos.DrawWireSphere(p1, 0.2f);
        }
    }
}
