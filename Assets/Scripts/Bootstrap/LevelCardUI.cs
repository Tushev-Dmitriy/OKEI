using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelCardUI : MonoBehaviour
{
    [SerializeField] private Image cardBackground;
    [SerializeField] private TMP_Text levelTitleText;
    [SerializeField] private TMP_Text levelDescriptionText;
    [SerializeField] private Image optionalIcon;
    [SerializeField] private Image lockIcon;
    [SerializeField] private Image completedIcon;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startButtonText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedOverlayText;
    [SerializeField] private Sprite unlockedBackgroundSprite;
    [SerializeField] private Sprite lockedBackgroundSprite;
    [SerializeField] private Color lockedTint = new Color(1f, 1f, 1f, 0.7f);

    public void ConfigureReferences(
        Image backgroundImage,
        TMP_Text titleLabel,
        TMP_Text descriptionLabel,
        Image iconImage,
        Image lockedIconImage,
        Image completedIconImage,
        Button button,
        TMP_Text buttonLabel,
        GameObject overlay,
        TMP_Text overlayLabel,
        Sprite unlockedSprite,
        Sprite lockedSprite)
    {
        cardBackground = backgroundImage;
        levelTitleText = titleLabel;
        levelDescriptionText = descriptionLabel;
        optionalIcon = iconImage;
        lockIcon = lockedIconImage;
        completedIcon = completedIconImage;
        startButton = button;
        startButtonText = buttonLabel;
        lockedOverlay = overlay;
        lockedOverlayText = overlayLabel;
        unlockedBackgroundSprite = unlockedSprite;
        lockedBackgroundSprite = lockedSprite;
    }

    public void ConfigureLevel(
        LevelProgressManager.LevelMenuEntry level,
        int levelIndex,
        bool isUnlocked,
        bool isCompleted,
        MainMenuController mainMenuController)
    {
        if (levelTitleText != null)
        {
            levelTitleText.text = string.IsNullOrWhiteSpace(level.displayName)
                ? $"LEVEL {levelIndex}"
                : level.displayName;
        }

        if (levelDescriptionText != null)
        {
            levelDescriptionText.text = level.description ?? string.Empty;
        }

        if (startButtonText != null)
        {
            startButtonText.text = "НАЧАТЬ";
        }

        if (lockedOverlayText != null)
        {
            lockedOverlayText.text = "ЗАБЛОКИРОВАНО";
        }

        if (optionalIcon != null)
        {
            optionalIcon.sprite = level.previewIcon;
            optionalIcon.gameObject.SetActive(level.previewIcon != null && isUnlocked);
        }

        if (cardBackground != null)
        {
            cardBackground.sprite = isUnlocked || lockedBackgroundSprite == null
                ? unlockedBackgroundSprite
                : lockedBackgroundSprite;
            cardBackground.color = isUnlocked ? Color.white : lockedTint;
        }

        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(!isUnlocked);
        }

        if (completedIcon != null)
        {
            completedIcon.gameObject.SetActive(isCompleted);
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.interactable = isUnlocked;
            startButton.gameObject.SetActive(isUnlocked);

            if (isUnlocked && mainMenuController != null)
            {
                startButton.onClick.AddListener(() => mainMenuController.StartLevel(levelIndex));
            }
        }
    }
}
