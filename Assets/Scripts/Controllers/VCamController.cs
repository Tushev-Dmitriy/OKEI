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
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference cameraMoveAction;

    [Header("Movement Options")]
    [SerializeField] private bool allowWS = false;

    private Vector2 inputValue;

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
        inputValue = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        float moveZ = inputValue.x * moveSpeed * Time.deltaTime;
        float moveX = allowWS ? -inputValue.y * moveSpeed * Time.deltaTime : 0f;

        Vector3 move = new Vector3(moveX, 0, moveZ);
        Vector3 targetPos = transform.position + move;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.z = Mathf.Clamp(targetPos.z, minZ, maxZ);

        transform.position = Vector3.Lerp(transform.position, targetPos, 0.15f);
    }
}