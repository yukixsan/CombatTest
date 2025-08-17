using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewSkill",menuName = "Combat/Skill Data")]

public class PlayerSkillData : CombatActionData
{
    [Header("Skill settings")]
    public float cooldown;
    public GameObject skillPrefab;
    public Vector3 spawnOffset;
    public bool attachToPlayer;
    
}
