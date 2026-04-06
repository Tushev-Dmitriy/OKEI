using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;
using DG.Tweening;

public class RobotUnlockHintUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Image robotIconImage;

    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float showDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float maxTotalHintDuration = 1.8f;

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
        float safeMax = Mathf.Max(0.6f, maxTotalHintDuration);
        fadeInDuration = Mathf.Min(fadeInDuration, safeMax * 0.25f);
        fadeOutDuration = Mathf.Min(fadeOutDuration, safeMax * 0.25f);
        float remaining = Mathf.Max(0.2f, safeMax - fadeInDuration - fadeOutDuration);
        showDuration = Mathf.Min(showDuration, remaining);

        if (hintPanel != null)
        {
            hintPanel.gameObject.SetActive(false);
            hintPanel.transform.localScale = Vector3.zero;
        }
    }

    private void Start()
    {
        if (_events != null)
        {
            _events.OnUnlocked += ShowUnlockHint;
        }
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.OnUnlocked -= ShowUnlockHint;
        }
        _currentSequence?.Kill();
    }

    private void ShowUnlockHint(RobotType robotType)
    {
        ShowHintForRobot(robotType);
    }

    public void ShowHintForRobot(RobotType robotType)
    {
        RobotConfigSO config = _unlockManager?.GetRobotConfig(robotType);
        if (config == null)
        {
            return;
        }

        if (hintText != null)
        {
            hintText.text = $"Открыт: {config.robotName}";
        }

        if (robotIconImage != null && config.robotIcon != null)
        {
            robotIconImage.sprite = config.robotIcon;
            robotIconImage.gameObject.SetActive(true);
        }

        AnimatePanel();
    }

    public void ShowCustomHint(string message, Sprite icon = null)
    {
        ShowCustomHint(message, icon, hideIconWhenMissing: true);
    }

    public void ShowSystemHint(string title, string message, Sprite icon = null)
    {
        if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(title))
            return;

        string formattedMessage = string.IsNullOrWhiteSpace(title)
            ? message
            : $"{title}\n{message}";

        ShowCustomHint(formattedMessage, icon, hideIconWhenMissing: false);
    }

    private void ShowCustomHint(string message, Sprite icon, bool hideIconWhenMissing)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (hintText != null)
        {
            hintText.text = message;
        }

        if (robotIconImage != null)
        {
            if (icon != null)
            {
                robotIconImage.sprite = icon;
                robotIconImage.gameObject.SetActive(true);
            }
            else if (hideIconWhenMissing)
            {
                robotIconImage.gameObject.SetActive(false);
            }
            else
            {
                robotIconImage.gameObject.SetActive(robotIconImage.sprite != null);
            }
        }

        AnimatePanel();
    }

    private void AnimatePanel()
    {
        if (hintPanel == null) return;

        _currentSequence?.Kill();
        
        hintPanel.transform.localScale = Vector3.zero;
        hintPanel.gameObject.SetActive(true);

        _currentSequence = DOTween.Sequence()
            .Append(hintPanel.transform.DOScale(1.15f, fadeInDuration * 0.7f))
            .Append(hintPanel.transform.DOScale(1f, fadeInDuration * 0.3f))
            .AppendInterval(showDuration)
            .Append(hintPanel.transform.DOScale(0f, fadeOutDuration))
            .OnComplete(() =>
            {
                if (hintPanel != null)
                    hintPanel.gameObject.SetActive(false);
            });
    }
}

