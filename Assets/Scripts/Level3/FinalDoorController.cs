using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FinalDoorController : MonoBehaviour
{
    [Header("Fallback Transform Animation")]
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private Vector3 _openLocalPositionOffset = new Vector3(0f, 5f, 0f);
    [SerializeField] private Vector3 _openLocalEulerOffset;
    [SerializeField] private float _openDuration = 1f;
    [SerializeField] private AnimationCurve _openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 _closedLocalPosition;
    private Quaternion _closedLocalRotation;
    private Coroutine _openRoutine;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (_doorTransform == null)
        {
            _doorTransform = transform;
        }

        _closedLocalPosition = _doorTransform.localPosition;
        _closedLocalRotation = _doorTransform.localRotation;
    }

    public void Open(bool playSound = true)
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        if (playSound)
        {
            GameAudio.PlayAtPoint(AudioCueIds.Level3DoorOpenLight, transform.position, 1f, 2f, 24f);
            GameAudio.PlayAtPoint(AudioCueIds.Level3DoorOpenFinal, transform.position, 0.55f, 2f, 24f);
        }

        if (_openRoutine != null)
        {
            StopCoroutine(_openRoutine);
        }

        _openRoutine = StartCoroutine(OpenByTransformRoutine());
    }

    private IEnumerator OpenByTransformRoutine()
    {
        if (_doorTransform == null || _openDuration <= 0f)
        {
            ApplyOpenTransformInstant();
            _openRoutine = null;
            yield break;
        }

        Vector3 startPosition = _doorTransform.localPosition;
        Quaternion startRotation = _doorTransform.localRotation;
        Vector3 targetPosition = _closedLocalPosition + _openLocalPositionOffset;
        Quaternion targetRotation = _closedLocalRotation * Quaternion.Euler(_openLocalEulerOffset);

        float elapsed = 0f;
        while (elapsed < _openDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / _openDuration);
            float easedProgress = _openCurve != null ? _openCurve.Evaluate(progress) : progress;

            _doorTransform.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, easedProgress);
            _doorTransform.localRotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, easedProgress);

            yield return null;
        }

        _doorTransform.localPosition = targetPosition;
        _doorTransform.localRotation = targetRotation;
        _openRoutine = null;
    }

    private void ApplyOpenTransformInstant()
    {
        if (_doorTransform == null)
        {
            return;
        }

        _doorTransform.localPosition = _closedLocalPosition + _openLocalPositionOffset;
        _doorTransform.localRotation = _closedLocalRotation * Quaternion.Euler(_openLocalEulerOffset);
    }
}
