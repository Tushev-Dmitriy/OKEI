using UnityEngine;

public class SimpleShipController : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float approachSpeed = 2.2f;
    [SerializeField] private float exitSpeed = 3f;
    [SerializeField] private float sinkSpeed = 2f;
    [SerializeField] private float sinkTiltSpeed = 30f;
    [SerializeField] private float sinkTiltAngle = 25f;

    private ShipMode _mode = ShipMode.Idle;
    private Quaternion _startRotation;

    private enum ShipMode
    {
        Idle,
        Approach,
        Waiting,
        Exit,
        Sinking
    }

    private void Awake()
    {
        _startRotation = transform.rotation;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        switch (_mode)
        {
            case ShipMode.Approach:
                MoveToPoint(holdPoint, approachSpeed, ShipMode.Waiting, dt);
                break;
            case ShipMode.Exit:
                MoveToPoint(exitPoint, exitSpeed, ShipMode.Idle, dt);
                break;
            case ShipMode.Sinking:
                UpdateSinking(dt);
                break;
        }
    }

    public void SetRoute(Transform hold, Transform exit)
    {
        holdPoint = hold;
        exitPoint = exit;
    }

    public void BeginApproach()
    {
        if (_mode == ShipMode.Sinking)
            return;

        if (holdPoint == null)
        {
            _mode = ShipMode.Waiting;
            return;
        }

        _mode = ShipMode.Approach;
    }

    public void BeginExit()
    {
        if (_mode == ShipMode.Sinking)
            return;

        if (exitPoint == null)
        {
            _mode = ShipMode.Idle;
            return;
        }

        _mode = ShipMode.Exit;
    }

    public void BeginSinking()
    {
        _mode = ShipMode.Sinking;
    }

    private void MoveToPoint(Transform target, float speed, ShipMode finalMode, float dt)
    {
        if (target == null)
        {
            _mode = finalMode;
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(transform.position, target.position, speed * dt);
        transform.position = nextPosition;

        if ((transform.position - target.position).sqrMagnitude < 0.02f)
            _mode = finalMode;
    }

    private void UpdateSinking(float dt)
    {
        transform.position += Vector3.down * sinkSpeed * dt;

        Quaternion sinkTargetRotation = _startRotation * Quaternion.Euler(sinkTiltAngle, 0f, sinkTiltAngle * 0.3f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, sinkTargetRotation, sinkTiltSpeed * dt);
    }
}
