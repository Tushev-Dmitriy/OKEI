using UnityEngine;
using DG.Tweening;

public class MovingPlatform : MonoBehaviour, IMovingPlatform
{
    [SerializeField] private MovingPlatformConfig _config;

    private Vector3 _startPos;
    private Tween _tween;

    private Vector3 _lastPosition;
    public Vector3 Velocity { get; private set; }

    private void Awake()
    {
        _startPos = transform.position;
        _lastPosition = transform.position;
    }

    private void Start()
    {
        Activate();
    }

    private void Update()
    {
        Velocity = (transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = transform.position;
    }

    public void Activate()
    {
        _tween = transform.DOMove(_startPos + _config.moveOffset, _config.moveDuration)
            .SetEase(Ease.Linear)
            .SetLoops(_config.loop ? -1 : 1, LoopType.Yoyo);
    }

    public void Deactivate()
    {
        _tween?.Kill();
        transform.position = _startPos;
    }
}
