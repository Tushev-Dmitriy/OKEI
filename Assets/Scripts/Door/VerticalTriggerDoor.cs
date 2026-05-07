using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField, Min(0f)] private float _minSoundMovementY = 2f;
    [SerializeField, Min(0f)] private float _soundCooldown = 0.35f;

    private Vector3 _closedLocalPosition;
    private Vector3 _openedLocalPosition;
    private Tween _moveTween;
    private bool _isOpen;
    private readonly HashSet<int> _activeActors = new();
    private Collider _triggerCollider;
    private float _lastSoundTime = -999f;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (_doorTransform == null)
        {
            _doorTransform = transform;
        }

        ValidateConfiguration();
        _triggerCollider = GetComponent<Collider>();
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

        int actorId = ResolveActorId(other);
        if (!_activeActors.Add(actorId))
        {
            return;
        }

        if (_activeActors.Count == 1)
        {
            OpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_closeOnExit || !other.CompareTag(_playerTag))
        {
            return;
        }

        int actorId = ResolveActorId(other);
        _activeActors.Remove(actorId);

        if (_activeActors.Count == 0)
        {
            CloseDoor();
        }
    }

    public void OpenDoor()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        PlayDoorSound(open: true, _openedLocalPosition);
        AnimateDoor(_openedLocalPosition);

        if (!_closeOnExit && _triggerCollider != null)
        {
            _triggerCollider.enabled = false;
        }
    }

    public void CloseDoor()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        PlayDoorSound(open: false, _closedLocalPosition);
        AnimateDoor(_closedLocalPosition);
    }

    private static string GetDoorCueId(bool open)
    {
        bool isLevel3 = SceneManager.GetActiveScene().name == "Level3";
        if (isLevel3)
            return open ? AudioCueIds.Level3DoorOpenLight : AudioCueIds.Level3DoorCloseLight;

        return open ? AudioCueIds.DoorOpenHeavy : AudioCueIds.DoorCloseHeavy;
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

    private void PlayDoorSound(bool open, Vector3 targetLocalPosition)
    {
        if (Time.time < _lastSoundTime + _soundCooldown)
        {
            return;
        }

        if (_doorTransform != null)
        {
            float yDelta = Mathf.Abs(targetLocalPosition.y - _doorTransform.localPosition.y);
            if (yDelta < _minSoundMovementY)
            {
                return;
            }
        }

        _lastSoundTime = Time.time;
        GameAudio.PlayAtPoint(GetDoorCueId(open), transform.position, 0.95f, 1.5f, 22f);
    }

    private static int ResolveActorId(Collider other)
    {
        if (other == null)
            return 0;

        Transform root = other.transform.root;
        return root != null ? root.GetInstanceID() : other.GetInstanceID();
    }
}
