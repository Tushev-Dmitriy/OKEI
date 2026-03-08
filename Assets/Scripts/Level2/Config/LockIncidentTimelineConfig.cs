using UnityEngine;

[CreateAssetMenu(fileName = "LockIncidentTimelineConfig", menuName = "Level2/Lock Config/Incidents/Timeline", order = 40)]
public class LockIncidentTimelineConfig : ScriptableObject
{
    [Header("Incidents")]
    public bool enableIncidents = true;
    public float firstIncidentDelayMin = 16f;
    public float firstIncidentDelayMax = 24f;
    public float incidentDelayMin = 20f;
    public float incidentDelayMax = 34f;
    public float incidentBaseDuration = 14f;
    public float incidentDurationPerThreatTier = 2.5f;
    public float threatTierStepSeconds = 85f;
    [Range(1, 6)] public int maxThreatTier = 5;
    public float threatStrengthPerTier = 0.22f;
    public float incidentTimeoutIntegrityHit = 10f;
    public float incidentResolveIntegrityBonus = 1.5f;
    public float incidentResolvedDelayMin = 12f;
    public float incidentResolvedDelayMax = 18f;
    public float incidentTimeoutDelayMin = 8f;
    public float incidentTimeoutDelayMax = 14f;
    public float immediateIncidentDelayMin = 8f;
    public float immediateIncidentDelayMax = 12f;
    public float incidentTierIntervalReductionSeconds = 2.5f;
    public float incidentTierIntervalMaxReductionFactor = 0.8f;
    public float incidentMinDelayFloor = 7f;
    public float threatTierStepSecondsFloor = 20f;
}
