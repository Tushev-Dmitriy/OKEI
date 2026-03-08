using UnityEngine;

[CreateAssetMenu(fileName = "LockPressureLeakIncidentConfig", menuName = "Level2/Lock Config/Incidents/Pressure Leak", order = 41)]
public class LockPressureLeakIncidentConfig : ScriptableObject
{
    [Range(0f, 1f)] public float stabilizationPressureLeakChance = 0.6f;
    [Range(0f, 1f)] public float waterPressureLeakChance = 0.5f;
    [Range(0f, 1f)] public float liftPressureLeakChance = 0.34f;

    public float pressureLeakExtraDrainPerSecond = 2.9f;
    public float pressureLeakIntegrityDrainPerSecond = 0.7f;
    public float pressureLeakTimeoutPressureLoss = 12f;
}
