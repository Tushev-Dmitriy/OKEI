using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LockControlSystem : MonoBehaviour
{
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
    [SerializeField, Range(0f, 100f)] private float systemIntegrity = 70f;
    [SerializeField, Range(0f, 100f)] private float pressure = 72f;
    [SerializeField, Range(0f, 100f)] private float temperature = 18f;
    [SerializeField, Range(0f, 100f)] private float waterLevel = 0f;
    [SerializeField, Range(0f, 100f)] private float liftPower = 0f;

    [Header("Targets")]
    [SerializeField, Range(0f, 100f)] private float minPressure = 45f;
    [SerializeField, Range(0f, 100f)] private float maxTemperature = 82f;
    [SerializeField, Range(0f, 100f)] private float waterLevelTarget = 80f;
    [SerializeField, Range(0f, 100f)] private float liftPowerTarget = 100f;
    [SerializeField] private float waterTargetTolerance = 1f;

    [Header("Phase 1 / Stabilization")]
    [SerializeField] private float stabilizationRequiredTime = 180f;
    [SerializeField] private float requiredIntegrityForPhase2 = 60f;
    [SerializeField] private float stabilizationTimerLossPerSecond = 1f;

    [Header("Core Simulation")]
    [SerializeField] private float pressureDrainPerSecond = 1.15f;
    [SerializeField] private float coolingPerSecond = 3.25f;
    [SerializeField] private float passiveHeatPerSecondPhase3 = 0.9f;
    [SerializeField] private float integrityRecoverPerSecond = 0.85f;
    [SerializeField] private float integrityDrainPerSecond = 5f;
    [SerializeField] private float brokenWhileDrainMultiplier = 2.2f;

    [Header("FOR Actions")]
    [SerializeField] private int pumpIterations = 5;
    [SerializeField] private float pumpStepValue = 4f;
    [SerializeField] private float pumpStepDelay = 0.3f;
    [SerializeField] private float waterStepValue = 0.45f;
    [SerializeField] private float waterStepDelay = 0.38f;
    [SerializeField] private float liftStepValue = 0.35f;
    [SerializeField] private float liftStepDelay = 0.2f;
    [SerializeField] private float liftHeatPerSmallStep = 0.3f;
    [SerializeField] private float liftHeatPerBigStep = 0.85f;

    [Header("Safety Penalties")]
    [SerializeField] private float phase2EmergencyDuration = 3f;
    [SerializeField] private float phase2EmergencyDrainPerSecond = 22f;
    [SerializeField] private float phase2ImmediateHit = 18f;
    [SerializeField] private float overheatIntegrityDrainPerSecond = 18f;

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

    private LockPhase _phase = LockPhase.Stabilization;
    private Coroutine _activeForRoutine;
    private Coroutine _failureRoutine;

    private float _stabilizationTimer;
    private float _phase2EmergencyTimer;

    private bool _failureTriggered;
    private bool _gateOpening;
    private bool _gatePoseInitialized;
    private bool _shipStarted;

    private string _forLabel = "FOR: ожидание";
    private int _forIteration;
    private int _forTotal;
    private bool _forRunning;

    private Vector3 _gateClosedLocalPosition;
    private Vector3 _gateOpenLocalPosition;
    private Quaternion _gateClosedLocalRotation;
    private Quaternion _gateOpenLocalRotation;

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

    public bool IsForRunning => _forRunning;
    public int ForIteration => _forIteration;
    public int ForTotal => _forTotal;
    public string ForLabel => _forLabel;

    public bool PowerEnabled => lockInputs != null && lockInputs.PowerEnabled;
    public bool CoolingEnabled => lockInputs != null && lockInputs.CoolingEnabled;
    public bool SafeModeEnabled => lockInputs != null && lockInputs.SafeModeEnabled;

    private void Awake()
    {
        ResolveLocalReferences();
        if (lockInputs != null)
            lockInputs.SetSystem(this);

        ResolveReferences();
        InitializeGatePose();
        lockUi?.Initialize(this, lockInputs);
        lockUi?.Refresh();
    }

    private void Start()
    {
        StartShipIfReady();
        ApplyWaterVisual();
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

        // WHILE support values that constantly change.
        pressure = Mathf.Clamp(pressure - pressureDrainPerSecond * dt, 0f, 100f);

        if (CoolingEnabled)
            temperature = Mathf.Clamp(temperature - coolingPerSecond * dt, 0f, 100f);

        if (_phase == LockPhase.LiftPreparation && !CoolingEnabled)
            temperature = Mathf.Clamp(temperature + passiveHeatPerSecondPhase3 * dt, 0f, 100f);

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
            case LockPhase.Completed:
                break;
            case LockPhase.Failed:
                break;
        }

        systemIntegrity = Mathf.Clamp(systemIntegrity, 0f, 100f);

        if (systemIntegrity <= 0f)
            TriggerFailure();

        UpdateGate(dt);
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
                    return TryStartWaterFor(10);

                if (_phase == LockPhase.LiftPreparation)
                    return TryStartLiftFor(25);

                return false;
            case LockControlAction.SecondaryButton:
                if (_phase == LockPhase.Stabilization)
                {
                    lockInputs.ToggleCooling();
                    return true;
                }

                if (_phase == LockPhase.WaterLeveling)
                    return TryStartWaterFor(5);

                if (_phase == LockPhase.LiftPreparation)
                    return TryStartLiftFor(10);

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

        return StartForRoutine("Подкачка FOR x5", pumpIterations, pumpStepDelay, false, false, i =>
        {
            pressure = Mathf.Clamp(pressure + pumpStepValue, 0f, 100f);
        });
    }

    public bool TryStartWaterFor(int iterations)
    {
        if (!CanReceiveInput || _phase != LockPhase.WaterLeveling || _forRunning)
            return false;

        int stepCount = Mathf.Max(1, iterations);

        return StartForRoutine($"Вода FOR x{stepCount}", stepCount, waterStepDelay, true, true, i =>
        {
            float direction = waterLevelTarget >= waterLevel ? 1f : -1f;
            waterLevel = Mathf.Clamp(waterLevel + direction * waterStepValue, 0f, 100f);
        });
    }

    public bool TryStartLiftFor(int iterations)
    {
        if (!CanReceiveInput || _phase != LockPhase.LiftPreparation || _forRunning)
            return false;

        int stepCount = Mathf.Max(1, iterations);
        float heatPerStep = stepCount >= 25 ? liftHeatPerBigStep : liftHeatPerSmallStep;

        return StartForRoutine($"Подъем FOR x{stepCount}", stepCount, liftStepDelay, true, false, i =>
        {
            liftPower = Mathf.Clamp(liftPower + liftStepValue, 0f, 100f);
            temperature = Mathf.Clamp(temperature + heatPerStep, 0f, 100f);
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
        _forLabel = "FOR: прерван";

        lockInputs?.SetInputEnabled(false);

        if (alarmAudio != null)
            alarmAudio.Play();

        if (simpleShipController != null)
            simpleShipController.BeginSinking();

        if (_failureRoutine != null)
            StopCoroutine(_failureRoutine);

        _failureRoutine = StartCoroutine(FailureSequence());
    }

    private void UpdateStabilizationPhase(float dt)
    {
        // WHILE
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
            _phase = LockPhase.WaterLeveling;
    }

    private void UpdateWaterPhase(float dt)
    {
        // WHILE
        if (WhileActive)
            systemIntegrity += integrityRecoverPerSecond * 0.35f * dt;
        else
            systemIntegrity -= integrityDrainPerSecond * brokenWhileDrainMultiplier * dt;

        if (_phase2EmergencyTimer > 0f)
        {
            _phase2EmergencyTimer -= dt;
            systemIntegrity -= phase2EmergencyDrainPerSecond * dt;
        }

        if (Mathf.Abs(waterLevelTarget - waterLevel) <= waterTargetTolerance)
            _phase = LockPhase.LiftPreparation;
    }

    private void UpdateLiftPhase(float dt)
    {
        // WHILE
        if (WhileActive)
            systemIntegrity += integrityRecoverPerSecond * 0.3f * dt;
        else
            systemIntegrity -= integrityDrainPerSecond * brokenWhileDrainMultiplier * dt;

        if (temperature > maxTemperature)
            systemIntegrity -= overheatIntegrityDrainPerSecond * dt;

        if (liftPower >= liftPowerTarget)
            CompleteLevel();
    }

    private bool StartForRoutine(string label, int iterations, float stepDelay, bool requireWhile, bool phase2CriticalIfBroken, Action<int> stepAction)
    {
        if (_forRunning)
            return false;

        _activeForRoutine = StartCoroutine(ForRoutine(label, iterations, stepDelay, requireWhile, phase2CriticalIfBroken, stepAction));
        return true;
    }

    private IEnumerator ForRoutine(string label, int iterations, float stepDelay, bool requireWhile, bool phase2CriticalIfBroken, Action<int> stepAction)
    {
        _forRunning = true;
        _forLabel = label;
        _forTotal = Mathf.Max(1, iterations);
        _forIteration = 0;

        for (int i = 0; i < _forTotal; i++)
        {
            // FOR
            if (_failureTriggered)
                break;

            if (requireWhile && !WhileActive)
            {
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
        _forLabel = "FOR: ожидание";
        _forIteration = 0;
        _forTotal = 0;
        _activeForRoutine = null;
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

    private void CompleteLevel()
    {
        if (_phase == LockPhase.Completed)
            return;

        _phase = LockPhase.Completed;
        _gateOpening = true;
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

    private void ResolveLocalReferences()
    {
        if (lockInputs == null)
            lockInputs = GetComponent<LockInputs>();

        if (lockUi == null)
            lockUi = GetComponent<LockUI>();
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
        if (_shipStarted)
            return;

        if (simpleShipController == null)
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
}
