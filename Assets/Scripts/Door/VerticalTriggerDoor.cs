using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class VerticalTriggerDoor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _doorTransform;

    [Header("Trigger")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _closeOnExit;

    [Header("Animation")]
    [SerializeField] private float _openDistance = 3f;
    [SerializeField] private float _duration = 1f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private bool _startOpened;

    private Vector3 _closedLocalPosition;
    private Vector3 _openedLocalPosition;
    private Tween _moveTween;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (_doorTransform == null)
        {
            _doorTransform = transform;
        }

        ValidateConfiguration();
        _closedLocalPosition = _doorTransform.localPosition;
        _openedLocalPosition = _closedLocalPosition + Vector3.down * _openDistance;
        _isOpen = _startOpened;
        ApplyStateInstant();
    }

    private void Reset()
    {
        var trigger = GetComponent<Collider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_playerTag))
        {
            return;
        }

        OpenDoor();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_closeOnExit || !other.CompareTag(_playerTag))
        {
            return;
        }

        CloseDoor();
    }

    public void OpenDoor()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        AnimateDoor(_openedLocalPosition);
    }

    public void CloseDoor()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        AnimateDoor(_closedLocalPosition);
    }

    private void AnimateDoor(Vector3 targetLocalPosition)
    {
        if (_doorTransform == null)
        {
            return;
        }

        _moveTween?.Kill();
        _moveTween = _doorTransform
            .DOLocalMove(targetLocalPosition, _duration)
            .SetEase(_ease);
    }

    private void ValidateConfiguration()
    {
        if (_doorTransform == null)
        {
            Debug.LogWarning($"{nameof(VerticalTriggerDoor)} on {name} has no door transform assigned.", this);
            return;
        }

        if (_doorTransform.gameObject.isStatic)
        {
            Debug.LogWarning(
                $"{nameof(VerticalTriggerDoor)} on {name}: {_doorTransform.name} is marked Static. " +
                "Moving doors must not be Static, otherwise the transform can change while the mesh stays in place.",
                _doorTransform);
        }

        var renderers = _doorTransform.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (!renderer.isPartOfStaticBatch)
            {
                continue;
            }

            Debug.LogWarning(
                $"{nameof(VerticalTriggerDoor)} on {name}: renderer {renderer.name} is in a static batch. " +
                "Disable Static on the door object and its visual children.",
                renderer);
            break;
        }
    }

    private void ApplyStateInstant()
    {
        if (_doorTransform == null)
        {
            return;
        }

        _doorTransform.localPosition = _isOpen ? _openedLocalPosition : _closedLocalPosition;
    }
}
