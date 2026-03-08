using UnityEngine;

[CreateAssetMenu(fileName = "LockCoolingFaultIncidentConfig", menuName = "Level2/Lock Config/Incidents/Cooling Fault", order = 42)]
public class LockCoolingFaultIncidentConfig : ScriptableObject
{
    [Range(0f, 1f)] public float liftCoolingFaultChance = 0.68f;

    public float coolingFaultHeatPerSecond = 1.25f;
    public float coolingFaultCoolingEfficiencyMultiplier = 0.35f;
    public float coolingFaultTimeoutHeatGain = 12f;
}
