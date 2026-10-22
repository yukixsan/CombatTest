using UnityEngine;

[CreateAssetMenu(fileName = "AwakeningData", menuName = "Combat/Awakening Data")]
public class AwakeningData : ScriptableObject
{
    [Header("Activation Animations")]
    public AnimationClip groundActivationClip;
    public AnimationClip airActivationClip;

    [Header("Cancel Windows")]
    public bool canBeCancelledWindup = false;
    public bool canBeCancelledActive = false;
    public bool canBeCancelledRecovery = true;

    [Header("VFX & SFX")]
    public AttackPhaseVFX activationVFX;

    [Header("Buff Parameters")]
    public float duration = 15f;
    public float damageMultiplier = 1.3f;
    public float moveSpeedMultiplier = 1.25f;
    public float animSpeedMultiplier = 1.15f;

    [Header("Awakened Skill Overrides")]
    [Tooltip("Replaces groundSkills by slot index (0..3). Leave slot empty/null to keep normal skill.")]
    public System.Collections.Generic.List<PlayerSkillData> awakenedGroundSkills = new();

    [Tooltip("Replaces airSkills by slot index (0..3). Leave slot empty/null to keep normal skill.")]
    public System.Collections.Generic.List<PlayerSkillData> awakenedAirSkills = new();

    [System.Serializable]
    public struct VFXOverrideEntry
    {
        public GameObject baseVFXPrefab;
        public GameObject awakenedVFXPrefab;
    }

    [Header("Visual Theme - VFX Overrides (Base -> Awakened Variant)")]
    public System.Collections.Generic.List<VFXOverrideEntry> vfxOverrides = new();

    private System.Collections.Generic.Dictionary<GameObject, GameObject> _vfxLookup;

    private void OnEnable() => RebuildVFXLookup();

    public void RebuildVFXLookup()
    {
        _vfxLookup = new System.Collections.Generic.Dictionary<GameObject, GameObject>();
        foreach (var entry in vfxOverrides)
        {
            if (entry.baseVFXPrefab != null && entry.awakenedVFXPrefab != null)
                _vfxLookup[entry.baseVFXPrefab] = entry.awakenedVFXPrefab;
        }
    }

    public bool TryGetVFXOverride(GameObject basePrefab, out GameObject awakenedPrefab)
    {
        if (basePrefab == null)
        {
            awakenedPrefab = null;
            return false;
        }

        if (_vfxLookup == null) RebuildVFXLookup();
        return _vfxLookup.TryGetValue(basePrefab, out awakenedPrefab);
    }

    public AnimationClip GetActivationClip(bool isAirborne)
    {
        AnimationClip clip = isAirborne ? airActivationClip : groundActivationClip;
        if (clip == null)
            clip = isAirborne ? groundActivationClip : airActivationClip;
        return clip;
    }
}

