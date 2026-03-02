using DG.Tweening;
using UnityEngine;

public class LockControlInteractable : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Transform animatedPart;
    [SerializeField] private Vector3 activeRotation;
    [SerializeField] private Vector3 activePosition;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    [Header("Control")]
    [SerializeField] private LockControlAction controlAction;
    [SerializeField] private bool toggleVisualState = true;
    [SerializeField] private bool startActive;

    private LockControlSystem _lockSystem;
    private Vector3 _startRotation;
    private Vector3 _startPosition;
    private bool _isActive;

    public void Setup(LockControlSystem lockSystem, LockControlAction action, bool toggleVisual)
    {
        _lockSystem = lockSystem;
        controlAction = action;
        toggleVisualState = toggleVisual;
        _isActive = startActive;
        ApplyVisualInstant(_isActive);
    }

    private void Awake()
    {
        if (animatedPart == null)
            animatedPart = transform;

        _startRotation = animatedPart.localEulerAngles;
        _startPosition = animatedPart.localPosition;

        // Runtime-added controls use default zero values, so keep animation relative to the current pose.
        if (activeRotation == Vector3.zero)
            activeRotation = _startRotation + new Vector3(0f, 0f, -22f);

        if (activePosition == Vector3.zero)
            activePosition = _startPosition + new Vector3(0f, -0.025f, 0f);

        _isActive = startActive;
        ApplyVisualInstant(_isActive);
    }

    public void Interact()
    {
        if (_lockSystem == null || !_lockSystem.CanInteract)
            return;

        if (!_lockSystem.ProcessControl(controlAction))
            return;

        if (toggleVisualState)
        {
            _isActive = !_isActive;
            AnimateToState(_isActive);
            return;
        }

        DOTween.Sequence()
            .Append(AnimateToState(true))
            .AppendInterval(duration * 0.25f)
            .Append(AnimateToState(false));
    }

    private Tween AnimateToState(bool active)
    {
        Vector3 targetRot = active ? activeRotation : _startRotation;
        Vector3 targetPos = active ? activePosition : _startPosition;

        return DOTween.Sequence()
            .Join(animatedPart.DOLocalRotate(targetRot, duration).SetEase(ease))
            .Join(animatedPart.DOLocalMove(targetPos, duration).SetEase(ease));
    }

    private void ApplyVisualInstant(bool active)
    {
        if (animatedPart == null)
            return;

        animatedPart.localEulerAngles = active ? activeRotation : _startRotation;
        animatedPart.localPosition = active ? activePosition : _startPosition;
    }
}
