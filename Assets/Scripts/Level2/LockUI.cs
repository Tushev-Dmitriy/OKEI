using System;
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
    [SerializeField] private TMP_Text phaseInfoText;
    [SerializeField] private TMP_Text cycleInfoText;
    [SerializeField] private TMP_Text alertText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressSlider;

    [Header("Cycle Indicators")]
    [SerializeField] private Toggle whileToggle;
    [SerializeField] private Toggle forToggle;

    [Header("Indicator Colors")]
    [SerializeField] private Color indicatorOnColor = new Color(0.28f, 0.92f, 0.62f, 1f);
    [SerializeField] private Color indicatorOffColor = new Color(0.58f, 0.22f, 0.22f, 1f);

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
    [SerializeField] private TMP_Text pumpFor5ButtonLabel;
    [SerializeField] private TMP_Text waterFor10ButtonLabel;
    [SerializeField] private TMP_Text waterFor5ButtonLabel;
    [SerializeField] private TMP_Text liftFor10ButtonLabel;
    [SerializeField] private TMP_Text liftFor25ButtonLabel;

    private bool _powerBound;
    private bool _coolingBound;
    private bool _safeModeBound;
    private bool _pumpBound;
    private bool _water10Bound;
    private bool _water5Bound;
    private bool _lift10Bound;
    private bool _lift25Bound;
    private bool _referencesResolved;

    private void Awake()
    {
        ResolveUiReferences();
        CacheMissingLabels();
        BindListeners();
        _referencesResolved = true;
    }

    public void Initialize(LockControlSystem controlSystem, LockInputs inputs)
    {
        lockControlSystem = controlSystem;
        lockInputs = inputs;

        ResolveUiReferences();
        CacheMissingLabels();
        BindListeners();
        _referencesResolved = true;
        Refresh();
    }

    public void Refresh()
    {
        if (!_referencesResolved)
        {
            ResolveUiReferences();
            CacheMissingLabels();
            BindListeners();
            _referencesResolved = true;
        }

        if (lockControlSystem == null)
            lockControlSystem = GetComponent<LockControlSystem>();

        if (lockInputs == null)
            lockInputs = GetComponent<LockInputs>();

        if (lockControlSystem == null || lockInputs == null)
            return;

        UpdateStatusText();
        UpdatePhaseInfoText();
        UpdateCycleInfoText();
        UpdateButtonLabels();
        UpdateAlertText();
        UpdateProgress();
        UpdateCycleIndicators();
        UpdateButtonStates();
    }

    private void BindListeners()
    {
        TryBind(powerButton, OnPowerClicked, ref _powerBound);
        TryBind(coolingButton, OnCoolingClicked, ref _coolingBound);
        TryBind(safeModeButton, OnSafeModeClicked, ref _safeModeBound);
        TryBind(pumpFor5Button, OnPumpFor5Clicked, ref _pumpBound);
        TryBind(waterFor10Button, OnWaterFor10Clicked, ref _water10Bound);
        TryBind(waterFor5Button, OnWaterFor5Clicked, ref _water5Bound);
        TryBind(liftFor10Button, OnLiftFor10Clicked, ref _lift10Bound);
        TryBind(liftFor25Button, OnLiftFor25Clicked, ref _lift25Bound);
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        string statusBlock = BuildSystemBlock();

        if (phaseInfoText == null)
            statusBlock += "\n\n" + BuildPhaseBlock();

        if (cycleInfoText == null)
            statusBlock += "\n\n" + BuildCycleBlock();

        if (progressText == null)
            statusBlock += $"\n\nПРОГРЕСС: {Mathf.Clamp01(lockControlSystem.SessionProgress) * 100f:0}%";

        if (alertText == null)
            statusBlock += "\n\n" + BuildInstructionMessage();

        statusText.text = statusBlock;
    }

    private void UpdatePhaseInfoText()
    {
        if (phaseInfoText == null)
            return;

        phaseInfoText.text = BuildPhaseBlock();
    }

    private void UpdateCycleInfoText()
    {
        if (cycleInfoText == null)
            return;

        cycleInfoText.text = BuildCycleBlock();
    }

    private void UpdateButtonLabels()
    {
        SetToggleLabel(powerButtonLabel, "Питание", lockInputs.PowerEnabled);
        SetToggleLabel(coolingButtonLabel, "Охлаждение", lockInputs.CoolingEnabled);
        SetToggleLabel(safeModeButtonLabel, "Безопасный режим", lockInputs.SafeModeEnabled);

        SetActionLabel(pumpFor5ButtonLabel, lockControlSystem.PumpForCaption);
        SetActionLabel(waterFor10ButtonLabel, lockControlSystem.WaterPrimaryCaption);
        SetActionLabel(waterFor5ButtonLabel, lockControlSystem.WaterSecondaryCaption);
        SetActionLabel(liftFor25ButtonLabel, lockControlSystem.LiftPrimaryCaption);
        SetActionLabel(liftFor10ButtonLabel, lockControlSystem.LiftSecondaryCaption);
    }

    private void UpdateAlertText()
    {
        if (alertText == null)
            return;

        alertText.text = BuildInstructionMessage();
        alertText.color = GetInstructionColor();
    }

    private string BuildPhaseInstruction()
    {
        switch (lockControlSystem.CurrentPhase)
        {
            case LockPhase.Stabilization:
                return "ЧТО ДЕЛАТЬ:\nДержите WHILE активным до 100% стабилизации, подкачивайте давление при просадке.";
            case LockPhase.WaterLeveling:
                return "ЧТО ДЕЛАТЬ:\nКорректируйте воду кнопками FOR x10/x5 и удерживайте уровень возле целевого.";
            case LockPhase.LiftPreparation:
                return "ЧТО ДЕЛАТЬ:\nНабирайте подъем ворот FOR x25/x10, следите за температурой и WHILE.";
            default:
                return "ЧТО ДЕЛАТЬ:\nВыполняйте действия текущей фазы.";
        }
    }

    private string BuildSystemBlock()
    {
        float stabilizationPercent = lockControlSystem.CurrentPhase == LockPhase.Stabilization
            ? lockControlSystem.StabilizationProgress * 100f
            : 100f;

        return
            $"Стабилизация: {stabilizationPercent:0}%\n" +
            $"Целостность: {lockControlSystem.SystemIntegrity:0.0}\n" +
            $"Давление: {lockControlSystem.Pressure:0.0}\n" +
            $"Температура: {lockControlSystem.Temperature:0.0}\n" +
            $"Уровень воды: {lockControlSystem.WaterLevel:0.0}/{lockControlSystem.WaterLevelTarget:0.0}\n" +
            $"Подъем ворот: {lockControlSystem.LiftPower:0.0}/{lockControlSystem.LiftPowerTarget:0.0}";
    }

    private string BuildPhaseBlock()
    {
        string incidentLine = lockControlSystem.HasActiveIncident
            ? $"{lockControlSystem.ActiveIncidentLabel} ({lockControlSystem.ActiveIncidentTimeLeft:0.0}с)"
            : "нет";

        return
            $"Фаза: {GetPhaseName(lockControlSystem.CurrentPhase)}\n" +
            $"Цель: {GetObjective(lockControlSystem.CurrentPhase)}\n" +
            $"Инцидент: {incidentLine}\n" +
            $"Угроза: {lockControlSystem.ThreatTier}";
    }

    private string BuildCycleBlock()
    {
        string forProgress = lockControlSystem.ForLabel;
        if (lockControlSystem.IsForRunning)
            forProgress = $"{forProgress} {lockControlSystem.ForIteration}/{lockControlSystem.ForTotal}";

        return
            $"WHILE: {(lockControlSystem.WhileActive ? "АКТИВЕН" : "НЕ АКТИВЕН")}\n" +
            forProgress;
    }

    private string BuildInstructionMessage()
    {
        if (lockControlSystem.CurrentPhase == LockPhase.Failed)
            return "ЧТО ДЕЛАТЬ:\nТРЕВОГА: камера затапливается, уровень будет перезапущен.";

        if (lockControlSystem.CurrentPhase == LockPhase.Completed)
            return "ЧТО ДЕЛАТЬ:\nЗАВЕРШЕНО: ворота открыты, корабль выходит.";

        if (lockControlSystem.HasActiveIncident)
            return $"ЧТО ДЕЛАТЬ:\nИНЦИДЕНТ: {lockControlSystem.ActiveIncidentLabel}. {lockControlSystem.ActiveIncidentHint}";

        if (!lockControlSystem.WhileActive)
            return "ЧТО ДЕЛАТЬ:\nСоберите WHILE: питание + охлаждение + безопасный режим + давление выше минимума.";

        if (lockControlSystem.IsForRunning)
            return "ЧТО ДЕЛАТЬ:\nИдет FOR-цикл. Дождитесь его завершения и не роняйте WHILE.";

        return BuildPhaseInstruction();
    }

    private Color GetInstructionColor()
    {
        if (lockControlSystem.CurrentPhase == LockPhase.Failed)
            return new Color(1f, 0.34f, 0.34f, 1f);

        if (lockControlSystem.CurrentPhase == LockPhase.Completed)
            return new Color(0.45f, 1f, 0.53f, 1f);

        if (lockControlSystem.HasActiveIncident)
            return new Color(1f, 0.75f, 0.35f, 1f);

        if (!lockControlSystem.WhileActive)
            return new Color(1f, 0.86f, 0.35f, 1f);

        if (lockControlSystem.IsForRunning)
            return new Color(0.55f, 0.92f, 1f, 1f);

        return Color.white;
    }

    private void UpdateProgress()
    {
        float progress = Mathf.Clamp01(lockControlSystem.SessionProgress);

        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            progressSlider.SetValueWithoutNotify(progress);
        }

        if (progressText != null)
            progressText.text = $"ПРОГРЕСС: {progress * 100f:0}%";
    }

    private void UpdateCycleIndicators()
    {
        SetToggleIndicator(whileToggle, lockControlSystem.WhileActive);
        SetToggleIndicator(forToggle, lockControlSystem.IsForRunning);
    }

    private void SetToggleIndicator(Toggle toggle, bool isActive)
    {
        if (toggle == null)
            return;

        toggle.interactable = false;
        toggle.SetIsOnWithoutNotify(isActive);

        Color baseColor = isActive ? indicatorOnColor : indicatorOffColor;

        if (toggle.targetGraphic != null)
            toggle.targetGraphic.color = new Color(baseColor.r * 0.4f, baseColor.g * 0.4f, baseColor.b * 0.4f, 0.95f);

        if (toggle.graphic != null)
            toggle.graphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, isActive ? 1f : 0.2f);
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

    private static string GetObjective(LockPhase phase)
    {
        switch (phase)
        {
            case LockPhase.Stabilization:
                return "Удерживайте WHILE до полной стабилизации";
            case LockPhase.WaterLeveling:
                return "Доведите уровень воды до целевого";
            case LockPhase.LiftPreparation:
                return "Наберите мощность подъема ворот";
            case LockPhase.Completed:
                return "Дождитесь выхода корабля";
            case LockPhase.Failed:
                return "Ожидайте перезапуск";
            default:
                return "-";
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

    private static void SetToggleLabel(TMP_Text label, string caption, bool enabled)
    {
        if (label == null)
            return;

        label.text = $"{caption}: {(enabled ? "ВКЛ" : "ВЫКЛ")}";
        label.color = enabled ? new Color(0.62f, 1f, 0.72f, 1f) : Color.white;
    }

    private static void SetActionLabel(TMP_Text label, string caption)
    {
        if (label == null)
            return;

        label.text = caption;
        label.color = Color.white;
    }

    private void CacheMissingLabels()
    {
        if (powerButtonLabel == null)
            powerButtonLabel = FindButtonLabel(powerButton);

        if (coolingButtonLabel == null)
            coolingButtonLabel = FindButtonLabel(coolingButton);

        if (safeModeButtonLabel == null)
            safeModeButtonLabel = FindButtonLabel(safeModeButton);

        if (pumpFor5ButtonLabel == null)
            pumpFor5ButtonLabel = FindButtonLabel(pumpFor5Button);

        if (waterFor10ButtonLabel == null)
            waterFor10ButtonLabel = FindButtonLabel(waterFor10Button);

        if (waterFor5ButtonLabel == null)
            waterFor5ButtonLabel = FindButtonLabel(waterFor5Button);

        if (liftFor10ButtonLabel == null)
            liftFor10ButtonLabel = FindButtonLabel(liftFor10Button);

        if (liftFor25ButtonLabel == null)
            liftFor25ButtonLabel = FindButtonLabel(liftFor25Button);
    }

    private static TMP_Text FindButtonLabel(Button button)
    {
        if (button == null)
            return null;

        Transform directLabel = button.transform.Find("Label");
        if (directLabel != null && directLabel.TryGetComponent(out TMP_Text text))
            return text;

        return button.GetComponentInChildren<TMP_Text>(true);
    }

    private void ResolveUiReferences()
    {
        Transform uiRoot = FindUiRoot();
        if (uiRoot == null)
            return;

        TMP_Text[] texts = uiRoot.GetComponentsInChildren<TMP_Text>(true);
        Slider[] sliders = uiRoot.GetComponentsInChildren<Slider>(true);
        Toggle[] toggles = uiRoot.GetComponentsInChildren<Toggle>(true);
        Button[] buttons = uiRoot.GetComponentsInChildren<Button>(true);

        if (statusText == null)
            statusText = FindTextByKeywords(texts, "lockstatus", "status", "система");

        if (phaseInfoText == null)
            phaseInfoText = FindTextByKeywords(texts, "lockphase", "phase", "этап", "фаза");

        if (cycleInfoText == null)
            cycleInfoText = FindTextByKeywords(texts, "lockcycle", "cycle", "цикл");

        if (alertText == null)
            alertText = FindTextByKeywords(texts, "lockalert", "alert", "hint", "todo", "что");

        if (progressText == null)
            progressText = FindTextByKeywords(texts, "lockprogress", "progress", "прогресс");

        if (progressSlider == null)
            progressSlider = FindSliderByKeywords(sliders, "progress", "прогресс");

        if (whileToggle == null)
            whileToggle = FindToggleByKeywords(toggles, "while");

        if (forToggle == null)
            forToggle = FindToggleByKeywords(toggles, "for");

        if (whileToggle == null && toggles.Length > 0)
            whileToggle = toggles[0];

        if (forToggle == null && toggles.Length > 1)
        {
            if (toggles[0] == whileToggle)
                forToggle = toggles[1];
            else
                forToggle = toggles[0];
        }

        if (powerButton == null)
            powerButton = FindButtonByKeywords(buttons, "power");

        if (coolingButton == null)
            coolingButton = FindButtonByKeywords(buttons, "cool");

        if (safeModeButton == null)
            safeModeButton = FindButtonByKeywords(buttons, "safe");

        if (pumpFor5Button == null)
            pumpFor5Button = FindButtonByKeywords(buttons, "pump");

        if (waterFor10Button == null)
            waterFor10Button = FindButtonByKeywords(buttons, "waterfor10", "water10");

        if (waterFor5Button == null)
            waterFor5Button = FindButtonByKeywords(buttons, "waterfor5", "water5");

        if (liftFor10Button == null)
            liftFor10Button = FindButtonByKeywords(buttons, "liftfor10", "lift10");

        if (liftFor25Button == null)
            liftFor25Button = FindButtonByKeywords(buttons, "liftfor25", "lift25");
    }

    private Transform FindUiRoot()
    {
        if (statusText != null)
            return statusText.transform.root;

        if (phaseInfoText != null)
            return phaseInfoText.transform.root;

        if (cycleInfoText != null)
            return cycleInfoText.transform.root;

        if (alertText != null)
            return alertText.transform.root;

        if (progressText != null)
            return progressText.transform.root;

        if (progressSlider != null)
            return progressSlider.transform.root;

        GameObject lockCanvas = GameObject.Find("LockCanvas");
        if (lockCanvas != null)
            return lockCanvas.transform;

        Canvas anyCanvas = FindFirstObjectByType<Canvas>();
        return anyCanvas != null ? anyCanvas.transform : null;
    }

    private static TMP_Text FindTextByKeywords(TMP_Text[] texts, params string[] keywords)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || !HasKeyword(text.name, keywords))
                continue;

            return text;
        }

        return null;
    }

    private static Slider FindSliderByKeywords(Slider[] sliders, params string[] keywords)
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null || !HasKeyword(slider.name, keywords))
                continue;

            return slider;
        }

        return sliders.Length > 0 ? sliders[0] : null;
    }

    private static Toggle FindToggleByKeywords(Toggle[] toggles, params string[] keywords)
    {
        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];
            if (toggle == null || !HasKeyword(toggle.name, keywords))
                continue;

            return toggle;
        }

        return null;
    }

    private static Button FindButtonByKeywords(Button[] buttons, params string[] keywords)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !HasKeyword(button.name, keywords))
                continue;

            return button;
        }

        return null;
    }

    private static bool HasKeyword(string source, string[] keywords)
    {
        if (string.IsNullOrEmpty(source) || keywords == null || keywords.Length == 0)
            return false;

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrEmpty(keyword))
                continue;

            if (source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static void TryBind(Button button, UnityEngine.Events.UnityAction callback, ref bool isBound)
    {
        if (isBound || button == null)
            return;

        button.onClick.AddListener(callback);
        isBound = true;
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
