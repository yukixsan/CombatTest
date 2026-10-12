using UnityEngine;

public enum CommandType
{
    Attack,
    Skill,
    Dash,
    Burst,
}
public interface IPlayerCommand
{

    void Execute(PlayerCombat combat);
}
    

