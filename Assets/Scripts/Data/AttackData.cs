using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/AttackData") ]
public class AttackData : CombatActionData
{
    

    [Header("Combo")]
    public AttackData nextCombo;

    [Header("Recovery")]
    [Tooltip("If true, this attack cannot be canceled even during recovery.")]
    public bool absoluteRecovery = false;

}
