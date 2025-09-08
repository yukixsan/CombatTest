using System.Collections.Generic;
using UnityEngine;

public class CombatActionData : ScriptableObject
{
    [Header("General")]
    public string Name;

    [Header("Reference")]
    public AnimationClip animationClip;

    [Header("Phase Durations")]
    public float windup = 0.2f;
    public float active = 0.2f;
    public float recovery = 0.3f;

    [Header("Properties")]
    public float damage = 10f;
    public float knockbackForce = 5f;
    public float launchForce;
    public int launchDir;
    public float hitstunDuration = 0.3f;

    [Header("Cancel Windows")]
    public bool canBeCancelledWindup = true;
    public bool canBeCancelledActive = false;
    public bool canBeCancelledRecovery = true;
}
