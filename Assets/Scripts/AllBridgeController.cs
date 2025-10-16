using UnityEngine;

public class AllBridgeController : MonoBehaviour
{
    [SerializeField] private BridgePart rightPart;
    [SerializeField] private BridgePart leftPart;
    [SerializeField] private RopePart rightRope;
    [SerializeField] private RopePart leftRope;

    public void RaiseBridge() => RaiseBridgeToPercent(1f);
    public void LowerBridge() => RaiseBridgeToPercent(0f);

    public void RaiseBridgeToPercent(float percent)
    {
        float progress = Mathf.Clamp01(percent / 100f);

        rightPart.MoveToProgress(progress);
        leftPart.MoveToProgress(progress);
        rightRope.MoveToProgress(progress);
        leftRope.MoveToProgress(progress);
    }
}
