using UnityEngine;
using Zenject;

public class PlayerController : MonoBehaviour, IPlayer
{
    private Rigidbody _rb;
    private PlayerConfig _config;
    private MainInputSystem _inputActions;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpInput;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float mouseSensitivity = 3f;

    private float _cameraPitch = 0f;

    [Inject]
    public void Construct(PlayerConfig config)
    {
        _config = config;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _inputActions = new MainInputSystem();
        _inputActions.Player.Enable();

        _inputActions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputActions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

        _inputActions.Player.Jump.performed += ctx => _jumpInput = true;

        _inputActions.Player.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        _inputActions.Player.Look.canceled += ctx => _lookInput = Vector2.zero;
    }

    private void Update()
    {
        HandleLook();
    }

    private void FixedUpdate()
    {
        Move(_moveInput);
        if (_jumpInput)
        {
            Jump();
            _jumpInput = false;
        }
    }

    private void HandleLook()
    {
        transform.Rotate(Vector3.up * _lookInput.x * mouseSensitivity * Time.deltaTime);

        _cameraPitch -= _lookInput.y * mouseSensitivity * Time.deltaTime;
        _cameraPitch = Mathf.Clamp(_cameraPitch, -80f, 80f);

        cameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    public void Move(Vector2 direction)
    {
        Vector3 move = (transform.forward * direction.y + transform.right * direction.x) * _config.moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + move);
    }

    public void Jump()
    {
        _rb.AddForce(Vector3.up * _config.jumpForce, ForceMode.Impulse);
    }
}
