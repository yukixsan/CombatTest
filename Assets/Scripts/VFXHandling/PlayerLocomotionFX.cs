using UnityEngine;

public class PlayerLocomotionFX : MonoBehaviour
{
    [Header("Dedicated Particle Systems (one instance each, reused via Play())")]
    [SerializeField] private ParticleSystem jumpDustPS;
    [SerializeField] private ParticleSystem landDustPS;
    [SerializeField] private ParticleSystem dashDustPS;
    [SerializeField] private ParticleSystem runDustPS; // looping - controlled via Play()/Stop()

    public void PlayJumpDust()
    {
        if (jumpDustPS == null) return;
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
        if (!runDustPS.isPlaying)
            runDustPS.Play();
    }

    public void StopRunDust()
    {
        if (runDustPS == null) return;
        if (runDustPS.isPlaying)
            runDustPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
