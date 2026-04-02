using UnityEngine;

public class FloatingTextBillboard : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void LateUpdate()
    {
        Transform cameraTransform = targetCamera != null ? targetCamera.transform : null;
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
            return;

        Vector3 direction = cameraTransform.position - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction, cameraTransform.up);
        }
    }
}
