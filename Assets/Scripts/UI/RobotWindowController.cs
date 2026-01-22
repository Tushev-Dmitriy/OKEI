using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class RobotWindowController : MonoBehaviour
{
    [Header("Robot Config")]
    [SerializeField] private RobotConfigSO robotConfig;
    
    [Header("Robot Info")]
    [SerializeField] private Image robotIconImage;
    [SerializeField] private TMP_Text robotNameText;
    
    [Header("Lock State")]
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Button robotButton;
    
    [Header("Health Slider")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthValueText;
    
    [Header("Damage Slider")]
    [SerializeField] private Slider damageSlider;
    [SerializeField] private TMP_Text damageValueText;
    
    [Header("Speed Slider")]
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TMP_Text speedValueText;
    
    [Header("Methods")]
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
        // Устанавливаем начальное заблокированное состояние
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(true);
        }

        if (robotButton != null)
        {
            robotButton.interactable = false;
        }
    }

    private void Start()
    {
        // После инжекта зависимостей загружаем данные и обновляем состояние
        if (robotConfig != null)
        {
            LoadRobotData();
            UpdateLockState();
        }

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (_events != null)
        {
            _events.OnRobotUnlocked += OnRobotUnlocked;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (_events != null)
        {
            _events.OnRobotUnlocked -= OnRobotUnlocked;
        }
    }

    private void OnRobotUnlocked(RobotType unlockedType)
    {
        if (robotConfig != null && robotConfig.robotType == unlockedType)
        {
            Debug.Log($"Робот {robotConfig.robotName} разблокирован! Обновляем UI.");
            UpdateLockState();
        }
    }

    private void LoadRobotData()
    {
        if (robotIconImage != null && robotConfig.robotIcon != null)
        {
            robotIconImage.sprite = robotConfig.robotIcon;
        }
        
        if (robotNameText != null)
        {
            robotNameText.text = robotConfig.robotName;
        }
        
        SetupSlider(healthSlider, healthValueText, robotConfig.health);
        SetupSlider(damageSlider, damageValueText, robotConfig.damage);
        SetupSlider(speedSlider, speedValueText, robotConfig.speed);
        
        SetMethodText(method1Text, 0);
        SetMethodText(method2Text, 1);
        SetMethodText(method3Text, 2);
    }

    private void UpdateLockState()
    {
        if (robotConfig == null || _unlockManager == null)
            return;

        bool isUnlocked = _unlockManager.IsRobotUnlocked(robotConfig.robotType);

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!isUnlocked);
        }

        if (robotButton != null)
        {
            robotButton.interactable = isUnlocked;
        }

        Debug.Log($"Робот {robotConfig.robotName} ({robotConfig.robotType}): {(isUnlocked ? "открыт" : "заблокирован")}");
    }

    private void SetupSlider(Slider slider, TMP_Text valueText, int value)
    {
        if (slider != null)
        {
            slider.maxValue = 5;
            slider.value = value;
            
            if (valueText != null)
            {
                valueText.text = $"{value}/5";
            }
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
}
