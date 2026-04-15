using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    [SerializeField] private GameObject hitBoxObject;

    public void Active()
    {
        hitBoxObject.SetActive(true);
    }

    public void Deactive()
    {
        hitBoxObject.SetActive(false);
    }
}
