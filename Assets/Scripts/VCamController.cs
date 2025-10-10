using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class VCamController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxOffset = 10f;
    [SerializeField] private float smoothTime = 0.3f;
    
    [Header("Input Settings")]
    [SerializeField] private float inputSensitivity = 1f;
    [SerializeField] private bool invertHorizontal = false;
    
    private InputAction moveAction;
    private float currentOffset = 0f;
    private float velocity = 0f;
    private Vector3 initialPosition;
    private CinemachinePositionComposer positionComposer;
    
    private void Awake()
    {            
        if (vcam == null)
        {
            Debug.LogError("VCamController: No CinemachineCamera component found!");
            return;
        }
        
        initialPosition = transform.position;
    }
    
    private void Start()
    {
        SetupInputActions();
    }
    
    private void SetupInputActions()
    {
        moveAction = new InputAction("CameraMove");

        moveAction.AddCompositeBinding("Axis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d")
            .With("Negative", "<Gamepad>/leftStick/x")
            .With("Positive", "<Gamepad>/leftStick/x")
            .With("Negative", "<Touchscreen>/primaryTouch/position")
            .With("Positive", "<Touchscreen>/primaryTouch/position");
        
        moveAction.Enable();
    }
    
    private void Update()
    {
        if (vcam == null) return;
        
        float input = moveAction.ReadValue<float>();

        input *= inputSensitivity;
        if (invertHorizontal) input = -input;
        
        float targetOffset = input * maxOffset;

        currentOffset = Mathf.SmoothDamp(currentOffset, targetOffset, ref velocity, smoothTime);

        ApplyCameraOffset();
    }
    
    private void ApplyCameraOffset()
    {
        Vector3 newPosition = initialPosition + Vector3.right * currentOffset;
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * moveSpeed);
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void SetMaxOffset(float offset)
    {
        maxOffset = offset;
    }
    
    public void ResetCameraPosition()
    {
        currentOffset = 0f;
        velocity = 0f;
        if (positionComposer != null)
        {
            var targetOffset = positionComposer.TargetOffset;
            targetOffset.x = 0f;
            positionComposer.TargetOffset = targetOffset;
        }
        transform.position = initialPosition;
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.Enable();
    }
    
    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }
    
    private void OnDestroy()
    {
        if (moveAction != null)
        {
            moveAction.Dispose();
        }
    }
    
    public void MoveLeft()
    {
        currentOffset = -maxOffset;
    }
    
    public void MoveRight()
    {
        currentOffset = maxOffset;
    }
    
    public void StopMoving()
    {
        currentOffset = 0f;
    }
}