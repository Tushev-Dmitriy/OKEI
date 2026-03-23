using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelSelectPanelController : MonoBehaviour
{
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private LevelProgressManager levelProgressManager;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private Button backButton;
    [SerializeField] private LevelCardUI[] levelCards;

    public void Configure(
        MainMenuController owner,
        LevelProgressManager progressManager,
        TMP_Text headerLabel,
        Button backAction,
        LevelCardUI[] cards)
    {
        mainMenuController = owner;
        levelProgressManager = progressManager;
        headerText = headerLabel;
        backButton = backAction;
        levelCards = cards;
    }

    private void Awake()
    {
        if (headerText != null && string.IsNullOrWhiteSpace(headerText.text))
        {
            headerText.text = "ВЫБОР УРОВНЯ";
        }

        WireButtons();
        RefreshCards();
    }

    private void OnEnable()
    {
        WireButtons();
        RefreshCards();
    }

    private void OnDisable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackPressed);
        }
    }

    public void RefreshCards()
    {
        if (levelProgressManager == null || levelCards == null)
        {
            return;
        }

        for (int i = 0; i < levelCards.Length; i++)
        {
            LevelCardUI card = levelCards[i];
            if (card == null)
            {
                continue;
            }

            int levelIndex = i + 1;
            LevelProgressManager.LevelMenuEntry level = levelProgressManager.GetLevel(levelIndex);

            if (level == null)
            {
                card.gameObject.SetActive(false);
                continue;
            }

            card.gameObject.SetActive(true);
            card.ConfigureLevel(
                level,
                levelIndex,
                LevelProgressManager.IsLevelUnlocked(levelIndex),
                LevelProgressManager.IsLevelCompleted(levelIndex),
                mainMenuController);
        }
    }

    private void WireButtons()
    {
        if (backButton == null)
        {
            return;
        }

        backButton.onClick.RemoveListener(HandleBackPressed);
        backButton.onClick.AddListener(HandleBackPressed);
    }

    private void HandleBackPressed()
    {
        mainMenuController?.ReturnToMainMenu();
    }
}
