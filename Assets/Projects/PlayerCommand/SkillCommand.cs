using UnityEngine;

public class SkillCommand : IPlayerCommand
{
    public readonly int SkillIndex;
    public readonly Vector2 Direction;

    public SkillCommand(int index, Vector2 dir)
    {
        SkillIndex = index;
        Direction = dir;
    }

    public void Execute(PlayerCombat combat)
    {
    }
}
