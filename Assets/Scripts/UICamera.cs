using UnityEngine;
public class UICamera : MonoBehaviour
{
    private Camera targetCamera;
    void Awake()
    {
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;
        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
                         targetCamera.transform.rotation * Vector3.up);
    }
}