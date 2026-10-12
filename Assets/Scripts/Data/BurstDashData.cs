using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BurstDashData", menuName = "Combat/Burst Dash Data")]
public class BurstDashData : ScriptableObject
{
    [System.Serializable]
    public struct DashProfile
    {
        public float speed;
        public float duration;

        public DashProfile(float speed, float duration)
        {
            this.speed = speed;
            this.duration = duration;
        }
    }

    [Header("Directional Profiles")]
    public DashProfile horizontalProfile = new DashProfile(30f, 0.2f);
    public DashProfile verticalProfile = new DashProfile(50f, 0.08f);
    public DashProfile diagonalProfile = new DashProfile(35f, 0.15f);

    [Header("Animations")]
    public AnimationClip groundNeutralClip;
    public AnimationClip airNeutralClip;
    public AnimationClip groundDashClip;
    public AnimationClip airDashClip;
    public AnimationClip dashUpClip;
    public AnimationClip dashDownClip;
    public AnimationClip dashSlantForwardClip;
    public AnimationClip dashSlantDownwardClip;

    [Header("Cancel Windows")]
    public bool canBeCancelledWindup = false;
    public bool canBeCancelledActive = false;
    public bool canBeCancelledRecovery = true;

    [System.Serializable]
    public struct MarkPayloadConfig
    {
        public float knockbackForce;
        public float launchForce;
        public int launchDir; // 1 = up, -1 = down, 0 = none
        public float damageMultiplier;

        public MarkPayloadConfig(float knockback, float launch, int dir, float damageMult = 1f)
        {
            knockbackForce = knockback;
            launchForce = launch;
            launchDir = dir;
            damageMultiplier = damageMult;
        }
    }

    [Header("Mark Spawning & Detonation")]
    public GameObject markPrefab;
    public float markLifetime = 2.5f;
    public float markBaseDamage = 15f;
    public float markHitstopDuration = 0.08f;
    public int markHitVFXIndex = 0;
    public GameObject detonationVFXPrefab;
    public AudioClip detonationSFX;
    public float detonationSFXVolume = 1f;

    [Header("Directional Mark Payloads")]
    public MarkPayloadConfig neutralPayload = new MarkPayloadConfig(0f, 0f, 0, 1f);
    public MarkPayloadConfig horizontalPayload = new MarkPayloadConfig(15f, 0f, 0, 1f);
    public MarkPayloadConfig upPayload = new MarkPayloadConfig(0f, 18f, 1, 1f);
    public MarkPayloadConfig downPayload = new MarkPayloadConfig(0f, 18f, -1, 1f);

    public BurstDirection ResolveDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.1f)
            return BurstDirection.Neutral;

        bool hasHorizontal = Mathf.Abs(dir.x) > 0.2f;
        if (hasHorizontal && dir.y > 0.2f) return BurstDirection.SlantUp;
        if (hasHorizontal && dir.y < -0.2f) return BurstDirection.SlantDown;
        if (!hasHorizontal && dir.y > 0.2f) return BurstDirection.Up;
        if (!hasHorizontal && dir.y < -0.2f) return BurstDirection.Down;

        return BurstDirection.Horizontal;
    }

    public AnimationClip GetAnimationClip(BurstDirection dir, bool isAirborne)
    {
        AnimationClip clip = null;

        switch (dir)
        {
            case BurstDirection.Neutral:
                clip = isAirborne ? airNeutralClip : groundNeutralClip;
                if (clip == null)
                    clip = isAirborne ? groundNeutralClip : airNeutralClip;
                break;

            case BurstDirection.SlantUp:
                clip = dashSlantForwardClip;
                break;

            case BurstDirection.SlantDown:
                clip = dashSlantDownwardClip;
                break;

            case BurstDirection.Up:
                clip = dashUpClip;
                break;

            case BurstDirection.Down:
                clip = dashDownClip;
                break;

            case BurstDirection.Horizontal:
            default:
                clip = isAirborne ? airDashClip : groundDashClip;
                break;
        }

        // Fallbacks if specific directional clip is unassigned
        if (clip == null)
            clip = isAirborne ? airDashClip : groundDashClip;
        if (clip == null)
            clip = isAirborne ? groundDashClip : airDashClip;

        return clip;
    }

    public DashProfile GetProfile(BurstDirection dir)
    {
        DashProfile profile;
        switch (dir)
        {
            case BurstDirection.SlantUp:
            case BurstDirection.SlantDown:
                profile = diagonalProfile;
                break;

            case BurstDirection.Up:
            case BurstDirection.Down:
                profile = verticalProfile;
                break;

            case BurstDirection.Horizontal:
            case BurstDirection.Neutral:
            default:
                profile = horizontalProfile;
                break;
        }

        // Fallbacks if zero/unassigned in Inspector
        if (profile.speed <= 0f)
            profile.speed = horizontalProfile.speed > 0f ? horizontalProfile.speed : 30f;
        if (profile.duration <= 0f)
            profile.duration = horizontalProfile.duration > 0f ? horizontalProfile.duration : 0.2f;

        return profile;
    }

    public DashProfile GetProfile(bool hasHorizontal, bool hasVertical)
    {
        if (hasHorizontal && hasVertical) return GetProfile(BurstDirection.SlantUp);
        if (hasVertical) return GetProfile(BurstDirection.Up);
        return GetProfile(BurstDirection.Horizontal);
    }

    public HitboxPayload CreateMarkPayload(BurstDirection dir, Transform attackerTransform)
    {
        MarkPayloadConfig config;
        switch (dir)
        {
            case BurstDirection.Up:
            case BurstDirection.SlantUp:
                config = upPayload;
                break;

            case BurstDirection.Down:
            case BurstDirection.SlantDown:
                config = downPayload;
                break;

            case BurstDirection.Horizontal:
                config = horizontalPayload;
                break;

            case BurstDirection.Neutral:
            default:
                config = neutralPayload;
                break;
        }

        float mult = config.damageMultiplier > 0f ? config.damageMultiplier : 1f;
        float finalDamage = markBaseDamage * mult;

        return new HitboxPayload(
            finalDamage,
            0f, // Mark has 0 attacker armor
            config.knockbackForce,
            config.launchForce,
            config.launchDir,
            markHitstopDuration,
            attackerTransform,
            markHitVFXIndex
        );
    }
}

public enum BurstDirection
{
    Neutral,
    Horizontal,
    Up,
    Down,
    SlantUp,
    SlantDown
}

