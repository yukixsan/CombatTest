using UnityEngine;
using System.Collections;

public class PlayerLocomotionFX : MonoBehaviour
{
    [Header("Dedicated Particle Systems (one instance each, reused via Play())")]
    [SerializeField] private ParticleSystem jumpDustPS;
    [SerializeField] private ParticleSystem landDustPS;
    [SerializeField] private ParticleSystem dashDustPS;
    [SerializeField] private ParticleSystem runDustPS; // looping - controlled via Play()/Stop()

    [Header("Run settings")]
    private Coroutine _pendingStopRoutine;
    private bool _runDustActive; // our own source of truth, not runDustPS.isPlaying
    [SerializeField] private float runDustStopDelay = 0.15f; 
    public void PlayJumpDust(Vector3 position)
    {
        if (jumpDustPS == null) return;
        jumpDustPS.transform.position = position;
        jumpDustPS.Play();
    }

    public void PlayLandDust()
    {
        if (landDustPS == null) return;
        landDustPS.Play();
    }

    public void PlayDashDust()
    {
        if (dashDustPS == null) return;
        dashDustPS.Play();
    }

    public void StartRunDust()
    {
        if (runDustPS == null) return;

        if (_pendingStopRoutine != null)
        {
            StopCoroutine(_pendingStopRoutine);
            _pendingStopRoutine = null;
        }

        if (!_runDustActive)
        {
            runDustPS.Play();
            _runDustActive = true;
        }
    }

    public void StopRunDust()
    {
        if (runDustPS == null) return;
        if (_pendingStopRoutine != null) return; // already scheduled

        _pendingStopRoutine = StartCoroutine(DelayedStopRunDust());
    }
    private IEnumerator DelayedStopRunDust()
    {
        yield return new WaitForSeconds(runDustStopDelay);
        runDustPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _runDustActive = false;
        _pendingStopRoutine = null;
    }
}
