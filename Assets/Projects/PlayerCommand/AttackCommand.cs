using UnityEngine;

public class AttackCommand : IPlayerCommand
{
     private int index;

    public AttackCommand(int index)
    {
        this.index = index;
    }

    public void Execute(PlayerCombat combat)
    {
    }
}
