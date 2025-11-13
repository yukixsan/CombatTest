using UnityEngine;

public enum CommandType
{
     Attack,
    Skill,
    Dash
}
public interface IPlayerCommand
{

    void Execute(PlayerCombat combat);
}
    

