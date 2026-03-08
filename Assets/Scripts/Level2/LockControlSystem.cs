using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LockControlSystem : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private bool applyConfigOnAwake = true;
    [SerializeField] private LockLevelConfig levelConfig;
    [SerializeField] private string fallbackConfigResourcePath = "Level2/LockLevelConfig_Default";

    [Header("References")]
    [SerializeField] private LockInputs lockInputs;
    [SerializeField] private LockUI lockUi;
    [SerializeField] private SimpleShipController simpleShipController;
    [SerializeField] private Transform shipHoldPoint;
    [SerializeField] private Transform shipExitPoint;
    [SerializeField] private Transform gateTransform;
    [SerializeField] private Transform waterTransform;
    [SerializeField] private AudioSource alarmAudio;

    [Header("Reload")]
    [SerializeField] private string reloadSceneName = "Level2";

    [Header("Core Params (0-100)")]
    [SerializeField, Range(0f, 100f)] private float systemIntegrity = 74f;
    [SerializeField, Range(0f, 100f)] private float pressure = 72f;
    [SerializeField, Range(0f, 100f)] private float temperature = 18f;
    [SerializeField, Range(0f, 100f)] private float waterLevel;
    [SerializeField, Range(0f, 100f)] private float liftPower;

    [Header("Targets")]
    [SerializeField, Range(0f, 100f)] private float minPressure = 45f;
    [SerializeField, Range(0f, 100f)] private float maxTemperature = 82f;
    [SerializeField, Range(0f, 100f)] private float waterLevelTarget = 80f;
    [SerializeField, Range(0f, 100f)] private float liftPowerTarget = 100f;
    [SerializeField] private float waterTargetTolerance = 1f;

    [Header("Phase 1 / Stabilization")]
    [SerializeField] private float stabilizationRequiredTime = 110f;
    [SerializeField] private float requiredIntegrityForPhase2 = 60f;
    [SerializeField] private float stabilizationTimerLossPerSecond = 1f;

    [Header("Core Simulation")]
    [SerializeField] private float pressureDrainPerSecond = 1.15f;
    [SerializeField] private float coolingPerSecond = 3.25f;
    [SerializeField] private float passiveHeatPerSecondPhase3 = 0.9f;
    [SerializeField] private float integrityRecoverPerSecond = 0.9f;
    [SerializeField] private float integrityDrainPerSecond = 5f;
    [SerializeField] private float brokenWhileDrainMultiplier = 2.2f;
    [SerializeField] private float waterPhaseIntegrityRecoveryMultiplier = 0.35f;
    [SerializeField] private float liftPhaseIntegrityRecoveryMultiplier = 0.3f;

    [Header("FOR Actions")]
    [SerializeField] private int pumpIterations = 5;
    [SerializeField] private float pumpStepValue = 4f;
    [SerializeField] private float pumpStepDelay = 0.3f;
    [SerializeField] private float waterStepValue = 0.75f;
    [SerializeField] private float waterStepDelay = 0.38f;
    [SerializeField] private float liftStepValue = 0.75f;
    [SerializeField] private float liftStepDelay = 0.2f;
    [SerializeField] private float liftHeatPerSmallStep = 0.3f;
    [SerializeField] private float liftHeatPerBigStep = 0.85f;
    [SerializeField, Min(1)] private int waterPrimaryIterations = 10;
    [SerializeField, Min(1)] private int waterSecondaryIterations = 5;
    [SerializeField, Min(1)] private int liftPrimaryIterations = 25;
    [SerializeField, Min(1)] private int liftSecondaryIterations = 10;
    [SerializeField] private float flowSurgeWaterStepRandomMin = 0.55f;
    [SerializeField] private float flowSurgeWaterStepRandomMax = 1.3f;

    [Header("Safety Penalties")]
    [SerializeField] private float phase2EmergencyDuration = 3f;
    [SerializeField] private float phase2EmergencyDrainPerSecond = 22f;
    [SerializeField] private float phase2ImmediateHit = 18f;
    [SerializeField] private float overheatIntegrityDrainPerSecond = 18f;

    [Header("Incidents")]
    [SerializeField] private bool enableIncidents = true;
    [SerializeField] private float firstIncidentDelayMin = 16f;
    [SerializeField] private float firstIncidentDelayMax = 24f;
    [SerializeField] private float incidentDelayMin = 20f;
    [SerializeField] private float incidentDelayMax = 34f;
    [SerializeField] private float incidentBaseDuration = 14f;
    [SerializeField] private float incidentDurationPerThreatTier = 2.5f;
    [SerializeField] private float threatTierStepSeconds = 85f;
    [SerializeField, Range(1, 6)] private int maxThreatTier = 5;
    [SerializeField] private float threatStrengthPerTier = 0.22f;
    [SerializeField] private float incidentTimeoutIntegrityHit = 10f;
    [SerializeField] private float incidentResolveIntegrityBonus = 1.5f;
    [SerializeField, Range(0f, 1f)] private float stabilizationPressureLeakChance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float waterPressureLeakChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float liftPressureLeakChance = 0.34f;
    [SerializeField, Range(0f, 1f)] private float liftCoolingFaultChance = 0.68f;
    [SerializeField] private float flowSurgeResolveDecayPerSecond = 0.5f;
    [SerializeField] private float incidentResolvedDelayMin = 12f;
    [SerializeField] private float incidentResolvedDelayMax = 18f;
    [SerializeField] private float incidentTimeoutDelayMin = 8f;
    [SerializeField] private float incidentTimeoutDelayMax = 14f;
    [SerializeField] private float immediateIncidentDelayMin = 8f;
    [SerializeField] private float immediateIncidentDelayMax = 12f;
    [SerializeField] private float incidentTierIntervalReductionSeconds = 2.5f;
    [SerializeField] private float incidentTierIntervalMaxReductionFactor = 0.8f;
    [SerializeField] private float incidentMinDelayFloor = 7f;
    [SerializeField] private float threatTierStepSecondsFloor = 20f;

    [Header("Incident Effects")]
    [SerializeField] private float pressureLeakExtraDrainPerSecond = 2.9f;
    [SerializeField] private float pressureLeakIntegrityDrainPerSecond = 0.7f;
    [SerializeField] private float coolingFaultHeatPerSecond = 1.25f;
    [SerializeField] private float coolingFaultCoolingEfficiencyMultiplier = 0.35f;
    [SerializeField] private float flowSurgeWhileBrokenDrainPerSecond = 3.4f;
    [SerializeField] private float flowSurgeStabilizeTime = 4f;
    [SerializeField] private float liftJamPassiveDrainPerSecond = 1.5f;
    [SerializeField] private float liftJamEfficiencyMultiplier = 0.55f;
    [SerializeField, Min(1)] private int liftJamResolveMinimumIterations = 10;
    [SerializeField] private float liftJamTimeoutLiftPenalty = 14f;
    [SerializeField] private float pressureLeakTimeoutPressureLoss = 12f;
    [SerializeField] private float coolingFaultTimeoutHeatGain = 12f;
    [SerializeField] private float flowSurgeTimeoutWaterDeltaMin = -8f;
    [SerializeField] private float flowSurgeTimeoutWaterDeltaMax = 8f;

    [Header("Failure")]
    [SerializeField] private float failureWaterRisePerSecond = 28f;
    [SerializeField] private float restartDelay = 4f;

    [Header("Water Visual")]
    [SerializeField] private float waterMinY = 3f;
    [SerializeField] private float waterMaxY = 14f;

    [Header("Gate Visual")]
    [SerializeField] private Vector3 gateOpenOffset = new Vector3(0f, 8f, 0f);
    [SerializeField] private Vector3 gateOpenEulerOffset;
    [SerializeField] private float gateOpenSpeed = 2.5f;

    [Header("Progress")]
    [SerializeField, Range(0f, 1f)] private float stabilizationProgressWeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float waterLevelingProgressWeight = 0.30f;

    [Header("Labels")]
    [SerializeField] private string forIdleLabel = "FOR: ожидание";
    [SerializeField] private string forInterruptedLabel = "FOR: прерван";
    [SerializeField] private string pumpForLabelTemplate = "Подкачка FOR x{0}";
    [SerializeField] private string waterForLabelTemplate = "Вода FOR x{0}";
    [SerializeField] private string liftForLabelTemplate = "Подъем FOR x{0}";
    [SerializeField] private string incidentLabelNone = "Нет";
    [SerializeField] private string pressureLeakIncidentLabel = "Утечка давления";
    [SerializeField] private string coolingFaultIncidentLabel = "Сбой охлаждения";
    [SerializeField] private string flowSurgeIncidentLabel = "Турбулентность потока";
    [SerializeField] private string liftJamIncidentLabel = "Заклинивание подъема";

    [Header("Hints")]
    [SerializeField] private string pressureLeakIncidentHint = "Сделайте подкачку FOR x5 один раз.";
    [SerializeField] private string coolingFaultIncidentHint = "Выключите охлаждение и включите снова.";
    [SerializeField] private string flowSurgeIncidentHintTemplate = "Удерживайте WHILE {0:0.0}с";
    [SerializeField] private string liftJamIncidentHint = "Сделайте Подъем FOR x10+ при активном WHILE.";
    [SerializeField] private string coolingRestartHint = "Включите охлаждение снова для завершения перезапуска.";
    [SerializeField] private string pressureLeakResolvedMessage = "Утечка устранена циклом подкачки.";
    [SerializeField] private string flowSurgeResolvedMessage = "Поток стабилизирован.";
    [SerializeField] private string liftJamResolvedMessage = "Заклинивание снято силовым циклом.";
    [SerializeField] private string coolingRestartResolvedMessage = "Контур охлаждения перезапущен.";
    [SerializeField] private string incidentTimeoutHint = "Время инцидента вышло. Системы повреждены.";

    [Header("Debug")]
    [SerializeField] private bool showDebugCompleteButton = true;
    [SerializeField] private bool allowDebugButtonInBuild;
    [SerializeField] private Rect debugCompleteButtonRect = new Rect(20f, 20f, 220f, 36f);
    [SerializeField] private KeyCode debugCompleteHotkey = KeyCode.F8;
    [SerializeField] private string debugCompleteButtonLabel = "DEBUG: Пройти уровень";
    [SerializeField] private float debugCompletePressureBonus = 5f;
    [SerializeField] private float debugCompleteTemperatureBuffer = 5f;
    [SerializeField] private float debugCompleteIntegrityFloor = 65f;

    private LockPhase _phase = LockPhase.Stabilization;
    private Coroutine _activeForRoutine;
    private Coroutine _failureRoutine;

    private float _stabilizationTimer;
    private float _phase2EmergencyTimer;

    private bool _failureTriggered;
    private bool _gateOpening;
    private bool _gatePoseInitialized;
    private bool _shipStarted;

    private string _forLabel;
    private int _forIteration;
    private int _forTotal;
    private bool _forRunning;

    private Vector3 _gateClosedLocalPosition;
    private Vector3 _gateOpenLocalPosition;
    private Quaternion _gateClosedLocalRotation;
    private Quaternion _gateOpenLocalRotation;

    private float _sessionTimer;
    private float _nextIncidentTimer;
    private int _threatTier = 1;
    private IncidentType _activeIncident = IncidentType.None;
    private float _incidentTimer;
    private float _incidentDuration;
    private float _incidentResolveProgress;
    private string _incidentLabel;
    private string _incidentHint = string.Empty;
    private bool _coolingRestartRequiresOff;
    private bool _coolingRestartRequiresOn;

    private bool _previousCoolingState;

    private enum IncidentType
    {
        None,
        PressureLeak,
        CoolingFault,
        FlowSurge,
        LiftJam
    }

    public LockPhase CurrentPhase => _phase;
    public bool WhileActive => IsWhileConditionTrue();
    public bool CanInteract => CanReceiveInput;
    public bool CanReceiveInput => !_failureTriggered && _phase != LockPhase.Completed && _phase != LockPhase.Failed;

    public float SystemIntegrity => systemIntegrity;
    public float Pressure => pressure;
    public float Temperature => temperature;
    public float WaterLevel => waterLevel;
    public float LiftPower => liftPower;
    public float WaterLevelTarget => waterLevelTarget;
    public float LiftPowerTarget => liftPowerTarget;
    public float StabilizationProgress => stabilizationRequiredTime <= 0f ? 1f : Mathf.Clamp01(_stabilizationTimer / stabilizationRequiredTime);
    public float SessionProgress => CalculateSessionProgress();

    public bool IsForRunning => _forRunning;
    public int ForIteration => _forIteration;
    public int ForTotal => _forTotal;
    public string ForLabel => _forLabel;
    public int WaterPrimaryIterations => Mathf.Max(1, waterPrimaryIterations);
    public int WaterSecondaryIterations => Mathf.Max(1, waterSecondaryIterations);
    public int LiftPrimaryIterations => Mathf.Max(1, liftPrimaryIterations);
    public int LiftSecondaryIterations => Mathf.Max(1, liftSecondaryIterations);
    public string PumpForCaption => FormatForLabel(pumpForLabelTemplate, Mathf.Max(1, pumpIterations));
    public string WaterPrimaryCaption => FormatForLabel(waterForLabelTemplate, WaterPrimaryIterations);
    public string WaterSecondaryCaption => FormatForLabel(waterForLabelTemplate, WaterSecondaryIterations);
    public string LiftPrimaryCaption => FormatForLabel(liftForLabelTemplate, LiftPrimaryIterations);
    public string LiftSecondaryCaption => FormatForLabel(liftForLabelTemplate, LiftSecondaryIterations);

    public int ThreatTier => _threatTier;
    public bool HasActiveIncident => _activeIncident != IncidentType.None;
    public string ActiveIncidentLabel => _incidentLabel;
    public string ActiveIncidentHint => _incidentHint;
    public float ActiveIncidentTimeLeft => Mathf.Max(0f, _incidentTimer);
    public float ActiveIncidentDuration => Mathf.Max(0.01f, _incidentDuration);

    public bool PowerEnabled => lockInputs != null && lockInputs.PowerEnabled;
    public bool CoolingEnabled => lockInputs != null && lockInputs.CoolingEnabled;
    public bool SafeModeEnabled => lockInputs != null && lockInputs.SafeModeEnabled;

    private void Awake()
    {
        ResolveAndApplyConfig();
        InitializeRuntimeLabels();
        ResolveLocalReferences();
        if (lockInputs != null)
            lockInputs.SetSystem(this);

        ResolveReferences();
        InitializeGatePose();
        SnapshotInputStates();
        lockUi?.Initialize(this, lockInputs);
        lockUi?.Refresh();
    }

    private void Start()
    {
        StartShipIfReady();
        ApplyWaterVisual();
        ScheduleNextIncident(isFirst: true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_failureTriggered)
        {
            UpdateGate(dt);
            ApplyWaterVisual();
            lockUi?.Refresh();
            return;
        }

        _sessionTimer += dt;
        UpdateThreatTier();
        HandleInputStateChanges();
        UpdateBaseSimulation(dt);
        ApplyActiveIncidentEffects(dt);

        switch (_phase)
        {
            case LockPhase.Stabilization:
                UpdateStabilizationPhase(dt);
                break;
            case LockPhase.WaterLeveling:
                UpdateWaterPhase(dt);
                break;
            case LockPhase.LiftPreparation:
                UpdateLiftPhase(dt);
                break;
        }

        ClampCoreValues();
        if (systemIntegrity <= 0f)
        {
            TriggerFailure();
            return;
        }

        UpdateIncidentSystem(dt);
        HandleDebugHotkey();

        UpdateGate(dt);
        ApplyWaterVisual();
        lockUi?.Refresh();
    }

    private void OnGUI()
    {
        if (!IsDebugControlAllowed() || _failureTriggered || _phase == LockPhase.Completed)
            return;

        if (GUI.Button(debugCompleteButtonRect, debugCompleteButtonLabel))
            DebugCompleteLevel();
    }

    public void DebugCompleteLevel()
    {
        if (_failureTriggered || _phase == LockPhase.Completed || _phase == LockPhase.Failed)
            return;

        _stabilizationTimer = stabilizationRequiredTime;
        pressure = Mathf.Max(pressure, minPressure + debugCompletePressureBonus);
        temperature = Mathf.Min(temperature, maxTemperature - debugCompleteTemperatureBuffer);
        systemIntegrity = Mathf.Max(systemIntegrity, debugCompleteIntegrityFloor);
        waterLevel = waterLevelTarget;
        liftPower = liftPowerTarget;

        CompleteLevel();
        ApplyWaterVisual();
        lockUi?.Refresh();
    }

    public bool ProcessControl(LockControlAction action)
    {
        if (!CanReceiveInput || lockInputs == null)
            return false;

        switch (action)
        {
            case LockControlAction.SafeModeLever:
                lockInputs.ToggleSafeMode();
                return true;
            case LockControlAction.PrimaryButton:
                if (_phase == LockPhase.Stabilization)
                {
                    lockInputs.TogglePower();
                    return true;
                }

                if (_phase == LockPhase.WaterLeveling)
                    return TryStartWaterPrimaryFor();

                if (_phase == LockPhase.LiftPreparation)
                    return TryStartLiftPrimaryFor();

                return false;
            case LockControlAction.SecondaryButton:
                if (_phase == LockPhase.Stabilization)
                {
                    lockInputs.ToggleCooling();
                    return true;
                }

                if (_phase == LockPhase.WaterLeveling)
                    return TryStartWaterSecondaryFor();

                if (_phase == LockPhase.LiftPreparation)
                    return TryStartLiftSecondaryFor();

                return false;
            case LockControlAction.PumpHandle:
                return TryStartPumpFor5();
            default:
                return false;
        }
    }

    public bool TryStartPumpFor5()
    {
        if (!CanReceiveInput || _forRunning)
            return false;

        return StartForRoutine(
            label: PumpForCaption,
            iterations: pumpIterations,
            stepDelay: pumpStepDelay,
            requireWhile: false,
            phase2CriticalIfBroken: false,
            stepAction: _ => { pressure = Mathf.Clamp(pressure + pumpStepValue, 0f, 100f); },
            onCompleted: () =>
            {
                if (_activeIncident == IncidentType.PressureLeak)
                    ResolveIncident(pressureLeakResolvedMessage);
            });
    }

    public bool TryStartWaterPrimaryFor()
    {
        return TryStartWaterFor(WaterPrimaryIterations);
    }

    public bool TryStartWaterSecondaryFor()
    {
        return TryStartWaterFor(WaterSecondaryIterations);
    }

    public bool TryStartLiftPrimaryFor()
    {
        return TryStartLiftFor(LiftPrimaryIterations);
    }

    public bool TryStartLiftSecondaryFor()
    {
        return TryStartLiftFor(LiftSecondaryIterations);
    }

    public bool TryStartWaterFor(int iterations)
    {
        if (!CanReceiveInput || _phase != LockPhase.WaterLeveling || _forRunning)
            return false;

        int stepCount = Mathf.Max(1, iterations);

        return StartForRoutine(
            label: FormatForLabel(waterForLabelTemplate, stepCount),
            iterations: stepCount,
            stepDelay: waterStepDelay,
            requireWhile: true,
            phase2CriticalIfBroken: true,
            stepAction: _ =>
            {
                float direction = waterLevelTarget >= waterLevel ? 1f : -1f;
                float step = waterStepValue;
                if (_activeIncident == IncidentType.FlowSurge)
                {
                    float min = Mathf.Min(flowSurgeWaterStepRandomMin, flowSurgeWaterStepRandomMax);
                    float max = Mathf.Max(flowSurgeWaterStepRandomMin, flowSurgeWaterStepRandomMax);
                    step *= UnityEngine.Random.Range(min, max);
                }

                waterLevel = Mathf.Clamp(waterLevel + direction * step, 0f, 100f);
            });
    }

    public bool TryStartLiftFor(int iterations)
    {
        if (!CanReceiveInput || _phase != LockPhase.LiftPreparation || _forRunning)
            return false;

        int stepCount = Mathf.Max(1, iterations);
        float heatPerStep = stepCount >= 25 ? liftHeatPerBigStep : liftHeatPerSmallStep;
        float efficiency = _activeIncident == IncidentType.LiftJam ? liftJamEfficiencyMultiplier : 1f;

        return StartForRoutine(
            label: FormatForLabel(liftForLabelTemplate, stepCount),
            iterations: stepCount,
            stepDelay: liftStepDelay,
            requireWhile: true,
            phase2CriticalIfBroken: false,
            stepAction: _ =>
            {
                liftPower = Mathf.Clamp(liftPower + liftStepValue * efficiency, 0f, 100f);
                temperature = Mathf.Clamp(temperature + heatPerStep, 0f, 100f);
            },
            onCompleted: () =>
            {
                if (_activeIncident == IncidentType.LiftJam && stepCount >= liftJamResolveMinimumIterations && WhileActive)
                    ResolveIncident(liftJamResolvedMessage);
            });
    }

    public void TriggerFailure()
    {
        if (_failureTriggered)
            return;

        _failureTriggered = true;
        _phase = LockPhase.Failed;
        systemIntegrity = 0f;

        if (_activeForRoutine != null)
            StopCoroutine(_activeForRoutine);

        _forRunning = false;
        _forIteration = 0;
        _forTotal = 0;
        _forLabel = forInterruptedLabel;
        ClearIncidentState();

        lockInputs?.SetInputEnabled(false);

        if (alarmAudio != null)
            alarmAudio.Play();

        if (simpleShipController != null)
            simpleShipController.BeginSinking();

        if (_failureRoutine != null)
            StopCoroutine(_failureRoutine);

        _failureRoutine = StartCoroutine(FailureSequence());
    }

    private void UpdateBaseSimulation(float dt)
    {
        float threatPressureMultiplier = 1f + (_threatTier - 1) * 0.1f;
        pressure = Mathf.Clamp(pressure - pressureDrainPerSecond * threatPressureMultiplier * dt, 0f, 100f);

        if (CoolingEnabled)
        {
            float coolingMultiplier = _activeIncident == IncidentType.CoolingFault ? coolingFaultCoolingEfficiencyMultiplier : 1f;
            temperature = Mathf.Clamp(temperature - coolingPerSecond * coolingMultiplier * dt, 0f, 100f);
        }

        if (_phase == LockPhase.LiftPreparation && !CoolingEnabled)
            temperature = Mathf.Clamp(temperature + passiveHeatPerSecondPhase3 * dt, 0f, 100f);
    }

    private void ApplyActiveIncidentEffects(float dt)
    {
        if (_activeIncident == IncidentType.None)
            return;

        float incidentStrength = GetIncidentStrengthMultiplier();

        switch (_activeIncident)
        {
            case IncidentType.PressureLeak:
                pressure = Mathf.Clamp(pressure - pressureLeakExtraDrainPerSecond * incidentStrength * dt, 0f, 100f);
                systemIntegrity -= pressureLeakIntegrityDrainPerSecond * incidentStrength * dt;
                break;
            case IncidentType.CoolingFault:
                temperature = Mathf.Clamp(temperature + coolingFaultHeatPerSecond * incidentStrength * dt, 0f, 100f);
                break;
            case IncidentType.FlowSurge:
                if (_phase == LockPhase.WaterLeveling && !WhileActive)
                    systemIntegrity -= flowSurgeWhileBrokenDrainPerSecond * incidentStrength * dt;
                break;
            case IncidentType.LiftJam:
                if (_phase == LockPhase.LiftPreparation && !WhileActive)
                    systemIntegrity -= liftJamPassiveDrainPerSecond * incidentStrength * dt;
                break;
        }
    }

    private void UpdateStabilizationPhase(float dt)
    {
        if (WhileActive)
        {
            _stabilizationTimer += dt;
            systemIntegrity += integrityRecoverPerSecond * dt;
        }
        else
        {
            _stabilizationTimer = Mathf.Max(0f, _stabilizationTimer - stabilizationTimerLossPerSecond * dt);
            systemIntegrity -= integrityDrainPerSecond * dt;
        }

        if (_stabilizationTimer >= stabilizationRequiredTime && systemIntegrity >= requiredIntegrityForPhase2)
        {
            _phase = LockPhase.WaterLeveling;
            ScheduleNextIncident(isFirst: false, immediateBias: true);
        }
    }

    private void UpdateWaterPhase(float dt)
    {
        if (WhileActive)
            systemIntegrity += integrityRecoverPerSecond * waterPhaseIntegrityRecoveryMultiplier * dt;
        else
            systemIntegrity -= integrityDrainPerSecond * brokenWhileDrainMultiplier * dt;

        if (_phase2EmergencyTimer > 0f)
        {
            _phase2EmergencyTimer -= dt;
            systemIntegrity -= phase2EmergencyDrainPerSecond * dt;
        }

        if (Mathf.Abs(waterLevelTarget - waterLevel) <= waterTargetTolerance)
        {
            _phase = LockPhase.LiftPreparation;
            ScheduleNextIncident(isFirst: false, immediateBias: true);
        }
    }

    private void UpdateLiftPhase(float dt)
    {
        if (WhileActive)
            systemIntegrity += integrityRecoverPerSecond * liftPhaseIntegrityRecoveryMultiplier * dt;
        else
            systemIntegrity -= integrityDrainPerSecond * brokenWhileDrainMultiplier * dt;

        if (temperature > maxTemperature)
            systemIntegrity -= overheatIntegrityDrainPerSecond * dt;

        if (liftPower >= liftPowerTarget)
            CompleteLevel();
    }

    private bool StartForRoutine(
        string label,
        int iterations,
        float stepDelay,
        bool requireWhile,
        bool phase2CriticalIfBroken,
        Action<int> stepAction,
        Action onCompleted = null)
    {
        if (_forRunning)
            return false;

        _activeForRoutine = StartCoroutine(ForRoutine(label, iterations, stepDelay, requireWhile, phase2CriticalIfBroken, stepAction, onCompleted));
        return true;
    }

    private IEnumerator ForRoutine(
        string label,
        int iterations,
        float stepDelay,
        bool requireWhile,
        bool phase2CriticalIfBroken,
        Action<int> stepAction,
        Action onCompleted)
    {
        _forRunning = true;
        _forLabel = label;
        _forTotal = Mathf.Max(1, iterations);
        _forIteration = 0;
        bool completedAll = true;

        for (int i = 0; i < _forTotal; i++)
        {
            if (_failureTriggered)
            {
                completedAll = false;
                break;
            }

            if (requireWhile && !WhileActive)
            {
                completedAll = false;

                if (phase2CriticalIfBroken)
                {
                    _phase2EmergencyTimer = phase2EmergencyDuration;
                    systemIntegrity = Mathf.Clamp(systemIntegrity - phase2ImmediateHit, 0f, 100f);
                }

                break;
            }

            stepAction?.Invoke(i);
            _forIteration = i + 1;

            if (stepDelay > 0f)
                yield return new WaitForSeconds(stepDelay);
            else
                yield return null;
        }

        _forRunning = false;
        _forLabel = forIdleLabel;
        _forIteration = 0;
        _forTotal = 0;
        _activeForRoutine = null;

        if (completedAll)
            onCompleted?.Invoke();
    }

    private void UpdateIncidentSystem(float dt)
    {
        if (!enableIncidents || _failureTriggered || _phase == LockPhase.Completed || _phase == LockPhase.Failed)
            return;

        if (_activeIncident != IncidentType.None)
        {
            _incidentTimer -= dt;
            UpdateIncidentResolveProgress(dt);

            if (_incidentTimer <= 0f)
                HandleIncidentTimeout();

            return;
        }

        _nextIncidentTimer -= dt;
        if (_nextIncidentTimer > 0f)
            return;

        StartIncident(PickIncidentForPhase(_phase));
        ScheduleNextIncident(isFirst: false);
    }

    private void UpdateIncidentResolveProgress(float dt)
    {
        if (_activeIncident != IncidentType.FlowSurge)
            return;

        if (WhileActive)
            _incidentResolveProgress += dt;
        else
            _incidentResolveProgress = Mathf.Max(0f, _incidentResolveProgress - dt * flowSurgeResolveDecayPerSecond);

        float remaining = Mathf.Max(0f, flowSurgeStabilizeTime - _incidentResolveProgress);
        _incidentHint = string.Format(flowSurgeIncidentHintTemplate, remaining);

        if (_incidentResolveProgress >= flowSurgeStabilizeTime)
            ResolveIncident(flowSurgeResolvedMessage);
    }

    private void StartIncident(IncidentType incidentType)
    {
        if (incidentType == IncidentType.None)
            return;

        _activeIncident = incidentType;
        _incidentDuration = incidentBaseDuration + (_threatTier - 1) * incidentDurationPerThreatTier;
        _incidentTimer = _incidentDuration;
        _incidentResolveProgress = 0f;
        _coolingRestartRequiresOff = false;
        _coolingRestartRequiresOn = false;

        switch (incidentType)
        {
            case IncidentType.PressureLeak:
                _incidentLabel = pressureLeakIncidentLabel;
                _incidentHint = pressureLeakIncidentHint;
                break;
            case IncidentType.CoolingFault:
                _incidentLabel = coolingFaultIncidentLabel;
                _incidentHint = coolingFaultIncidentHint;
                _coolingRestartRequiresOff = true;
                break;
            case IncidentType.FlowSurge:
                _incidentLabel = flowSurgeIncidentLabel;
                _incidentHint = string.Format(flowSurgeIncidentHintTemplate, flowSurgeStabilizeTime);
                break;
            case IncidentType.LiftJam:
                _incidentLabel = liftJamIncidentLabel;
                _incidentHint = liftJamIncidentHint;
                break;
        }
    }

    private void ResolveIncident(string message)
    {
        if (_activeIncident == IncidentType.None)
            return;

        systemIntegrity = Mathf.Clamp(systemIntegrity + incidentResolveIntegrityBonus, 0f, 100f);
        ClearIncidentState();
        _incidentHint = message;
        _nextIncidentTimer = Mathf.Min(_nextIncidentTimer, UnityEngine.Random.Range(incidentResolvedDelayMin, incidentResolvedDelayMax));
    }

    private void HandleIncidentTimeout()
    {
        float strength = GetIncidentStrengthMultiplier();
        systemIntegrity = Mathf.Clamp(systemIntegrity - incidentTimeoutIntegrityHit * strength, 0f, 100f);

        switch (_activeIncident)
        {
            case IncidentType.PressureLeak:
                pressure = Mathf.Clamp(pressure - pressureLeakTimeoutPressureLoss, 0f, 100f);
                break;
            case IncidentType.CoolingFault:
                temperature = Mathf.Clamp(temperature + coolingFaultTimeoutHeatGain, 0f, 100f);
                break;
            case IncidentType.FlowSurge:
                waterLevel = Mathf.Clamp(waterLevel + UnityEngine.Random.Range(flowSurgeTimeoutWaterDeltaMin, flowSurgeTimeoutWaterDeltaMax), 0f, 100f);
                break;
            case IncidentType.LiftJam:
                liftPower = Mathf.Clamp(liftPower - liftJamTimeoutLiftPenalty, 0f, 100f);
                break;
        }

        ClearIncidentState();
        _incidentHint = incidentTimeoutHint;
        _nextIncidentTimer = UnityEngine.Random.Range(incidentTimeoutDelayMin, incidentTimeoutDelayMax);

        if (systemIntegrity <= 0f)
            TriggerFailure();
    }

    private void ClearIncidentState()
    {
        _activeIncident = IncidentType.None;
        _incidentTimer = 0f;
        _incidentDuration = 0f;
        _incidentResolveProgress = 0f;
        _incidentLabel = incidentLabelNone;
        _coolingRestartRequiresOff = false;
        _coolingRestartRequiresOn = false;
    }

    private IncidentType PickIncidentForPhase(LockPhase phase)
    {
        float roll = UnityEngine.Random.value;

        switch (phase)
        {
            case LockPhase.Stabilization:
                return roll < stabilizationPressureLeakChance ? IncidentType.PressureLeak : IncidentType.CoolingFault;
            case LockPhase.WaterLeveling:
                return roll < waterPressureLeakChance ? IncidentType.PressureLeak : IncidentType.FlowSurge;
            case LockPhase.LiftPreparation:
                if (roll < liftPressureLeakChance)
                    return IncidentType.PressureLeak;
                if (roll < liftCoolingFaultChance)
                    return IncidentType.CoolingFault;
                return IncidentType.LiftJam;
            default:
                return IncidentType.None;
        }
    }

    private void ScheduleNextIncident(bool isFirst, bool immediateBias = false)
    {
        if (!enableIncidents)
        {
            _nextIncidentTimer = float.PositiveInfinity;
            return;
        }

        float min = isFirst ? firstIncidentDelayMin : incidentDelayMin;
        float max = isFirst ? firstIncidentDelayMax : incidentDelayMax;
        float tierReduction = (_threatTier - 1) * incidentTierIntervalReductionSeconds;
        min = Mathf.Max(incidentMinDelayFloor, min - tierReduction);
        max = Mathf.Max(min + 1f, max - tierReduction * incidentTierIntervalMaxReductionFactor);

        _nextIncidentTimer = UnityEngine.Random.Range(min, max);
        if (immediateBias)
            _nextIncidentTimer = Mathf.Min(_nextIncidentTimer, UnityEngine.Random.Range(immediateIncidentDelayMin, immediateIncidentDelayMax));
    }

    private void UpdateThreatTier()
    {
        int computedTier = 1 + Mathf.FloorToInt(_sessionTimer / Mathf.Max(threatTierStepSecondsFloor, threatTierStepSeconds));
        _threatTier = Mathf.Clamp(computedTier, 1, maxThreatTier);
    }

    private void HandleInputStateChanges()
    {
        if (lockInputs == null)
            return;

        bool coolingState = lockInputs.CoolingEnabled;

        if (coolingState != _previousCoolingState)
            OnCoolingStateChanged(coolingState);

        _previousCoolingState = coolingState;
    }

    private void OnCoolingStateChanged(bool isEnabled)
    {
        if (_activeIncident != IncidentType.CoolingFault)
            return;

        if (_coolingRestartRequiresOff && !isEnabled)
        {
            _coolingRestartRequiresOff = false;
            _coolingRestartRequiresOn = true;
            _incidentHint = coolingRestartHint;
            return;
        }

        if (_coolingRestartRequiresOn && isEnabled)
            ResolveIncident(coolingRestartResolvedMessage);
    }

    private void CompleteLevel()
    {
        if (_phase == LockPhase.Completed)
            return;

        _phase = LockPhase.Completed;
        _gateOpening = true;

        if (_activeForRoutine != null)
            StopCoroutine(_activeForRoutine);

        _forRunning = false;
        _forLabel = forIdleLabel;
        _forIteration = 0;
        _forTotal = 0;
        ClearIncidentState();

        lockInputs?.SetInputEnabled(false);

        if (simpleShipController != null)
            simpleShipController.BeginExit();
    }

    private bool IsWhileConditionTrue()
    {
        if (lockInputs == null)
            return false;

        return lockInputs.SafeModeEnabled && lockInputs.PowerEnabled && lockInputs.CoolingEnabled && pressure > minPressure;
    }

    private float GetIncidentStrengthMultiplier()
    {
        return 1f + (_threatTier - 1) * threatStrengthPerTier;
    }

    private float CalculateSessionProgress()
    {
        switch (_phase)
        {
            case LockPhase.Stabilization:
                return stabilizationProgressWeight * StabilizationProgress;
            case LockPhase.WaterLeveling:
            {
                float normalizedDistance = Mathf.Clamp01(Mathf.Abs(waterLevelTarget - waterLevel) / Mathf.Max(1f, waterLevelTarget));
                return stabilizationProgressWeight + waterLevelingProgressWeight * (1f - normalizedDistance);
            }
            case LockPhase.LiftPreparation:
            {
                float liftProgressWeight = Mathf.Clamp01(1f - stabilizationProgressWeight - waterLevelingProgressWeight);
                return stabilizationProgressWeight + waterLevelingProgressWeight + liftProgressWeight * Mathf.Clamp01(liftPower / Mathf.Max(1f, liftPowerTarget));
            }
            case LockPhase.Completed:
                return 1f;
            default:
                return 0f;
        }
    }

    private static string FormatForLabel(string template, int iterations)
    {
        if (string.IsNullOrWhiteSpace(template))
            return $"FOR x{Mathf.Max(1, iterations)}";

        return string.Format(template, Mathf.Max(1, iterations));
    }

    private bool IsDebugControlAllowed()
    {
        if (!showDebugCompleteButton)
            return false;

        return Application.isEditor || allowDebugButtonInBuild;
    }

    private void HandleDebugHotkey()
    {
        if (!IsDebugControlAllowed())
            return;

        if (Input.GetKeyDown(debugCompleteHotkey))
            DebugCompleteLevel();
    }

    private void ClampCoreValues()
    {
        systemIntegrity = Mathf.Clamp(systemIntegrity, 0f, 100f);
        pressure = Mathf.Clamp(pressure, 0f, 100f);
        temperature = Mathf.Clamp(temperature, 0f, 100f);
        waterLevel = Mathf.Clamp(waterLevel, 0f, 100f);
        liftPower = Mathf.Clamp(liftPower, 0f, 100f);
    }

    private void UpdateGate(float dt)
    {
        if (!_gatePoseInitialized || gateTransform == null)
            return;

        Vector3 targetPosition = _gateOpening ? _gateOpenLocalPosition : _gateClosedLocalPosition;
        Quaternion targetRotation = _gateOpening ? _gateOpenLocalRotation : _gateClosedLocalRotation;

        gateTransform.localPosition = Vector3.MoveTowards(gateTransform.localPosition, targetPosition, gateOpenSpeed * dt);
        gateTransform.localRotation = Quaternion.RotateTowards(gateTransform.localRotation, targetRotation, gateOpenSpeed * 45f * dt);
    }

    private void ApplyWaterVisual()
    {
        if (waterTransform == null)
            return;

        float normalized = Mathf.Clamp01(waterLevel / 100f);
        Vector3 position = waterTransform.position;
        position.y = Mathf.Lerp(waterMinY, waterMaxY, normalized);
        waterTransform.position = position;
    }

    private IEnumerator FailureSequence()
    {
        float timer = 0f;

        while (timer < restartDelay)
        {
            timer += Time.deltaTime;
            waterLevel = Mathf.Clamp(waterLevel + failureWaterRisePerSecond * Time.deltaTime, 0f, 100f);
            ApplyWaterVisual();
            lockUi?.Refresh();
            yield return null;
        }

        ReloadCurrentLevel();
    }

    private void ReloadCurrentLevel()
    {
        if (!string.IsNullOrWhiteSpace(reloadSceneName) && Application.CanStreamedLevelBeLoaded(reloadSceneName))
        {
            SceneManager.LoadScene(reloadSceneName);
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void ResolveAndApplyConfig()
    {
        if (levelConfig == null && !string.IsNullOrWhiteSpace(fallbackConfigResourcePath))
            levelConfig = Resources.Load<LockLevelConfig>(fallbackConfigResourcePath);

        if (!applyConfigOnAwake || levelConfig == null)
            return;

        levelConfig.ApplyTo(this);
    }

    private void InitializeRuntimeLabels()
    {
        _forLabel = forIdleLabel;
        _incidentLabel = incidentLabelNone;
    }

    private void ResolveLocalReferences()
    {
        if (lockInputs == null)
            lockInputs = GetComponent<LockInputs>();

        if (lockUi == null)
            lockUi = GetComponent<LockUI>();

        if (lockUi == null)
            lockUi = FindFirstObjectByType<LockUI>();
    }

    private void ResolveReferences()
    {
        if (waterTransform == null)
        {
            GameObject waterObject = GameObject.Find("Water");
            if (waterObject != null)
                waterTransform = waterObject.transform;
        }

        if (simpleShipController == null)
            simpleShipController = FindFirstObjectByType<SimpleShipController>();

        if (simpleShipController != null)
        {
            ShipController legacyShip = simpleShipController.GetComponent<ShipController>();
            if (legacyShip != null)
                legacyShip.enabled = false;

            if (shipHoldPoint != null || shipExitPoint != null)
                simpleShipController.SetRoute(shipHoldPoint, shipExitPoint);
        }
    }

    private void StartShipIfReady()
    {
        if (_shipStarted || simpleShipController == null)
            return;

        simpleShipController.BeginApproach();
        _shipStarted = true;
    }

    private void InitializeGatePose()
    {
        if (gateTransform == null)
            return;

        _gateClosedLocalPosition = gateTransform.localPosition;
        _gateClosedLocalRotation = gateTransform.localRotation;
        _gateOpenLocalPosition = _gateClosedLocalPosition + gateOpenOffset;
        _gateOpenLocalRotation = _gateClosedLocalRotation * Quaternion.Euler(gateOpenEulerOffset);
        _gatePoseInitialized = true;
    }

    private void SnapshotInputStates()
    {
        if (lockInputs == null)
            return;

        _previousCoolingState = lockInputs.CoolingEnabled;
    }
}
