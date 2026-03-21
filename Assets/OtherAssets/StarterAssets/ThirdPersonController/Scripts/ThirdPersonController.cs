using UnityEngine;
using Unity.Cinemachine;
using Zenject;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using Zenject;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        private const float StockMoveSpeed = 2.0f;
        private const float StockSprintSpeed = 5.335f;
        private const float StockJumpHeight = 1.2f;
        private const float StockGravity = -15.0f;
        private const float StockSize = 1.35f;
        private const float SprintSpeedMultiplier = StockSprintSpeed / StockMoveSpeed;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = StockMoveSpeed;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = StockSprintSpeed;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Header("Player Visual")]
        [Tooltip("Scale multiplier applied to the player root object")]
        public float Size = StockSize;
        [SerializeField] private Transform _sizeRoot;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private Vector3 _externalVelocity;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private bool _isSizeInitialized;
        private float _baseGroundedOffset;
        private float _baseGroundedRadius;
        private float _baseControllerStepOffset;
        private float _baseMoveSpeed;
        private float _baseSprintSpeed;
        private float _baseJumpHeight;
        private float _baseGravity;
        private float _lastAppliedSizeScale = 1f;
        private Transform _attachedScaleCamera;
        private Vector3 _attachedScaleCameraBaseLocalPosition;
        private Transform _attachedFollowCamera;
        private Vector3 _attachedFollowCameraBaseLocalPosition;
        private CinemachineThirdPersonFollow _thirdPersonFollow;
        private Vector3 _thirdPersonFollowBaseShoulderOffset;
        private float _thirdPersonFollowBaseVerticalArmLength;
        private float _thirdPersonFollowBaseCameraDistance;
        private float _thirdPersonFollowBaseCameraRadius;
        private Cinemachine3rdPersonFollow _legacyThirdPersonFollow;
        private Vector3 _legacyThirdPersonFollowBaseShoulderOffset;
        private float _legacyThirdPersonFollowBaseVerticalArmLength;
        private float _legacyThirdPersonFollowBaseCameraDistance;
        private float _legacyThirdPersonFollowBaseCameraRadius;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            Size = Size > 0.01f ? Size : StockSize;
            InitializeSizeData();
            ApplySize(Size);
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            bool sphereGrounded = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );
            bool controllerGrounded = _controller != null && _controller.isGrounded;
            Grounded = controllerGrounded || sphereGrounded;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (_controller == null || !_controller.enabled)
            {
                return;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            float animationTargetSpeed = _input.sprint ? _baseSprintSpeed : _baseMoveSpeed;

            if (_input.move == Vector2.zero)
            {
                targetSpeed = 0.0f;
                animationTargetSpeed = 0.0f;
            }

            float currentHorizontalSpeed =
                new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(
                    currentHorizontalSpeed,
                    targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate
                );

                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, animationTargetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation =
                    Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                    _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    RotationSmoothTime
                );

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection =
                Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            Vector3 playerHorizontalVelocity = Vector3.zero;

            if (_input.move != Vector2.zero)
            {
                playerHorizontalVelocity =
                    targetDirection.normalized * _speed;
            }

            Vector3 playerMove =
                playerHorizontalVelocity +
                Vector3.up * _verticalVelocity;

            Vector3 finalVelocity = playerMove + _externalVelocity;

            _controller.Move(finalVelocity * Time.deltaTime);

            _externalVelocity = Vector3.zero;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private float GetSizeScaleMultiplier()
        {
            return Mathf.Max(0.01f, Size / StockSize);
        }

        private void ApplyScaledGameplayParameters()
        {
            float sizeScale = GetSizeScaleMultiplier();
            float runtimeScale = _lastAppliedSizeScale > 0.0001f ? sizeScale / _lastAppliedSizeScale : 1f;

            MoveSpeed = _baseMoveSpeed * sizeScale;
            SprintSpeed = _baseSprintSpeed * sizeScale;
            JumpHeight = _baseJumpHeight * sizeScale;
            Gravity = _baseGravity * sizeScale;

            _speed *= runtimeScale;
            _verticalVelocity *= runtimeScale;
            _externalVelocity *= runtimeScale;

            _lastAppliedSizeScale = sizeScale;
        }


        public void SetExternalVelocity(Vector3 velocity)
        {
            _externalVelocity = velocity;
        }

        public void RestoreDefaultParameters()
        {
            _baseMoveSpeed = StockMoveSpeed;
            _baseSprintSpeed = StockSprintSpeed;
            _baseJumpHeight = StockJumpHeight;
            _baseGravity = StockGravity;
            ApplySize(StockSize);
        }

        private void InitializeSizeData()
        {
            if (_isSizeInitialized)
            {
                return;
            }

            if (_controller == null)
            {
                _controller = GetComponent<CharacterController>();
            }

            _sizeRoot = ResolveSizeRoot();
            float normalizedSize = Size > 0.01f ? Size : StockSize;
            _baseGroundedOffset = GroundedOffset / normalizedSize;
            _baseGroundedRadius = GroundedRadius / normalizedSize;
            _baseControllerStepOffset = _controller.stepOffset / normalizedSize;
            _baseMoveSpeed = MoveSpeed;
            _baseSprintSpeed = SprintSpeed;
            _baseJumpHeight = JumpHeight;
            _baseGravity = Gravity;
            InitializeAttachedScaleCamera();
            _isSizeInitialized = true;
        }

        private Transform ResolveSizeRoot()
        {
            if (_sizeRoot != null)
            {
                return _sizeRoot;
            }

            for (Transform current = transform; current != null; current = current.parent)
            {
                if (current.name == "MainPlayer")
                {
                    return current;
                }
            }

            return transform.root != null ? transform.root : transform;
        }

        private void InitializeAttachedScaleCamera()
        {
            _attachedScaleCamera = ResolveAttachedScaleCamera();
            if (_attachedScaleCamera != null && _sizeRoot != null)
            {
                _attachedScaleCameraBaseLocalPosition = _attachedScaleCamera.localPosition;
            }

            _attachedFollowCamera = ResolveAttachedFollowCamera();
            if (_attachedFollowCamera != null && _sizeRoot != null)
            {
                _attachedFollowCameraBaseLocalPosition = _attachedFollowCamera.localPosition;
            }

            if (_attachedFollowCamera != null)
            {
                _thirdPersonFollow = _attachedFollowCamera.GetComponent<CinemachineThirdPersonFollow>();
                if (_thirdPersonFollow != null)
                {
                    _thirdPersonFollowBaseShoulderOffset = _thirdPersonFollow.ShoulderOffset;
                    _thirdPersonFollowBaseVerticalArmLength = _thirdPersonFollow.VerticalArmLength;
                    _thirdPersonFollowBaseCameraDistance = _thirdPersonFollow.CameraDistance;
                    _thirdPersonFollowBaseCameraRadius = _thirdPersonFollow.AvoidObstacles.CameraRadius;
                }

                _legacyThirdPersonFollow = _attachedFollowCamera.GetComponent<Cinemachine3rdPersonFollow>();
                if (_legacyThirdPersonFollow != null)
                {
                    _legacyThirdPersonFollowBaseShoulderOffset = _legacyThirdPersonFollow.ShoulderOffset;
                    _legacyThirdPersonFollowBaseVerticalArmLength = _legacyThirdPersonFollow.VerticalArmLength;
                    _legacyThirdPersonFollowBaseCameraDistance = _legacyThirdPersonFollow.CameraDistance;
                    _legacyThirdPersonFollowBaseCameraRadius = _legacyThirdPersonFollow.CameraRadius;
                }
            }
        }

        private Transform ResolveAttachedScaleCamera()
        {
            if (_sizeRoot == null)
            {
                return null;
            }

            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            if (_mainCamera != null && _mainCamera.transform.IsChildOf(_sizeRoot))
            {
                return _mainCamera.transform;
            }

            foreach (Transform child in _sizeRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("MainCamera"))
                {
                    return child;
                }
            }

            return null;
        }

        private Transform ResolveAttachedFollowCamera()
        {
            if (_sizeRoot == null)
            {
                return null;
            }

            foreach (Transform child in _sizeRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "PlayerFollowCamera")
                {
                    return child;
                }
            }

            return null;
        }

        private void RestoreAttachedLocalPosition(Transform target, Vector3 baseLocalPosition)
        {
            if (target == null || _sizeRoot == null || !target.IsChildOf(_sizeRoot))
            {
                return;
            }

            float sizeScale = GetSizeScaleMultiplier();
            target.localPosition = baseLocalPosition / sizeScale;
        }

        private void ApplyScaledFollowCameraSettings()
        {
            float sizeScale = GetSizeScaleMultiplier();

            if (_thirdPersonFollow != null)
            {
                _thirdPersonFollow.ShoulderOffset = _thirdPersonFollowBaseShoulderOffset * sizeScale;
                _thirdPersonFollow.VerticalArmLength = _thirdPersonFollowBaseVerticalArmLength * sizeScale;
                _thirdPersonFollow.CameraDistance = Mathf.Max(0.2f, _thirdPersonFollowBaseCameraDistance * sizeScale);

                var obstacles = _thirdPersonFollow.AvoidObstacles;
                obstacles.CameraRadius = Mathf.Max(0.02f, _thirdPersonFollowBaseCameraRadius * sizeScale);
                _thirdPersonFollow.AvoidObstacles = obstacles;
            }

            if (_legacyThirdPersonFollow != null)
            {
                _legacyThirdPersonFollow.ShoulderOffset = _legacyThirdPersonFollowBaseShoulderOffset * sizeScale;
                _legacyThirdPersonFollow.VerticalArmLength = _legacyThirdPersonFollowBaseVerticalArmLength * sizeScale;
                _legacyThirdPersonFollow.CameraDistance = Mathf.Max(0.2f, _legacyThirdPersonFollowBaseCameraDistance * sizeScale);
                _legacyThirdPersonFollow.CameraRadius = Mathf.Max(0.02f, _legacyThirdPersonFollowBaseCameraRadius * sizeScale);
            }
        }

        private void RestoreAttachedScaleCamera()
        {
            RestoreAttachedLocalPosition(_attachedScaleCamera, _attachedScaleCameraBaseLocalPosition);
            RestoreAttachedLocalPosition(_attachedFollowCamera, _attachedFollowCameraBaseLocalPosition);
            ApplyScaledFollowCameraSettings();
        }

        private void ApplySize(float size)
        {
            Size = Mathf.Max(0.01f, size);
            InitializeSizeData();

            if (_controller == null)
            {
                _controller = GetComponent<CharacterController>();
            }

            Vector3 controllerAnchorBeforeScale = GetControllerAnchorPoint();

            if (_sizeRoot != null)
            {
                _sizeRoot.localScale = Vector3.one * Size;
            }

            GroundedOffset = _baseGroundedOffset * Size;
            GroundedRadius = Mathf.Max(0.01f, _baseGroundedRadius * Size);
            _controller.stepOffset = Mathf.Max(0f, _baseControllerStepOffset * Size);
            ApplyScaledGameplayParameters();
            RestoreAttachedScaleCamera();

            Vector3 controllerAnchorAfterScale = GetControllerAnchorPoint();
            Vector3 controllerAnchorOffset = controllerAnchorBeforeScale - controllerAnchorAfterScale;
            transform.position += controllerAnchorOffset;

            Physics.SyncTransforms();

            Physics.SyncTransforms();
            GroundedCheck();
        }

        private Vector3 GetControllerAnchorPoint()
        {
            if (_controller == null)
            {
                return transform.position;
            }

            Bounds controllerBounds = _controller.bounds;
            if (controllerBounds.size.sqrMagnitude <= Mathf.Epsilon)
            {
                return transform.position;
            }

            return new Vector3(
                controllerBounds.center.x,
                controllerBounds.min.y,
                controllerBounds.center.z
            );
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        //Zenject Injection Constructor
        private SignalBus _signalBus;

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
            _signalBus.Subscribe<PlayerParamChangedSignal>(OnParamChanged);
        }

        private void OnDestroy()
        {
            _signalBus?.Unsubscribe<PlayerParamChangedSignal>(OnParamChanged);
        }

        private void OnParamChanged(PlayerParamChangedSignal signal)
        {
            switch (signal.ParamType)
            {
                case PlayerParamType.JumpHeight:
                    _baseJumpHeight = signal.Value / GetSizeScaleMultiplier();
                    ApplyScaledGameplayParameters();
                    break;

                case PlayerParamType.MoveSpeed:
                    _baseMoveSpeed = signal.Value / GetSizeScaleMultiplier();
                    _baseSprintSpeed = _baseMoveSpeed * SprintSpeedMultiplier;
                    ApplyScaledGameplayParameters();
                    break;

                case PlayerParamType.Gravity:
                    _baseGravity = Mathf.Min(signal.Value / GetSizeScaleMultiplier(), -0.01f);
                    ApplyScaledGameplayParameters();
                    break;

                case PlayerParamType.Size:
                    ApplySize(signal.Value);
                    break;
            }
        }
    }
}
