using UnityEngine;

[CreateAssetMenu(fileName = "LockSimulationConfig", menuName = "Level2/Lock Config/Simulation", order = 30)]
public class LockSimulationConfig : ScriptableObject
{
    [Header("Core Simulation")]
    public float pressureDrainPerSecond = 1.15f;
    public float coolingPerSecond = 3.25f;
    public float passiveHeatPerSecondPhase3 = 0.9f;
    public float integrityRecoverPerSecond = 0.9f;
    public float integrityDrainPerSecond = 5f;
    public float brokenWhileDrainMultiplier = 2.2f;
    public float waterPhaseIntegrityRecoveryMultiplier = 0.35f;
    public float liftPhaseIntegrityRecoveryMultiplier = 0.3f;

    [Header("FOR Actions")]
    public int pumpIterations = 5;
    public float pumpStepValue = 4f;
    public float pumpStepDelay = 0.3f;
    public float waterStepValue = 0.75f;
    public float waterStepDelay = 0.38f;
    public float liftStepValue = 0.75f;
    public float liftStepDelay = 0.2f;
    public float liftHeatPerSmallStep = 0.3f;
    public float liftHeatPerBigStep = 0.85f;
    [Min(1)] public int waterPrimaryIterations = 10;
    [Min(1)] public int waterSecondaryIterations = 5;
    [Min(1)] public int liftPrimaryIterations = 25;
    [Min(1)] public int liftSecondaryIterations = 10;

    [Header("Safety Penalties")]
    public float phase2EmergencyDuration = 3f;
    public float phase2EmergencyDrainPerSecond = 22f;
    public float phase2ImmediateHit = 18f;
    public float overheatIntegrityDrainPerSecond = 18f;

    [Header("Failure")]
    public float failureWaterRisePerSecond = 28f;
    public float restartDelay = 4f;

    [Header("Progress")]
    [Range(0f, 1f)] public float stabilizationProgressWeight = 0.35f;
    [Range(0f, 1f)] public float waterLevelingProgressWeight = 0.3f;
}
