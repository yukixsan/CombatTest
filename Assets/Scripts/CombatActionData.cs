using System.Collections.Generic;
using UnityEngine;
public enum DirectionVariant
{
    Neutral = 0,
    Up = 1,
    Down = -1
}
[System.Serializable]
public struct AttackPhaseVFX
{
    public GameObject prefab;
    public Vector3 localOffset; // relative to _model
}
public class CombatActionData : ScriptableObject
{
    [Header("General")]
    public string Name;

    [Header("Reference")]
    public AnimationClip animationClip;
    public int VFXindex = 0;

    [Header("Weapon Visual Setting")]
    public bool useHandWeapon = false;

    [Header("Properties")]
    public float damage = 10f;
    public float knockbackForce = 5f;
    public float launchForce;
    public int launchDir;
    public float hitstunDuration;
    public bool Airborne;
    public int airLimit = 0;

    [Header("Cancel Windows")]
    public bool canBeCancelledWindup = true;
    public bool canBeCancelledActive = false;
    public bool canBeCancelledRecovery = true;
    [Header("VFXs")]
    public AttackPhaseVFX windupVFX;
    public AttackPhaseVFX activeVFX;
    public AttackPhaseVFX recoveryVFX;
}
