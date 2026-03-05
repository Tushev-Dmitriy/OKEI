using UnityEngine;

[CreateAssetMenu(
    fileName = "LockLevelConfig_Default",
    menuName = "Level2/Lock Level Config",
    order = 0)]
public class LockLevelConfig : ScriptableObject
{
    [Header("Reload")]
    public string reloadSceneName = "Level2";

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
    public float flowSurgeWaterStepRandomMin = 0.55f;
    public float flowSurgeWaterStepRandomMax = 1.3f;

    [Header("Safety Penalties")]
    public float phase2EmergencyDuration = 3f;
    public float phase2EmergencyDrainPerSecond = 22f;
    public float phase2ImmediateHit = 18f;
    public float overheatIntegrityDrainPerSecond = 18f;

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
    [Range(0f, 1f)] public float stabilizationPressureLeakChance = 0.6f;
    [Range(0f, 1f)] public float waterPressureLeakChance = 0.5f;
    [Range(0f, 1f)] public float liftPressureLeakChance = 0.34f;
    [Range(0f, 1f)] public float liftCoolingFaultChance = 0.68f;
    public float flowSurgeResolveDecayPerSecond = 0.5f;
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

    [Header("Incident Effects")]
    public float pressureLeakExtraDrainPerSecond = 2.9f;
    public float pressureLeakIntegrityDrainPerSecond = 0.7f;
    public float coolingFaultHeatPerSecond = 1.25f;
    public float coolingFaultCoolingEfficiencyMultiplier = 0.35f;
    public float flowSurgeWhileBrokenDrainPerSecond = 3.4f;
    public float flowSurgeStabilizeTime = 4f;
    public float liftJamPassiveDrainPerSecond = 1.5f;
    public float liftJamEfficiencyMultiplier = 0.55f;
    [Min(1)] public int liftJamResolveMinimumIterations = 10;
    public float liftJamTimeoutLiftPenalty = 14f;
    public float pressureLeakTimeoutPressureLoss = 12f;
    public float coolingFaultTimeoutHeatGain = 12f;
    public float flowSurgeTimeoutWaterDeltaMin = -8f;
    public float flowSurgeTimeoutWaterDeltaMax = 8f;

    [Header("Failure")]
    public float failureWaterRisePerSecond = 28f;
    public float restartDelay = 4f;

    [Header("Water Visual")]
    public float waterMinY = 3f;
    public float waterMaxY = 14f;

    [Header("Gate Visual")]
    public Vector3 gateOpenOffset = new Vector3(0f, 8f, 0f);
    public Vector3 gateOpenEulerOffset;
    public float gateOpenSpeed = 2.5f;

    [Header("Progress")]
    [Range(0f, 1f)] public float stabilizationProgressWeight = 0.35f;
    [Range(0f, 1f)] public float waterLevelingProgressWeight = 0.30f;

    [Header("Text Labels")]
    public string forIdleLabel = "FOR: ожидание";
    public string forInterruptedLabel = "FOR: прерван";
    public string pumpForLabelTemplate = "Подкачка FOR x{0}";
    public string waterForLabelTemplate = "Вода FOR x{0}";
    public string liftForLabelTemplate = "Подъем FOR x{0}";
    public string incidentLabelNone = "Нет";
    public string pressureLeakIncidentLabel = "Утечка давления";
    public string coolingFaultIncidentLabel = "Сбой охлаждения";
    public string flowSurgeIncidentLabel = "Турбулентность потока";
    public string liftJamIncidentLabel = "Заклинивание подъема";

    [Header("Text Hints")]
    public string pressureLeakIncidentHint = "Сделайте подкачку FOR x5 один раз.";
    public string coolingFaultIncidentHint = "Выключите охлаждение и включите снова.";
    public string flowSurgeIncidentHintTemplate = "Удерживайте WHILE {0:0.0}с";
    public string liftJamIncidentHint = "Сделайте Подъем FOR x10+ при активном WHILE.";
    public string coolingRestartHint = "Включите охлаждение снова для завершения перезапуска.";
    public string pressureLeakResolvedMessage = "Утечка устранена циклом подкачки.";
    public string flowSurgeResolvedMessage = "Поток стабилизирован.";
    public string liftJamResolvedMessage = "Заклинивание снято силовым циклом.";
    public string coolingRestartResolvedMessage = "Контур охлаждения перезапущен.";
    public string incidentTimeoutHint = "Время инцидента вышло. Системы повреждены.";

    [Header("Debug")]
    public bool showDebugCompleteButton = true;
    public bool allowDebugButtonInBuild;
    public Rect debugCompleteButtonRect = new Rect(20f, 20f, 220f, 36f);
    public KeyCode debugCompleteHotkey = KeyCode.F8;
    public string debugCompleteButtonLabel = "DEBUG: Пройти уровень";
    public float debugCompletePressureBonus = 5f;
    public float debugCompleteTemperatureBuffer = 5f;
    public float debugCompleteIntegrityFloor = 65f;
}
