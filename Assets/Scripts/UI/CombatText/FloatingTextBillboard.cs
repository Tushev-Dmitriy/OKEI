using Unity.Cinemachine;
using UnityEngine;

public class FloatingTextBillboard : MonoBehaviour
{
    [SerializeField] private CinemachineCamera targetCamera;

    private void LateUpdate()
    {
        Transform cameraTransform = targetCamera.transform;
        Vector3 direction = transform.position - cameraTransform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction, cameraTransform.up);
        }
    }
}
