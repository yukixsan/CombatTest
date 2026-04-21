using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    [SerializeField] private GameObject hitBoxObject;

    public void Active()
    {
        hitBoxObject.GetComponent<BoxCollider>().enabled = true;
    }

    public void Deactive()
    {
        hitBoxObject.GetComponent<BoxCollider>().enabled = false;
    }
}
