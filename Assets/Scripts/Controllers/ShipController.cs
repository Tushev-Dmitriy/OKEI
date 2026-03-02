using DG.Tweening;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private GameObject posToStopObj;
    [SerializeField] private GameObject posToEndObj;
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

    private void Awake()
    {
        _posToStop = posToStopObj.transform.position;
        _posToEnd = posToEndObj.transform.position;
    }

    private void Start()
    {
        MoveToStop();
    }

    private void MoveToStop()
    {
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

        KillMovementTweens();
        _moveTween = transform.DOMove(_posToEnd, moveToEndDuration).SetEase(Ease.InOutSine);
        _hasStopped = false;
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
