using System;
using UnityEngine;

public class AllBridgeController : MonoBehaviour
{
    [SerializeField] private BridgePart rightPart;
    [SerializeField] private BridgePart leftPart;
    [SerializeField] private RopePart rightRope;
    [SerializeField] private RopePart leftRope;

    public event Action<float> OnBridgePercentChanged;

    private float _currentPercent;

    public void RaiseBridge() => RaiseBridgeToPercent(100f);
    public void LowerBridge() => RaiseBridgeToPercent(0f);

    public void RaiseBridgeToPercent(float percent)
    {
        _currentPercent = Mathf.Clamp(percent, 0f, 100f);

        rightPart.MoveToProgress(_currentPercent / 100f);
        leftPart.MoveToProgress(_currentPercent / 100f);
        rightRope.MoveToProgress(_currentPercent / 100f);
        leftRope.MoveToProgress(_currentPercent / 100f);

        OnBridgePercentChanged?.Invoke(_currentPercent);
    }

    public float CurrentPercent => _currentPercent;
}
