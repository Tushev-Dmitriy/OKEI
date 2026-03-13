using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LockControlSystem : MonoBehaviour
{
    [SerializeField] private LockLevelConfig levelConfig;
    [SerializeField] private string defaultConfigResourcePath = "Level2/LockLevelConfig_Default";

    [SerializeField] private LockInputs lockInputs;
    [SerializeField] private LockUI lockUi;
    [SerializeField] private ShipController shipController;
    [SerializeField] private Transform gateTransform;
    [SerializeField] private Transform lockWaterTransform;
    [SerializeField] private Transform outsideWaterTransform;
    [SerializeField] private AudioSource alarmAudio;

    private string reloadSceneName = "Level2";

    private float systemIntegrity = 74f;
    private float pressure = 72f;
    private float temperature = 18f;
    private float waterLevel;
    private float liftPower;

    private float minPressure = 45f;
    private float maxTemperature = 82f;
    private float waterLevelTarget = 80f;
    private float liftPowerTarget = 100f;
    private float waterTargetTolerance = 1f;

    private float stabilizationRequiredTime = 110f;
    private float requiredIntegrityForPhase2 = 60f;
    private float stabilizationTimerLossPerSecond = 1f;

    private float pressureDrainPerSecond = 1.15f;
    private float coolingPerSecond = 3.25f;
    private float passiveHeatPerSecondPhase3 = 0.9f;
    private float integrityRecoverPerSecond = 0.9f;
    private float integrityDrainPerSecond = 5f;
    private float brokenWhileDrainMultiplier = 2.2f;
    private float waterPhaseIntegrityRecoveryMultiplier = 0.35f;
    private float liftPhaseIntegrityRecoveryMultiplier = 0.3f;

    private int pumpIterations = 5;
    private float pumpStepValue = 4f;
    private float pumpStepDelay = 0.3f;
    private float waterStepValue = 0.75f;
    private float waterStepDelay = 0.38f;
    private float liftStepValue = 0.75f;
    private float liftStepDelay = 0.2f;
    private float liftHeatPerSmallStep = 0.3f;
    private float liftHeatPerBigStep = 0.85f;
    private int waterPrimaryIterations = 10;
    private int waterSecondaryIterations = 5;
    private int liftPrimaryIterations = 25;
    private int liftSecondaryIterations = 10;
    private float flowSurgeWaterStepRandomMin = 0.55f;
    private float flowSurgeWaterStepRandomMax = 1.3f;

    private float phase2EmergencyDuration = 3f;
    private float phase2EmergencyDrainPerSecond = 22f;
    private float phase2ImmediateHit = 18f;
    private float overheatIntegrityDrainPerSecond = 18f;

    private bool enableIncidents = true;
    private float firstIncidentDelayMin = 16f;
    private float firstIncidentDelayMax = 24f;
    private float incidentDelayMin = 20f;
    private float incidentDelayMax = 34f;
    private float incidentBaseDuration = 14f;
    private float incidentDurationPerThreatTier = 2.5f;
    private float threatTierStepSeconds = 85f;
    private int maxThreatTier = 5;
    private float threatStrengthPerTier = 0.22f;
    private float incidentTimeoutIntegrityHit = 10f;
    private float incidentResolveIntegrityBonus = 1.5f;
    private float stabilizationPressureLeakChance = 0.6f;
    private float waterPressureLeakChance = 0.5f;
    private float liftPressureLeakChance = 0.34f;
    private float liftCoolingFaultChance = 0.68f;
    private float flowSurgeResolveDecayPerSecond = 0.5f;
    private float incidentResolvedDelayMin = 12f;
    private float incidentResolvedDelayMax = 18f;
    private float incidentTimeoutDelayMin = 8f;
    private float incidentTimeoutDelayMax = 14f;
    private float immediateIncidentDelayMin = 8f;
    private float immediateIncidentDelayMax = 12f;
    private float incidentTierIntervalReductionSeconds = 2.5f;
    private float incidentTierIntervalMaxReductionFactor = 0.8f;
    private float incidentMinDelayFloor = 7f;
    private float threatTierStepSecondsFloor = 20f;

    private float pressureLeakExtraDrainPerSecond = 2.9f;
    private float pressureLeakIntegrityDrainPerSecond = 0.7f;
    private float coolingFaultHeatPerSecond = 1.25f;
    private float coolingFaultCoolingEfficiencyMultiplier = 0.35f;
    private float flowSurgeWhileBrokenDrainPerSecond = 3.4f;
    private float flowSurgeStabilizeTime = 4f;
    private float liftJamPassiveDrainPerSecond = 1.5f;
    private float liftJamEfficiencyMultiplier = 0.55f;
    private int liftJamResolveMinimumIterations = 10;
    private float liftJamTimeoutLiftPenalty = 14f;
    private float pressureLeakTimeoutPressureLoss = 12f;
    private float coolingFaultTimeoutHeatGain = 12f;
    private float flowSurgeTimeoutWaterDeltaMin = -8f;
    private float flowSurgeTimeoutWaterDeltaMax = 8f;

    private float failureWaterRisePerSecond = 28f;
    private float restartDelay = 4f;

    private float waterMinY = 3f;
    private float waterMaxY = 14f;

    private Vector3 gateOpenOffset = new Vector3(0f, -255f, 0f);
    private Vector3 gateOpenEulerOffset;
    private float gateOpenSpeed = 2.5f;
    private float gateDropDistance = 55f;

    private float stabilizationProgressWeight = 0.35f;
    private float waterLevelingProgressWeight = 0.30f;

    private string forIdleLabel = "FOR: ожидание";
    private string forInterruptedLabel = "FOR: прерван";
    private string pumpForLabelTemplate = "Подкачка FOR x{0}";
    private string waterForLabelTemplate = "Вода FOR x{0}";
    private string liftForLabelTemplate = "Подъем FOR x{0}";
    private string incidentLabelNone = "Нет";
    private string pressureLeakIncidentLabel = "Утечка давления";
    private string coolingFaultIncidentLabel = "Сбой охлаждения";
    private string flowSurgeIncidentLabel = "Турбулентность потока";
    private string liftJamIncidentLabel = "Заклинивание подъема";

    private string pressureLeakIncidentHint = "Сделайте подкачку FOR x5 один раз.";
    private string coolingFaultIncidentHint = "Выключите охлаждение и включите снова.";
    private string flowSurgeIncidentHintTemplate = "Удерживайте WHILE {0:0.0}с";
    private string liftJamIncidentHint = "Сделайте Подъем FOR x10+ при активном WHILE.";
    private string coolingRestartHint = "Включите охлаждение снова для завершения перезапуска.";
    private string pressureLeakResolvedMessage = "Утечка устранена циклом подкачки.";
    private string flowSurgeResolvedMessage = "Поток стабилизирован.";
    private string liftJamResolvedMessage = "Заклинивание снято силовым циклом.";
    private string coolingRestartResolvedMessage = "Контур охлаждения перезапущен.";
    private string incidentTimeoutHint = "Время инцидента вышло. Системы повреждены.";

    private bool showDebugCompleteButton = true;
    private bool allowDebugButtonInBuild;
    private Rect debugCompleteButtonRect = new Rect(20f, 20f, 220f, 36f);
    private KeyCode debugCompleteHotkey = KeyCode.F8;
    private string debugCompleteButtonLabel = "DEBUG: Пройти уровень";
    private float debugCompletePressureBonus = 5f;
    private float debugCompleteTemperatureBuffer = 5f;
    private float debugCompleteIntegrityFloor = 65f;

    private LockPhase _phase = LockPhase.Stabilization;
    private Coroutine _activeForRoutine;
    private Coroutine _failureRoutine;

    private float _stabilizationTimer;
    private float _phase2EmergencyTimer;

    private bool _failureTriggered;
    private bool _gateOpening;
    private bool _gatePoseInitialized;
    private bool _gameplayStarted;

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
    private bool _lockWaterStartCaptured;
    private float _lockWaterStartY;

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
    public bool CanReceiveInput => _gameplayStarted && !_failureTriggered && _phase != LockPhase.Completed && _phase != LockPhase.Failed;

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
        ApplyWaterVisual();

        if (shipController == null)
        {
            StartGameplay();
            return;
        }

        lockInputs?.SetInputEnabled(false);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (!_gameplayStarted)
        {
            TryStartGameplayAfterShipDocked();
            UpdateGate(dt);
            ApplyWaterVisual();
            lockUi?.Refresh();
            return;
        }

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

        shipController?.SinkShip();

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

        AlignLockWaterWithOutside();
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
        shipController?.MoveToEnd();

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
        if (lockWaterTransform == null)
            return;

        if (!_lockWaterStartCaptured)
        {
            _lockWaterStartY = lockWaterTransform.position.y;
            _lockWaterStartCaptured = true;
        }

        float targetY;
        if (outsideWaterTransform != null)
        {
            float progress = Mathf.Clamp01(SessionProgress);
            targetY = Mathf.Lerp(_lockWaterStartY, outsideWaterTransform.position.y, progress);
        }
        else
        {
            float normalized = Mathf.Clamp01(waterLevel / 100f);
            targetY = Mathf.Lerp(waterMinY, waterMaxY, normalized);
        }

        Vector3 position = lockWaterTransform.position;
        position.y = targetY;
        lockWaterTransform.position = position;
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
        if (levelConfig == null && !string.IsNullOrWhiteSpace(defaultConfigResourcePath))
            levelConfig = Resources.Load<LockLevelConfig>(defaultConfigResourcePath);

        if (levelConfig == null)
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
        if (lockWaterTransform == null)
        {
            GameObject waterObject = GameObject.Find("Water");
            if (waterObject != null)
                lockWaterTransform = waterObject.transform;
        }

        if (outsideWaterTransform == null)
        {
            GameObject outsideWaterObject = GameObject.Find("OutsideWater");
            if (outsideWaterObject != null)
                outsideWaterTransform = outsideWaterObject.transform;
        }

        if (shipController == null)
            shipController = FindFirstObjectByType<ShipController>();

        shipController?.SetLockWaterTransform(lockWaterTransform);
    }

    private void TryStartGameplayAfterShipDocked()
    {
        if (shipController != null && !shipController.HasReachedStop)
            return;

        StartGameplay();
    }

    private void StartGameplay()
    {
        if (_gameplayStarted)
            return;

        _gameplayStarted = true;
        lockInputs?.SetInputEnabled(true);
        ScheduleNextIncident(isFirst: true);
    }

    private void InitializeGatePose()
    {
        if (gateTransform == null)
            return;

        _gateClosedLocalPosition = gateTransform.localPosition;
        _gateClosedLocalRotation = gateTransform.localRotation;
        Vector3 openOffset = gateOpenOffset;
        openOffset.y = -Mathf.Abs(gateDropDistance);
        _gateOpenLocalPosition = _gateClosedLocalPosition + openOffset;
        _gateOpenLocalRotation = _gateClosedLocalRotation * Quaternion.Euler(gateOpenEulerOffset);
        _gatePoseInitialized = true;
    }

    private void AlignLockWaterWithOutside()
    {
        if (lockWaterTransform == null || outsideWaterTransform == null)
            return;

        Vector3 position = lockWaterTransform.position;
        position.y = outsideWaterTransform.position.y;
        lockWaterTransform.position = position;
    }

    private void SnapshotInputStates()
    {
        if (lockInputs == null)
            return;

        _previousCoolingState = lockInputs.CoolingEnabled;
    }
}
