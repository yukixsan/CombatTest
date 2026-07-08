using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    [SerializeField] private GameObject hitBoxObject;
    public HitboxPayload Payload { get; private set; }
    public bool HasPayload { get; private set; }

    public void Active()
    {
        hitBoxObject.GetComponent<BoxCollider>().enabled = true;
    }

    public void Deactive()
    {
        hitBoxObject.GetComponent<BoxCollider>().enabled = false;
        HasPayload = false;
    }
    public void SetPayload(HitboxPayload payload)
    {
        Payload = payload;
        HasPayload = true;
    }

}
