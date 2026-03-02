using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LockControlSystem : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private AllBridgeController bridgeController;
    [SerializeField] private ShipController shipController;
    [SerializeField] private Transform waterTransform;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private AudioSource alarmAudio;

    [Header("Control Objects (optional scene controls)")]
    [SerializeField] private string safeModeObjectName = "lever";
    [SerializeField] private string powerObjectName = "bigbutton";
    [SerializeField] private string coolingObjectName = "button";
    [SerializeField] private string pumpObjectName = "handle";
    [SerializeField] private bool useSceneInteractables;

    [Header("Core Params (0-100)")]
    [SerializeField, Range(0f, 100f)] private float systemIntegrity = 55f;
    [SerializeField, Range(0f, 100f)] private float pressure = 80f;
    [SerializeField, Range(0f, 100f)] private float temperature = 15f;
    [SerializeField, Range(0f, 100f)] private float waterLevel = 0f;
    [SerializeField, Range(0f, 100f)] private float liftPower = 0f;

    [Header("Requested Tunables")]
    [SerializeField] private float integrityDrainSpeed = 2f;
    [SerializeField] private float integrityRecoverSpeed = 1.5f;
    [SerializeField] private float pressureDrainSpeed = 0.3f;
    [SerializeField] private float temperatureIncreasePerBigFor = 9f;
    [SerializeField] private float temperatureCoolSpeed = 3f;
    [SerializeField, Range(0f, 100f)] private float liftPowerTarget = 100f;
    [SerializeField, Range(0f, 100f)] private float waterLevelTarget = 100f;

    [Header("Thresholds")]
    [SerializeField] private float minPressure = 20f;
    [SerializeField] private float maxTemperature = 85f;
    [SerializeField] private float stabilizationHoldTime = 35f;

    [Header("FOR Step Values")]
    [SerializeField] private float pressureStep = 2.4f;
    [SerializeField] private float waterStep = 0.25f;
    [SerializeField] private float liftStep = 0.12f;
    [SerializeField] private float smallLiftHeatFactor = 0.45f;

    [Header("Failure Tuning")]
    [SerializeField] private float brokenWhileDrainMultiplier = 2.4f;
    [SerializeField] private float overheatIntegrityDrain = 10f;
    [SerializeField] private float waterUnsafeFailureDelay = 6f;
    [SerializeField] private float failureWaterRiseSpeed = 28f;
    [SerializeField] private float restartDelay = 4f;

    [Header("Water Visual")]
    [SerializeField] private float waterMinY = 3f;
    [SerializeField] private float waterMaxY = 14f;

    [Header("Simple UI")]
    [SerializeField] private bool showTestHints = true;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool showControlPanel = true;
    [SerializeField] private Rect panelRect = new Rect(16f, 16f, 440f, 480f);

    private LockPhase _phase = LockPhase.Stabilization;
    private bool _powerEnabled;
    private bool _coolingEnabled;
    private bool _safeModeEnabled;
    private bool _failureTriggered;
    private float _stabilizationTimer;
    private float _waterUnsafeTimer;

    private Coroutine _failureRoutine;

    public bool CanInteract => _phase != LockPhase.Completed && _phase != LockPhase.Failed && !_failureTriggered;
    public LockPhase CurrentPhase => _phase;
    public bool WhileActive => _safeModeEnabled && _powerEnabled && _coolingEnabled && pressure > minPressure;

    private void Awake()
    {
        EnsureRuntimeControls();
        if (statusText == null)
            statusText = GameObject.Find("ActionInfoText")?.GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (_phase == LockPhase.Completed || _phase == LockPhase.Failed)
        {
            UpdateUI();
            return;
        }

        float dt = Time.deltaTime;
        pressure = Mathf.Clamp(pressure - pressureDrainSpeed * dt, 0f, 100f);

        if (_coolingEnabled)
            temperature = Mathf.Clamp(temperature - temperatureCoolSpeed * dt, 0f, 100f);

        // while (...) condition for system stability
        bool whileActive = WhileActive;

        switch (_phase)
        {
            case LockPhase.Stabilization:
                UpdateStabilization(whileActive, dt);
                break;
            case LockPhase.WaterLeveling:
                UpdateWaterLeveling(whileActive, dt);
                break;
            case LockPhase.LiftPreparation:
                UpdateLiftPreparation(whileActive, dt);
                break;
        }

        ApplyWaterVisual();
        UpdateBridgeProgress();
        UpdateUI();

        if (systemIntegrity <= 0f && !_failureTriggered)
            TriggerFailure();
    }

    public bool ProcessControl(LockControlAction action)
    {
        if (!CanInteract)
            return false;

        switch (action)
        {
            case LockControlAction.SafeModeLever:
                _safeModeEnabled = !_safeModeEnabled;
                return true;
            case LockControlAction.PrimaryButton:
                if (_phase == LockPhase.Stabilization)
                {
                    _powerEnabled = !_powerEnabled;
                    return true;
                }
                if (_phase == LockPhase.WaterLeveling)
                    return DoWaterFor(10);
                if (_phase == LockPhase.LiftPreparation)
                    return DoLiftFor(25, temperatureIncreasePerBigFor);
                return false;
            case LockControlAction.SecondaryButton:
                if (_phase == LockPhase.Stabilization)
                {
                    _coolingEnabled = !_coolingEnabled;
                    return true;
                }
                if (_phase == LockPhase.WaterLeveling)
                    return DoWaterFor(5);
                if (_phase == LockPhase.LiftPreparation)
                    return DoLiftFor(10, temperatureIncreasePerBigFor * smallLiftHeatFactor);
                return false;
            case LockControlAction.PumpHandle:
                DoPumpForFive();
                return true;
            default:
                return false;
        }
    }

    public void TriggerFailure()
    {
        if (_failureTriggered)
            return;

        _failureTriggered = true;
        _phase = LockPhase.Failed;
        systemIntegrity = 0f;

        if (playerInteractor != null)
            playerInteractor.enabled = false;

        if (alarmAudio != null)
            alarmAudio.Play();

        if (shipController != null)
            shipController.SinkShip();

        if (_failureRoutine != null)
            StopCoroutine(_failureRoutine);

        _failureRoutine = StartCoroutine(FailureSequence());
    }

    private void UpdateStabilization(bool whileActive, float dt)
    {
        if (whileActive)
        {
            _stabilizationTimer += dt;
            systemIntegrity = Mathf.Clamp(systemIntegrity + integrityRecoverSpeed * dt, 0f, 100f);
        }
        else
        {
            _stabilizationTimer = 0f;
            systemIntegrity = Mathf.Clamp(systemIntegrity - integrityDrainSpeed * dt, 0f, 100f);
        }

        if (_stabilizationTimer >= stabilizationHoldTime && systemIntegrity >= 95f && whileActive)
            _phase = LockPhase.WaterLeveling;
    }

    private void UpdateWaterLeveling(bool whileActive, float dt)
    {
        if (whileActive)
        {
            _waterUnsafeTimer = 0f;
            systemIntegrity = Mathf.Clamp(systemIntegrity + integrityRecoverSpeed * 0.35f * dt, 0f, 100f);
        }
        else
        {
            // Water phase combines FOR actions with this WHILE guard:
            // if WHILE is broken for too long, the lock fails.
            _waterUnsafeTimer += dt;
            systemIntegrity = Mathf.Clamp(systemIntegrity - (integrityDrainSpeed * brokenWhileDrainMultiplier) * dt, 0f, 100f);

            if (_waterUnsafeTimer >= waterUnsafeFailureDelay)
            {
                TriggerFailure();
                return;
            }
        }

        if (waterLevel >= waterLevelTarget)
            _phase = LockPhase.LiftPreparation;
    }

    private void UpdateLiftPreparation(bool whileActive, float dt)
    {
        if (whileActive)
        {
            systemIntegrity = Mathf.Clamp(systemIntegrity + integrityRecoverSpeed * 0.3f * dt, 0f, 100f);
        }
        else
        {
            systemIntegrity = Mathf.Clamp(systemIntegrity - (integrityDrainSpeed * brokenWhileDrainMultiplier) * dt, 0f, 100f);
        }

        if (!_coolingEnabled)
            temperature = Mathf.Clamp(temperature + temperatureCoolSpeed * 0.8f * dt, 0f, 100f);

        if (temperature > maxTemperature)
            systemIntegrity = Mathf.Clamp(systemIntegrity - overheatIntegrityDrain * dt, 0f, 100f);

        if (liftPower >= liftPowerTarget)
            CompleteLevel();
    }

    private void DoPumpForFive()
    {
        // for (int i = 0; i < 5; i++) { pressure += step; }
        for (int i = 0; i < 5; i++)
            pressure += pressureStep;

        pressure = Mathf.Clamp(pressure, 0f, 100f);
    }

    private bool DoWaterFor(int iterations)
    {
        if (_phase != LockPhase.WaterLeveling)
            return false;

        // FOR step runs only while stability condition is valid
        if (!WhileActive)
            return false;

        for (int i = 0; i < iterations; i++)
            waterLevel += waterStep;

        waterLevel = Mathf.Clamp(waterLevel, 0f, 100f);
        return true;
    }

    private bool DoLiftFor(int iterations, float heatPerClick)
    {
        if (_phase != LockPhase.LiftPreparation)
            return false;

        if (!WhileActive)
            return false;

        for (int i = 0; i < iterations; i++)
            liftPower += liftStep;

        liftPower = Mathf.Clamp(liftPower, 0f, 100f);
        temperature = Mathf.Clamp(temperature + heatPerClick, 0f, 100f);
        return true;
    }

    private void CompleteLevel()
    {
        _phase = LockPhase.Completed;

        if (bridgeController != null)
            bridgeController.RaiseBridge();

        if (shipController != null)
            shipController.MoveToEnd();
    }

    private IEnumerator FailureSequence()
    {
        float timer = 0f;
        while (timer < restartDelay)
        {
            timer += Time.deltaTime;
            waterLevel = Mathf.Clamp(waterLevel + failureWaterRiseSpeed * Time.deltaTime, 0f, 100f);
            ApplyWaterVisual();
            UpdateUI();
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ApplyWaterVisual()
    {
        if (waterTransform == null)
            return;

        float normalizedLevel = Mathf.Clamp01(waterLevel / 100f);
        Vector3 pos = waterTransform.position;
        pos.y = Mathf.Lerp(waterMinY, waterMaxY, normalizedLevel);
        waterTransform.position = pos;
    }

    private void UpdateBridgeProgress()
    {
        if (bridgeController == null || _phase != LockPhase.LiftPreparation)
            return;

        float targetPercent = Mathf.Clamp01(liftPower / Mathf.Max(1f, liftPowerTarget)) * 100f;
        bridgeController.RaiseBridgeToPercent(targetPercent);
    }

    private void UpdateUI()
    {
        if (statusText == null)
            return;

        statusText.text =
            $"Phase: {_phase}\n" +
            $"WHILE: {(WhileActive ? "ACTIVE" : "STOPPED")}\n" +
            $"SystemIntegrity: {systemIntegrity:0.0}\n" +
            $"Pressure: {pressure:0.0}\n" +
            $"Temperature: {temperature:0.0}\n" +
            $"WaterLevel: {waterLevel:0.0}\n" +
            $"LiftPower: {liftPower:0.0}\n\n" +
            $"Power: {(_powerEnabled ? "ON" : "OFF")}  " +
            $"Cooling: {(_coolingEnabled ? "ON" : "OFF")}  " +
            $"SafeMode: {(_safeModeEnabled ? "ON" : "OFF")}";

        if (showTestHints)
        {
            statusText.text +=
                "\n\nControls:\n" +
                "Use on-screen buttons only\n" +
                "SafeMode + Power + Cooling keep WHILE active\n" +
                "Pump keeps pressure above minimum";
        }
    }

    private void OnGUI()
    {
        if (!showControlPanel)
            return;

        GUILayout.BeginArea(panelRect, GUI.skin.box);
        GUILayout.Label("Lock Control Panel");
        GUILayout.Space(8f);

        GUILayout.Label($"Phase: {_phase}");
        GUILayout.Label($"WHILE: {(WhileActive ? "ACTIVE" : "STOPPED")}");
        GUILayout.Label($"Integrity: {systemIntegrity:0.0}");
        GUILayout.Label($"Pressure: {pressure:0.0}");
        GUILayout.Label($"Temperature: {temperature:0.0}");
        GUILayout.Label($"Water: {waterLevel:0.0}");
        GUILayout.Label($"Lift: {liftPower:0.0}");
        GUILayout.Space(8f);

        GUI.enabled = CanInteract;
        if (GUILayout.Button($"SafeMode ({(_safeModeEnabled ? "ON" : "OFF")})", GUILayout.Height(30f)))
            ProcessControl(LockControlAction.SafeModeLever);

        if (GUILayout.Button($"Power ({(_powerEnabled ? "ON" : "OFF")})", GUILayout.Height(30f)))
            ProcessControl(LockControlAction.PrimaryButton);

        if (GUILayout.Button($"Cooling ({(_coolingEnabled ? "ON" : "OFF")})", GUILayout.Height(30f)))
            ProcessControl(LockControlAction.SecondaryButton);

        if (GUILayout.Button("Pump FOR x5", GUILayout.Height(30f)))
            ProcessControl(LockControlAction.PumpHandle);

        GUILayout.Space(6f);
        switch (_phase)
        {
            case LockPhase.WaterLeveling:
                if (GUILayout.Button("FOR x10 (Water)", GUILayout.Height(30f)))
                    ProcessControl(LockControlAction.PrimaryButton);
                if (GUILayout.Button("FOR x5 (Water)", GUILayout.Height(30f)))
                    ProcessControl(LockControlAction.SecondaryButton);
                break;
            case LockPhase.LiftPreparation:
                if (GUILayout.Button("FOR x25 (Lift)", GUILayout.Height(30f)))
                    ProcessControl(LockControlAction.PrimaryButton);
                if (GUILayout.Button("FOR x10 (Lift)", GUILayout.Height(30f)))
                    ProcessControl(LockControlAction.SecondaryButton);
                break;
        }

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private void EnsureRuntimeControls()
    {
        if (bridgeController == null)
            bridgeController = FindFirstObjectByType<AllBridgeController>();
        if (shipController == null)
            shipController = FindFirstObjectByType<ShipController>();
        if (playerInteractor == null)
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();

        if (waterTransform == null)
        {
            GameObject waterObject = GameObject.Find("Water");
            if (waterObject != null)
                waterTransform = waterObject.transform;
        }

        if (!useSceneInteractables)
            return;

        RegisterControl(safeModeObjectName, LockControlAction.SafeModeLever, true, true);
        RegisterControl(powerObjectName, LockControlAction.PrimaryButton, true, true);
        RegisterControl(coolingObjectName, LockControlAction.SecondaryButton, true, true);
        RegisterControl(pumpObjectName, LockControlAction.PumpHandle, false, true);
    }

    private void RegisterControl(string objectName, LockControlAction action, bool toggleVisual, bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return;

        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            if (!optional)
                Debug.LogWarning($"LockControlSystem: object '{objectName}' not found.");
            return;
        }

        target.layer = 3;

        if (target.GetComponent<Collider>() == null)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            BoxCollider collider = target.AddComponent<BoxCollider>();
            if (renderer != null)
                collider.size = target.transform.InverseTransformVector(renderer.bounds.size);
        }

        InteractableAnimator oldAnimator = target.GetComponent<InteractableAnimator>();
        if (oldAnimator != null)
            oldAnimator.enabled = false;

        LockControlInteractable interactable = target.GetComponent<LockControlInteractable>();
        if (interactable == null)
            interactable = target.AddComponent<LockControlInteractable>();

        interactable.Setup(this, action, toggleVisual);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapLevel2()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Equals("Level2"))
            return;

        if (FindFirstObjectByType<LockControlSystem>() != null)
            return;

        GameObject bootstrapObject = new GameObject("LockControlSystem");
        bootstrapObject.AddComponent<LockControlSystem>();
    }
}
