using UnityEngine;

[CreateAssetMenu(fileName = "LockLiftJamIncidentConfig", menuName = "Level2/Lock Config/Incidents/Lift Jam", order = 44)]
public class LockLiftJamIncidentConfig : ScriptableObject
{
    public float liftJamPassiveDrainPerSecond = 1.5f;
    public float liftJamEfficiencyMultiplier = 0.55f;
    [Min(1)] public int liftJamResolveMinimumIterations = 10;
    public float liftJamTimeoutLiftPenalty = 14f;
}
