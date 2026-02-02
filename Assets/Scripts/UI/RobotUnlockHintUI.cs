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

