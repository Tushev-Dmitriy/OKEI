using DG.Tweening;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private GameObject posToStopObj;
    [SerializeField] private GameObject posToEndObj;
    [SerializeField] private Transform lockWaterTransform;
    [SerializeField] private float moveToStopDuration = 10f;
    [SerializeField] private float moveToEndDuration = 12f;
    [SerializeField] private float sinkDepth = 7f;
    [SerializeField] private float sinkDuration = 4f;

    private Vector3 _posToStop;
    private Vector3 _posToEnd;

    private Tween _moveTween;
    private Tween _sinkTween;
    private bool _hasStopped;
    private bool _isSinking;
    private bool _floatOffsetCaptured;
    private float _floatOffsetY;

    public bool HasReachedStop => _hasStopped;

    private void Awake()
    {
        if (posToStopObj != null)
            _posToStop = posToStopObj.transform.position;

        if (posToEndObj != null)
            _posToEnd = posToEndObj.transform.position;
    }

    private void Start()
    {
        MoveToStop();
    }

    private void Update()
    {
        if (!_hasStopped || _isSinking || lockWaterTransform == null)
            return;

        if (!_floatOffsetCaptured)
        {
            _floatOffsetY = transform.position.y - lockWaterTransform.position.y;
            _floatOffsetCaptured = true;
        }

        Vector3 position = transform.position;
        position.y = lockWaterTransform.position.y + _floatOffsetY;
        transform.position = position;
    }

    public void SetLockWaterTransform(Transform waterTransform)
    {
        lockWaterTransform = waterTransform;
        _floatOffsetCaptured = false;
    }

    private void MoveToStop()
    {
        if (posToStopObj == null)
        {
            _hasStopped = true;
            return;
        }

        KillMovementTweens();
        _moveTween = transform.DOMove(_posToStop, moveToStopDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(ChangeStop);
    }

    private void ChangeStop() => _hasStopped = !_hasStopped;

    public void MoveToEnd()
    {
        if (_isSinking)
            return;

        if (posToEndObj == null)
            return;

        KillMovementTweens();
        _moveTween = transform.DOMove(_posToEnd, moveToEndDuration).SetEase(Ease.InOutSine);
        _hasStopped = false;
        _floatOffsetCaptured = false;
    }

    public void SinkShip()
    {
        if (_isSinking)
            return;

        _isSinking = true;
        KillMovementTweens();

        Vector3 sinkTarget = transform.position + Vector3.down * sinkDepth;
        _sinkTween = transform.DOMove(sinkTarget, sinkDuration).SetEase(Ease.InSine);
    }

    private void KillMovementTweens()
    {
        _moveTween?.Kill();
        _sinkTween?.Kill();
    }

    private void OnDestroy()
    {
        KillMovementTweens();
    }
}
