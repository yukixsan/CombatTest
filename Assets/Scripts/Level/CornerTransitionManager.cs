using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CornerTransitionManager : MonoBehaviour
{
    private static CornerTransitionManager _instance;
    public static CornerTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<CornerTransitionManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[CornerTransitionManager]");
                    _instance = go.AddComponent<CornerTransitionManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Cinemachine References")]
    [SerializeField] private CinemachineBrain cinemachineBrain;

    private bool _isTransitioning;
    public bool IsTransitioning => _isTransitioning;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (cinemachineBrain == null)
        {
            cinemachineBrain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : FindAnyObjectByType<CinemachineBrain>();
        }
    }

    public void StartTransition(PlayerStateController player, CornerTransitionConfig config)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(player, config));
    }

    private IEnumerator TransitionRoutine(PlayerStateController player, CornerTransitionConfig config)
    {
        _isTransitioning = true;

        // 1. Lock combat and normal movement input
        player.LockCombatInput();
        player.Combat?.ResetAttack();
        player.SetMovePermission(false);
        player.SetJumpPermission(false);

        // 2. Start automated movement into the corner (if configured)
        if (config.cornerWalkDuration > 0f)
        {
            player.Movement.SetAutomatedMoveInput(config.automatedWalkDirection);
        }

        // 3. Initiate camera blend to the Corner Orbit Camera
        SetCameraPriority(config.street1Cam, 10);
        SetCameraPriority(config.cornerOrbitCam, 20);

        // Wait for corner walk
        if (config.cornerWalkDuration > 0f)
        {
            yield return new WaitForSeconds(config.cornerWalkDuration);
            player.Movement.SetAutomatedMoveInput(null);
        }

        // Wait for remaining camera orbit blend time
        float remainingOrbitTime = Mathf.Max(0f, config.cameraOrbitDuration - config.cornerWalkDuration);
        if (remainingOrbitTime > 0f)
        {
            yield return new WaitForSeconds(remainingOrbitTime);
        }

        // 4. Seamless coordinate swap to Street 2
        Vector3 oldPos = player.transform.position;
        if (config.street2EntryAnchor != null)
        {
            player.Movement.TeleportTo(config.street2EntryAnchor.position);
            Vector3 delta = config.street2EntryAnchor.position - oldPos;
            if (config.street1Cam != null)
            {
                config.street1Cam.OnTargetObjectWarped(player.transform, delta);
            }
        }

        // 5. Return to gameplay camera (reuses street1Cam if street2Cam is not assigned)
        var activeGameplayCam = config.street2Cam != null ? config.street2Cam : config.street1Cam;
        SetCameraPriority(config.cornerOrbitCam, 5);
        SetCameraPriority(activeGameplayCam, 25);

        // Settle delay for camera stabilization
        if (config.street2SettleDelay > 0f)
        {
            yield return new WaitForSeconds(config.street2SettleDelay);
        }

        // 6. Restore player control
        player.SetMovePermission(true);
        player.SetJumpPermission(true);
        player.ReleaseCombatInput();

        _isTransitioning = false;
    }

    public void SetCameraPriority(CinemachineCamera cam, int priority)
    {
        if (cam == null) return;
        PrioritySettings p = cam.Priority;
        p.Value = priority;
        cam.Priority = p;
    }
}
