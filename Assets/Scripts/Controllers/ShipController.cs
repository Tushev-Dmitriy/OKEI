using DG.Tweening;
using System;
using UnityEngine;

public class ShipController : MonoBehaviour, ISceneSaveable
{
    [SerializeField] private string saveId = "Level2.ShipController";
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
    private ShipMotionState _motionState;
    private bool _hasReachedEnd;

    public bool HasReachedStop => _hasStopped;
    public bool HasReachedEnd => _hasReachedEnd;
    public string SaveId => saveId;
    public event Action ReachedEnd;

    private enum ShipMotionState
    {
        Idle,
        MovingToStop,
        MovingToEnd,
        Sinking
    }

    [Serializable]
    private sealed class ShipSaveState
    {
        public Vector3Data position;
        public Vector3Data rotation;
        public bool hasStopped;
        public bool isSinking;
        public bool floatOffsetCaptured;
        public float floatOffsetY;
        public int motionState;
        public bool hasReachedEnd;
    }

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
            _motionState = ShipMotionState.Idle;
            return;
        }

        KillMovementTweens();
        _motionState = ShipMotionState.MovingToStop;
        _moveTween = transform.DOMove(_posToStop, moveToStopDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(ChangeStop);
    }

    private void ChangeStop()
    {
        _hasStopped = true;
        _motionState = ShipMotionState.Idle;
        _hasReachedEnd = false;
    }

    public void MoveToEnd()
    {
        if (_isSinking)
            return;

        if (posToEndObj == null)
            return;

        KillMovementTweens();
        _motionState = ShipMotionState.MovingToEnd;
        _hasReachedEnd = false;
        _moveTween = transform.DOMove(_posToEnd, moveToEndDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(HandleReachedEnd);
        _hasStopped = false;
        _floatOffsetCaptured = false;
    }

    public void SinkShip()
    {
        if (_isSinking)
            return;

        _isSinking = true;
        KillMovementTweens();
        _motionState = ShipMotionState.Sinking;

        Vector3 sinkTarget = transform.position + Vector3.down * sinkDepth;
        _sinkTween = transform.DOMove(sinkTarget, sinkDuration).SetEase(Ease.InSine);
    }

    public void ForceDocked()
    {
        KillMovementTweens();
        if (posToStopObj != null)
        {
            transform.position = _posToStop;
        }

        _hasStopped = true;
        _hasReachedEnd = false;
        _isSinking = false;
        _motionState = ShipMotionState.Idle;
        _floatOffsetCaptured = false;
    }

    public SceneObjectStateData CaptureState()
    {
        ShipSaveState state = new ShipSaveState
        {
            position = ToVector3Data(transform.position),
            rotation = ToVector3Data(transform.eulerAngles),
            hasStopped = _hasStopped,
            isSinking = _isSinking,
            floatOffsetCaptured = _floatOffsetCaptured,
            floatOffsetY = _floatOffsetY,
            motionState = (int)_motionState,
            hasReachedEnd = _hasReachedEnd
        };

        return new SceneObjectStateData
        {
            id = SaveId,
            type = SceneObjectType.Ship,
            state = _hasStopped ? 1 : 0,
            json = JsonUtility.ToJson(state)
        };
    }

    public void RestoreState(SceneObjectStateData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.json))
        {
            return;
        }

        ShipSaveState state = JsonUtility.FromJson<ShipSaveState>(data.json);
        if (state == null)
        {
            return;
        }

        KillMovementTweens();
        transform.position = FromVector3Data(state.position);
        transform.eulerAngles = FromVector3Data(state.rotation);
        _hasStopped = state.hasStopped;
        _isSinking = state.isSinking;
        _floatOffsetCaptured = state.floatOffsetCaptured;
        _floatOffsetY = state.floatOffsetY;
        _hasReachedEnd = state.hasReachedEnd;
        _motionState = Enum.IsDefined(typeof(ShipMotionState), state.motionState)
            ? (ShipMotionState)state.motionState
            : ShipMotionState.Idle;

        if (_motionState == ShipMotionState.MovingToStop && !_hasStopped)
        {
            MoveToStop();
        }
        else if (_motionState == ShipMotionState.MovingToEnd)
        {
            MoveToEnd();
        }
    }

    private void HandleReachedEnd()
    {
        _hasReachedEnd = true;
        _motionState = ShipMotionState.Idle;
        ReachedEnd?.Invoke();
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

    private static Vector3Data ToVector3Data(Vector3 value)
    {
        return new Vector3Data { x = value.x, y = value.y, z = value.z };
    }

    private static Vector3 FromVector3Data(Vector3Data value)
    {
        if (value == null)
        {
            return Vector3.zero;
        }

        return new Vector3(value.x, value.y, value.z);
    }
}
