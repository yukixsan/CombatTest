using UnityEngine;

public enum CommandType
{
    Attack,
    Skill,
    Dash,
    Burst,
    Awaken,
}
public interface IPlayerCommand
{

    void Execute(PlayerCombat combat);
}
    

