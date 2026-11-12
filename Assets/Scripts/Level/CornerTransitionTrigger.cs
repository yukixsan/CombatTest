using UnityEngine;
using Unity.Cinemachine;

[System.Serializable]
public struct CornerTransitionConfig
{
    [Header("Cinemachine Cameras")]
    [Tooltip("Camera tracking Street 1 (priority will be set to 10 initially)")]
    public CinemachineCamera street1Cam;

    [Tooltip("Camera angled around the corner/intersection (priority boosted to 20 during transition)")]
    public CinemachineCamera cornerOrbitCam;

    [Tooltip("Optional camera tracking Street 2. If left empty, street1Cam is automatically reused!")]
    public CinemachineCamera street2Cam;

    [Header("Timing")]
    [Tooltip("How long (in seconds) the player character jogs forward into the corner")]
    public float cornerWalkDuration;

    [Tooltip("Total duration (in seconds) of the camera orbit/blend before snapping to Street 2")]
    public float cameraOrbitDuration;

    [Tooltip("Brief buffer delay (in seconds) after reaching Street 2 before handing back control")]
    public float street2SettleDelay;

    [Header("Movement & Anchors")]
    [Tooltip("Automated horizontal walk direction (+1 for Right, -1 for Left)")]
    public float automatedWalkDirection;

    [Tooltip("Transform marking where the player should land on Street 2 along the X axis")]
    public Transform street2EntryAnchor;
}

[RequireComponent(typeof(Collider))]
public class CornerTransitionTrigger : MonoBehaviour
{
    [SerializeField] private CornerTransitionConfig config = new CornerTransitionConfig
    {
        cornerWalkDuration = 1.0f,
        cameraOrbitDuration = 1.2f,
        street2SettleDelay = 0.1f,
        automatedWalkDirection = 1.0f
    };

    [SerializeField] private bool triggerOnce = true;
    private bool hasTriggered;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        var player = other.GetComponent<PlayerStateController>() 
                  ?? other.GetComponentInParent<PlayerStateController>();

        if (player == null) return;

        hasTriggered = true;
        CornerTransitionManager.Instance.StartTransition(player, config);
    }

    public void ResetTrigger() => hasTriggered = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.center, col.size);
        }

        if (config.street2EntryAnchor != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(config.street2EntryAnchor.position, 0.5f);
            Gizmos.DrawLine(transform.position, config.street2EntryAnchor.position);
        }
    }
}
