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
    [SerializeField] private bool _toggleMode;

    [Header("Objects to actions")]
    [SerializeField] private AllBridgeController _allBridgeController;
    [SerializeField] private float _percentToObject;

    [Header("Condition controller")]
    [SerializeField] private List<InteractableTextConroller> _textController = new List<InteractableTextConroller>();

    private bool _isActive;
    private Vector3 _startRotation;
    private Vector3 _startPosition;

    private void Awake()
    {
        if (_animatedPart == null) _animatedPart = transform;

        _startRotation = _animatedPart.localEulerAngles;
        _startPosition = _animatedPart.localPosition;
    }

    public void Animate()
    {
        if (_toggleMode) _isActive = !_isActive;

        Vector3 targetRot = _isActive ? _activeRotation : _startRotation;
        Vector3 targetPos = _isActive ? _activePosition : _startPosition;

        if (!_isActive)
        {
            _allBridgeController.RaiseBridgeToPercent(-_percentToObject);
        } else
        {
            _allBridgeController.RaiseBridgeToPercent(_percentToObject);
        }

        foreach (var tc in _textController)
        {
            tc.SwapText();
        }
        Sequence seq = DOTween.Sequence();
        seq.Append(_animatedPart.DOLocalRotate(targetRot, _duration).SetEase(_ease));
        seq.Join(_animatedPart.DOLocalMove(targetPos, _duration).SetEase(_ease));
    }
}
