using UnityEngine;

public class CommandInterpreter : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;


    private Vector2 lastDirection = Vector2.zero;

    public void Receive(BufferedCommand cmd)
    {
        switch (cmd.type)
        {
            case CommandType.Attack:
                playerCombat.ExecuteAttack(lastDirection);
                break;

            case CommandType.Skill:
                playerCombat.ExecuteSkill(cmd.skillIndex);
                break;

            case CommandType.Dash:
                playerCombat.ExecuteDash();
                break;
        }
    }


    public void UpdateDirection(Vector2 dir)
    {
        lastDirection = dir;
    }
}
