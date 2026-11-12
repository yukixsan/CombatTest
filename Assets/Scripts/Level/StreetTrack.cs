using UnityEngine;

public class StreetTrack : MonoBehaviour
{
    [Header("Street Configuration")]
    [Tooltip("Forward direction of this street (e.g. (1,0,0) for World X, (0,0,1) for World Z)")]
    public Vector3 forwardDirection = Vector3.right;

    public enum DepthAxisLock
    {
        [Tooltip("Automatically detects based on forwardDirection (e.g. locks Z for World X streets, locks X for World Z streets)")]
        AutoDetect,
        [Tooltip("Locks Z position (standard 2.5D street running along World X)")]
        FreezeZ,
        [Tooltip("Locks X position (highway running along World Z)")]
        FreezeX
    }

    [Tooltip("Which depth axis to lock to prevent the character from drifting off the road into the background/foreground. Also automatically freezes all rotations.")]
    public DepthAxisLock depthLock = DepthAxisLock.AutoDetect;

    public RigidbodyConstraints GetConstraints()
    {
        DepthAxisLock mode = depthLock;
        if (mode == DepthAxisLock.AutoDetect)
        {
            mode = Mathf.Abs(forwardDirection.x) >= Mathf.Abs(forwardDirection.z) 
                ? DepthAxisLock.FreezeZ 
                : DepthAxisLock.FreezeX;
        }

        RigidbodyConstraints posFreeze = mode == DepthAxisLock.FreezeZ 
            ? RigidbodyConstraints.FreezePositionZ 
            : RigidbodyConstraints.FreezePositionX;

        return posFreeze | RigidbodyConstraints.FreezeRotation;
    }

    [Header("Player Rotation & Alignment")]
    [Tooltip("Target player Y rotation on this street (e.g. 0 for Street 1, 90 for Street 2)")]
    public float playerYRotation = 90f;

    [Tooltip("If true, snaps characters to this track's exact depth coordinate (X on highway, Z on street) so they share the exact same 2.5D lane")]
    public bool snapToTrackDepth = false;

    public void ApplyToPlayer(PlayerMovement player)
    {
        if (player == null) return;
        Quaternion rot = Quaternion.Euler(0f, playerYRotation, 0f);
        player.SetStreetTrack(forwardDirection, GetConstraints(), rot);

        if (snapToTrackDepth)
        {
            Vector3 pos = player.transform.position;
            bool isZStreet = Mathf.Abs(forwardDirection.z) > Mathf.Abs(forwardDirection.x);
            if (isZStreet)
            {
                pos.x = transform.position.x;
            }
            else
            {
                pos.z = transform.position.z;
            }
            player.TeleportTo(pos);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var mover = other.GetComponent<PlayerMovement>() ?? other.GetComponentInParent<PlayerMovement>();
        if (mover != null)
        {
            ApplyToPlayer(mover);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, forwardDirection.normalized * 3f);
    }
}
