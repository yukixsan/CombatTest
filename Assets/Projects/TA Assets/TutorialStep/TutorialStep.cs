using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TutorialStep 
{
    [Tooltip("Shown in the tutorial UI, e.g. 'Perform the 3-hit ground combo'")]
    public string displayText;

    [Tooltip("Ordered sequence of moves required to complete this step. " +
            "Length 1 for a single input, length 3+ for a combo chain.")]
    public List<CombatActionData> requiredSequence = new();

    [Tooltip("How many successful completions of the full sequence are needed " +
            "before this step is considered done.")]
    public int requiredCompletions = 1;

    [HideInInspector] public int currentSequenceProgress;
    [HideInInspector] public int completionCount;

    public bool IsComplete => completionCount >= requiredCompletions;
}
