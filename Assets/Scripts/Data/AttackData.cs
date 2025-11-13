using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/AttackData") ]
public class AttackData : CombatActionData
{
    

    [Header("Combo")]
    public AttackData nextCombo;

    [Header("Mapping")]
    [Tooltip("Which combo slot this attack fills (1,2,3 etc).")]
    public int comboIndex = 1;

    [Tooltip("Direction variant required for this attack (Neutral / Up / Down)")]
    public DirectionVariant directionVariant = DirectionVariant.Neutral;
    
    [Header("Recovery")]
    [Tooltip("If true, this attack cannot be canceled even during recovery.")]
    public bool absoluteRecovery = false;

}
