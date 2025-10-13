using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class VCamController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minZ = 471f;
    [SerializeField] private float maxZ = 529f;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference cameraMoveAction;

    private float inputValue;

    private void OnEnable()
    {
        if (cameraMoveAction != null)
        {
            cameraMoveAction.action.Enable();
            cameraMoveAction.action.performed += OnCameraMove;
            cameraMoveAction.action.canceled += OnCameraMove;
        }
    }

    private void OnDisable()
    {
        if (cameraMoveAction != null)
        {
            cameraMoveAction.action.performed -= OnCameraMove;
            cameraMoveAction.action.canceled -= OnCameraMove;
            cameraMoveAction.action.Disable();
        }
    }

    private void OnCameraMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        inputValue = input.x;
    }

    private void Update()
    {
        float newZ = transform.position.z + inputValue * moveSpeed * Time.deltaTime;
        newZ = Mathf.Clamp(newZ, minZ, maxZ);

        Vector3 newPos = new Vector3(transform.position.x, transform.position.y, newZ);
        transform.position = Vector3.Lerp(transform.position, newPos, 0.15f);
    }
}
