using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;
using DG.Tweening;

public class RobotUnlockHintUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Image robotIconImage;

    [Header("Hint Templates")]
    [SerializeField] private string attackerHintTemplate = "Враг слишком силен! Нужен <color=red>атакующий робот</color>";
    [SerializeField] private string healerHintTemplate = "Ваш робот поврежден! Нужен <color=green>лечащий робот</color>";
    [SerializeField] private string defenderHintTemplate = "Слишком много урона! Нужен <color=blue>защищающий робот</color>";

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float showDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private RobotUnlockManager _unlockManager;
    private RobotUnlockEvents _events;
    private Sequence _currentSequence;

    [Inject]
    public void Construct(RobotUnlockManager unlockManager, RobotUnlockEvents events)
    {
        _unlockManager = unlockManager;
        _events = events;
    }

    private void Awake()
    {
        if (hintPanel != null)
        {
            hintPanel.alpha = 0f;
            hintPanel.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (_events != null)
        {
            _events.OnRobotHintRequested += HandleHintRequest;
        }
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.OnRobotHintRequested -= HandleHintRequest;
        }

        _currentSequence?.Kill();
    }

    private void HandleHintRequest(RobotType robotType)
    {
        string hintTemplate = GetHintTemplate(robotType);
        ShowHint(robotType, hintTemplate);
    }

    private string GetHintTemplate(RobotType robotType)
    {
        return robotType switch
        {
            RobotType.Attacker => attackerHintTemplate,
            RobotType.Healer => healerHintTemplate,
            RobotType.Defender => defenderHintTemplate,
            _ => $"Нужен робот типа {robotType}"
        };
    }

    private void ShowHint(RobotType robotType, string hint)
    {
        if (_unlockManager == null)
        {
            Debug.LogWarning("RobotUnlockManager не найден!");
            return;
        }

        if (_unlockManager.IsRobotUnlocked(robotType))
        {
            Debug.Log($"Робот {robotType} уже открыт. Подсказка не показывается.");
            return;
        }

        RobotConfigSO config = _unlockManager.GetRobotConfig(robotType);

        if (config == null)
        {
            Debug.LogWarning($"Конфиг для робота {robotType} не найден!");
            return;
        }

        if (hintText != null)
        {
            hintText.text = hint;
        }

        if (robotIconImage != null)
        {
            if (config.robotIcon != null)
            {
                robotIconImage.sprite = config.robotIcon;
                robotIconImage.gameObject.SetActive(true);
                Debug.Log($"Иконка робота {robotType} установлена: {config.robotIcon.name}");
            }
            else
            {
                robotIconImage.gameObject.SetActive(false);
                Debug.LogWarning($"У конфига робота {robotType} отсутствует иконка!");
            }
        }

        AnimateHintPanel();

        Debug.Log($"Показана подсказка для робота {robotType}");
    }

    private void AnimateHintPanel()
    {
        if (hintPanel == null)
        {
            Debug.LogWarning("hintPanel не назначен!");
            return;
        }

        _currentSequence?.Kill();
        hintPanel.gameObject.SetActive(true);
        hintPanel.alpha = 1f;

        var panelTransform = hintPanel.transform;
        panelTransform.localScale = Vector3.zero;

        _currentSequence = DOTween.Sequence()
            .Append(panelTransform.DOScale(1.15f, fadeInDuration * 0.7f).SetEase(Ease.OutBack))
            .Append(panelTransform.DOScale(1f, fadeInDuration * 0.3f).SetEase(Ease.InOutSine))
            .AppendInterval(showDuration)
            .Append(panelTransform.DOScale(0f, fadeOutDuration).SetEase(Ease.InBack))
            .OnStart(() =>
            {
                hintPanel.alpha = 1f;
            })
            .OnComplete(() =>
            {
                if (hintPanel != null)
                {
                    hintPanel.alpha = 0f;
                    hintPanel.gameObject.SetActive(false);
                }
            });
    }
}
