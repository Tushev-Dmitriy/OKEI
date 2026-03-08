using UnityEngine;

[CreateAssetMenu(fileName = "LockCoreConfig", menuName = "Level2/Lock Config/Core", order = 20)]
public class LockCoreConfig : ScriptableObject
{
    [Header("Core Params (0-100)")]
    [Range(0f, 100f)] public float systemIntegrity = 74f;
    [Range(0f, 100f)] public float pressure = 72f;
    [Range(0f, 100f)] public float temperature = 18f;
    [Range(0f, 100f)] public float waterLevel;
    [Range(0f, 100f)] public float liftPower;

    [Header("Targets")]
    [Range(0f, 100f)] public float minPressure = 45f;
    [Range(0f, 100f)] public float maxTemperature = 82f;
    [Range(0f, 100f)] public float waterLevelTarget = 80f;
    [Range(0f, 100f)] public float liftPowerTarget = 100f;
    public float waterTargetTolerance = 1f;

    [Header("Phase 1 / Stabilization")]
    public float stabilizationRequiredTime = 110f;
    public float requiredIntegrityForPhase2 = 60f;
    public float stabilizationTimerLossPerSecond = 1f;
}
