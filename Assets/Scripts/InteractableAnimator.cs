using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class InteractableAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Transform _animatedPart;
    [SerializeField] private Vector3 _activeRotation;
    [SerializeField] private Vector3 _activePosition;
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private bool _toggleMode = true;

    [Header("Action Settings")]
    [SerializeField] private AllBridgeController _allBridgeController;
    [SerializeField] private float _percentStep = 10f;
    [SerializeField] private List<InteractableTextConroller> _textControllers = new();

    private bool _isActive;
    private float _currentPercent;
    private Vector3 _startRotation;
    private Vector3 _startPosition;

    private void Awake()
    {
        if (!_animatedPart)
            _animatedPart = transform;

        _startRotation = _animatedPart.localEulerAngles;
        _startPosition = _animatedPart.localPosition;
    }

    public void Animate()
    {
        foreach (var tc in _textControllers)
        {
            if (tc.useCounter)
                tc.IncrementCounter();
            else
                tc.SwapText();
        }

        if (_toggleMode)
        {
            _isActive = !_isActive;
            _currentPercent = _isActive ? 100f : 0f;
            _allBridgeController.RaiseBridgeToPercent(_currentPercent);

            AnimatePart(_isActive);
        }
        else
        {
            _currentPercent = Mathf.Clamp(_currentPercent + _percentStep, 0f, 100f);
            _allBridgeController.RaiseBridgeToPercent(_currentPercent);

            Sequence seq = DOTween.Sequence();
            seq.Append(AnimatePart(true));
            seq.AppendInterval(_duration + 0.05f);
            seq.Append(AnimatePart(false));
        }
    }

    private Tween AnimatePart(bool activate)
    {
        Vector3 targetRot = activate ? _activeRotation : _startRotation;
        Vector3 targetPos = activate ? _activePosition : _startPosition;

        return DOTween.Sequence()
            .Join(_animatedPart.DOLocalRotate(targetRot, _duration).SetEase(_ease))
            .Join(_animatedPart.DOLocalMove(targetPos, _duration).SetEase(_ease));
    }
}
