using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LockUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LockControlSystem lockControlSystem;
    [SerializeField] private LockInputs lockInputs;

    [Header("Readouts")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text alertText;

    [Header("Control Buttons")]
    [SerializeField] private Button powerButton;
    [SerializeField] private Button coolingButton;
    [SerializeField] private Button safeModeButton;
    [SerializeField] private Button pumpFor5Button;
    [SerializeField] private Button waterFor10Button;
    [SerializeField] private Button waterFor5Button;
    [SerializeField] private Button liftFor10Button;
    [SerializeField] private Button liftFor25Button;

    [Header("Button Labels")]
    [SerializeField] private TMP_Text powerButtonLabel;
    [SerializeField] private TMP_Text coolingButtonLabel;
    [SerializeField] private TMP_Text safeModeButtonLabel;

    private bool _listenersBound;

    public void Initialize(LockControlSystem controlSystem, LockInputs inputs)
    {
        lockControlSystem = controlSystem;
        lockInputs = inputs;

        BindListeners();
        Refresh();
    }

    public void Refresh()
    {
        if (lockControlSystem == null || lockInputs == null)
            return;

        UpdateStatusText();
        UpdateButtonLabels();
        UpdateAlertText();
        UpdateButtonStates();
    }

    private void BindListeners()
    {
        if (_listenersBound)
            return;

        if (powerButton != null)
            powerButton.onClick.AddListener(OnPowerClicked);

        if (coolingButton != null)
            coolingButton.onClick.AddListener(OnCoolingClicked);

        if (safeModeButton != null)
            safeModeButton.onClick.AddListener(OnSafeModeClicked);

        if (pumpFor5Button != null)
            pumpFor5Button.onClick.AddListener(OnPumpFor5Clicked);

        if (waterFor10Button != null)
            waterFor10Button.onClick.AddListener(OnWaterFor10Clicked);

        if (waterFor5Button != null)
            waterFor5Button.onClick.AddListener(OnWaterFor5Clicked);

        if (liftFor10Button != null)
            liftFor10Button.onClick.AddListener(OnLiftFor10Clicked);

        if (liftFor25Button != null)
            liftFor25Button.onClick.AddListener(OnLiftFor25Clicked);

        _listenersBound = true;
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        string forProgress = lockControlSystem.IsForRunning
            ? $"{lockControlSystem.ForLabel} {lockControlSystem.ForIteration}/{lockControlSystem.ForTotal}"
            : "FOR: ожидание";

        statusText.text =
            $"Фаза: {GetPhaseName(lockControlSystem.CurrentPhase)}\n" +
            $"WHILE: {(lockControlSystem.WhileActive ? "АКТИВЕН" : "ОСТАНОВЛЕН")}\n" +
            $"{forProgress}\n\n" +
            $"Целостность: {lockControlSystem.SystemIntegrity:0.0}\n" +
            $"Давление: {lockControlSystem.Pressure:0.0}\n" +
            $"Температура: {lockControlSystem.Temperature:0.0}\n" +
            $"Уровень воды: {lockControlSystem.WaterLevel:0.0}/{lockControlSystem.WaterLevelTarget:0.0}\n" +
            $"Мощность подъема: {lockControlSystem.LiftPower:0.0}/{lockControlSystem.LiftPowerTarget:0.0}";
    }

    private void UpdateButtonLabels()
    {
        if (powerButtonLabel != null)
            powerButtonLabel.text = $"Питание: {(lockInputs.PowerEnabled ? "ВКЛ" : "ВЫКЛ")}";

        if (coolingButtonLabel != null)
            coolingButtonLabel.text = $"Охлаждение: {(lockInputs.CoolingEnabled ? "ВКЛ" : "ВЫКЛ")}";

        if (safeModeButtonLabel != null)
            safeModeButtonLabel.text = $"Безопасный режим: {(lockInputs.SafeModeEnabled ? "ВКЛ" : "ВЫКЛ")}";
    }

    private void UpdateAlertText()
    {
        if (alertText == null)
            return;

        if (lockControlSystem.CurrentPhase == LockPhase.Failed)
            alertText.text = "ТРЕВОГА: КАМЕРА ЗАТОПЛЯЕТСЯ";
        else if (lockControlSystem.CurrentPhase == LockPhase.Completed)
            alertText.text = "ЗАВЕРШЕНО: КОРАБЛЬ ПОКИДАЕТ ШЛЮЗ";
        else
            alertText.text = "Статус: Нормальная работа";
    }

    private static string GetPhaseName(LockPhase phase)
    {
        switch (phase)
        {
            case LockPhase.Stabilization:
                return "Стабилизация";
            case LockPhase.WaterLeveling:
                return "Выравнивание воды";
            case LockPhase.LiftPreparation:
                return "Подъем ворот";
            case LockPhase.Completed:
                return "Завершено";
            case LockPhase.Failed:
                return "Авария";
            default:
                return "Неизвестно";
        }
    }

    private void UpdateButtonStates()
    {
        bool canInput = lockControlSystem.CanReceiveInput;
        bool canRunFor = canInput && !lockControlSystem.IsForRunning;

        SetInteractable(powerButton, canInput);
        SetInteractable(coolingButton, canInput);
        SetInteractable(safeModeButton, canInput);

        SetInteractable(pumpFor5Button, canRunFor);

        bool inWaterPhase = lockControlSystem.CurrentPhase == LockPhase.WaterLeveling;
        bool inLiftPhase = lockControlSystem.CurrentPhase == LockPhase.LiftPreparation;

        SetInteractable(waterFor10Button, canRunFor && inWaterPhase);
        SetInteractable(waterFor5Button, canRunFor && inWaterPhase);
        SetInteractable(liftFor10Button, canRunFor && inLiftPhase);
        SetInteractable(liftFor25Button, canRunFor && inLiftPhase);
    }

    private static void SetInteractable(Selectable selectable, bool value)
    {
        if (selectable != null)
            selectable.interactable = value;
    }

    private void OnPowerClicked()
    {
        lockInputs?.TogglePower();
        Refresh();
    }

    private void OnCoolingClicked()
    {
        lockInputs?.ToggleCooling();
        Refresh();
    }

    private void OnSafeModeClicked()
    {
        lockInputs?.ToggleSafeMode();
        Refresh();
    }

    private void OnPumpFor5Clicked()
    {
        lockInputs?.PumpForFive();
    }

    private void OnWaterFor10Clicked()
    {
        lockInputs?.WaterForTen();
    }

    private void OnWaterFor5Clicked()
    {
        lockInputs?.WaterForFive();
    }

    private void OnLiftFor10Clicked()
    {
        lockInputs?.LiftForTen();
    }

    private void OnLiftFor25Clicked()
    {
        lockInputs?.LiftForTwentyFive();
    }
}
