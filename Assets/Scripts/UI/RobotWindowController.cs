using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class RobotWindowController : MonoBehaviour
{
    [SerializeField] private RobotConfigSO robotConfig;
    [SerializeField] private bool useNextRobotFromManager;
    [SerializeField] private bool useSelectionFromUI;
    [SerializeField] private RobotSelectionUI selectionUI;
    
    [SerializeField] private Image robotIconImage;
    [SerializeField] private TMP_Text robotNameText;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Button robotButton;
    
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthValueText;
    [SerializeField] private Slider damageSlider;
    [SerializeField] private TMP_Text damageValueText;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TMP_Text speedValueText;
    
    [SerializeField] private TMP_Text method1Text;
    [SerializeField] private TMP_Text method2Text;
    [SerializeField] private TMP_Text method3Text;

    private RobotUnlockManager _unlockManager;
    private RobotUnlockEvents _events;

    [Inject]
    public void Construct(RobotUnlockManager unlockManager, RobotUnlockEvents events)
    {
        _unlockManager = unlockManager;
        _events = events;
    }

    private void Awake()
    {
        SetLocked(true);
    }

    private void Start()
    {
        if (useSelectionFromUI && selectionUI == null)
            selectionUI = FindFirstObjectByType<RobotSelectionUI>();

        ResolveRobotConfig();

        if (robotConfig != null)
        {
            LoadRobotData();
            UpdateLockState();
        }
        else
        {
        }

        if (_events != null)
        {
            _events.OnUnlocked += OnRobotUnlocked;
        }

        if (_unlockManager != null)
        {
            _unlockManager.OnProgressApplied += OnProgressApplied;
        }

        if (selectionUI != null)
        {
            selectionUI.OnSelectedRobotChanged += OnSelectedRobotChanged;
        }
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.OnUnlocked -= OnRobotUnlocked;
        }

        if (_unlockManager != null)
        {
            _unlockManager.OnProgressApplied -= OnProgressApplied;
        }

        if (selectionUI != null)
        {
            selectionUI.OnSelectedRobotChanged -= OnSelectedRobotChanged;
        }
    }

    private void OnRobotUnlocked(RobotType unlockedType)
    {
        if (useSelectionFromUI)
        {
            UpdateLockState();
            return;
        }

        if (useNextRobotFromManager)
        {
            ResolveRobotConfig();
            if (robotConfig != null)
            {
                LoadRobotData();
                UpdateLockState();
            }
            return;
        }

        if (robotConfig != null && robotConfig.robotType == unlockedType)
        {
            UpdateLockState();
        }
    }

    private void LoadRobotData()
    {
        if (robotIconImage != null && robotConfig.robotIcon != null)
            robotIconImage.sprite = robotConfig.robotIcon;
        
        if (robotNameText != null)
            robotNameText.text = robotConfig.robotName;
        
        SetupSlider(healthSlider, healthValueText, robotConfig.health);
        SetupSlider(damageSlider, damageValueText, robotConfig.damage);
        SetupSlider(speedSlider, speedValueText, robotConfig.speed);
        
        SetMethodText(method1Text, 0);
        SetMethodText(method2Text, 1);
        SetMethodText(method3Text, 2);
    }

    private void UpdateLockState()
    {
        if (robotConfig == null || _unlockManager == null) return;

        bool isUnlocked = _unlockManager.IsRobotUnlocked(robotConfig.robotType);
        SetLocked(!isUnlocked);
        
    }

    private void SetLocked(bool locked)
    {
        if (lockOverlay != null)
            lockOverlay.SetActive(locked);

        if (robotButton != null)
            robotButton.interactable = !locked;
    }

    private void SetupSlider(Slider slider, TMP_Text valueText, int value)
    {
        if (slider != null)
        {
            slider.maxValue = 5;
            slider.value = value;
            
            if (valueText != null)
                valueText.text = $"{value}/5";
        }
    }

    private void SetMethodText(TMP_Text text, int index)
    {
        if (text == null) return;

        var parent = text.transform.parent != null ? text.transform.parent.gameObject : null;

        bool hasMethod = robotConfig.methodNames != null
            && index < robotConfig.methodNames.Count
            && !string.IsNullOrWhiteSpace(robotConfig.methodNames[index]);

        if (hasMethod)
        {
            text.text = robotConfig.methodNames[index];
            if (parent != null) parent.SetActive(true);
        }
        else
        {
            text.text = "";
            if (parent != null) parent.SetActive(false);
        }
    }

    private void OnProgressApplied()
    {
        if (useSelectionFromUI)
        {
            ResolveRobotConfig();
            if (robotConfig != null)
            {
                LoadRobotData();
                UpdateLockState();
            }
            return;
        }

        if (useNextRobotFromManager)
        {
            ResolveRobotConfig();
            if (robotConfig != null)
            {
                LoadRobotData();
                UpdateLockState();
            }
            return;
        }

        UpdateLockState();
    }

    private void ResolveRobotConfig()
    {
        if (useSelectionFromUI && selectionUI != null && _unlockManager != null)
        {
            var selected = selectionUI.GetSelectedType();
            robotConfig = _unlockManager.GetRobotConfig(selected);
            return;
        }

        if (useNextRobotFromManager && _unlockManager != null)
        {
            var nextType = _unlockManager.GetNextRobotToUnlock();
            if (nextType == RobotType.None)
                return;

            robotConfig = _unlockManager.GetRobotConfig(nextType);
        }
    }

    private void OnSelectedRobotChanged(RobotType type)
    {
        if (_unlockManager == null)
            return;

        robotConfig = _unlockManager.GetRobotConfig(type);
        if (robotConfig != null)
        {
            LoadRobotData();
            UpdateLockState();
        }
    }
}

