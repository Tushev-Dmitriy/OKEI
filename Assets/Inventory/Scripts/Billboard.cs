using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _camera;

    private void Start()
    {
        _camera = ResolveCamera();
    }

    private void LateUpdate()
    {
        if (_camera == null || !_camera.isActiveAndEnabled)
        {
            _camera = ResolveCamera();
            if (_camera == null)
                return;
        }

        transform.LookAt(transform.position + _camera.transform.rotation * Vector3.forward,
            _camera.transform.rotation * Vector3.up);
    }

    private static Camera ResolveCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        Camera best = null;
        float bestDepth = float.MinValue;

        foreach (Camera cam in cameras)
        {
            if (cam == null || !cam.isActiveAndEnabled)
                continue;

            if (cam.depth > bestDepth)
            {
                bestDepth = cam.depth;
                best = cam;
            }
        }

        return best;
    }
}
