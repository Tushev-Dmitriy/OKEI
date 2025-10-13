using UnityEngine;

public class AllBridgeController : MonoBehaviour
{
    [SerializeField] private BridgePart rightPart;
    [SerializeField] private BridgePart leftPart;

    public void RaiseBridge()
    {
        rightPart.RaisePart();
        leftPart.RaisePart();
    }

    public void LowerBridge()
    {
        rightPart.LowerPart();
        leftPart.LowerPart();
    }
}
