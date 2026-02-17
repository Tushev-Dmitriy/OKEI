using UnityEngine;

public class Bridge : MonoBehaviour, ISceneSaveable
{
    [SerializeField] private AllBridgeController bridgeController;
    [SerializeField] private string saveId;
    [SerializeField] SceneObjectType objectType;
    [SerializeField] private int openPercent;

    public string SaveId => saveId;
    public void ChangePercent(int percent)
    {
        openPercent = percent;
    }

    public SceneObjectStateData CaptureState()
    {
        return new SceneObjectStateData
        {
            id = saveId,
            type = objectType,
            state = openPercent
        };
    }

    public void RestoreState(SceneObjectStateData data)
    {
        openPercent = data.state;
        ApplyInstant();
    }

    public void ApplyInstant()
    {
        if (openPercent != 0) 
        {
            bridgeController.RaiseBridgeToPercent(openPercent);
        } else
        {
            bridgeController.LowerBridge();
        }
    }
}
