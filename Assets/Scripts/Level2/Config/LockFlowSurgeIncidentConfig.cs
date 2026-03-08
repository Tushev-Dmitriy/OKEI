using UnityEngine;

[CreateAssetMenu(fileName = "LockFlowSurgeIncidentConfig", menuName = "Level2/Lock Config/Incidents/Flow Surge", order = 43)]
public class LockFlowSurgeIncidentConfig : ScriptableObject
{
    public float flowSurgeResolveDecayPerSecond = 0.5f;
    public float flowSurgeWhileBrokenDrainPerSecond = 3.4f;
    public float flowSurgeStabilizeTime = 4f;
    public float flowSurgeTimeoutWaterDeltaMin = -8f;
    public float flowSurgeTimeoutWaterDeltaMax = 8f;
    public float flowSurgeWaterStepRandomMin = 0.55f;
    public float flowSurgeWaterStepRandomMax = 1.3f;
}
