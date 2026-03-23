using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private LevelProgressManager levelProgressManager;
    [SerializeField] private CanvasGroup mainMenuPanel;
    [SerializeField] private RectTransform mainMenuPanelTransform;
    [SerializeField] private CanvasGroup levelSelectPanel;
    [SerializeField] private RectTransform levelSelectPanelTransform;
    [SerializeField] private CanvasGroup settingsPanel;
    [SerializeField] private RectTransform settingsPanelTransform;
    [SerializeField] private LevelSelectPanelController levelSelectController;
    [SerializeField] private SettingsPanelController settingsController;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private float hiddenPanelScale = 0.97f;
    [SerializeField] private float hiddenPanelOffsetY = 24f;

    private bool _isLoadingLevel;

    public void Configure(
        LevelProgressManager progressManager,
        CanvasGroup mainPanelGroup,
        RectTransform mainPanelRect,
        CanvasGroup levelPanelGroup,
        RectTransform levelPanelRect,
        CanvasGroup settingsPanelGroup,
        RectTransform settingsPanelRect,
        LevelSelectPanelController levelSelect,
        SettingsPanelController settings,
        Button continueAction,
        Button levelSelectAction,
        Button settingsAction,
        Button exitAction,
        TMP_Text titleLabel,
        TMP_Text subtitleLabel)
    {
        levelProgressManager = progressManager;
        mainMenuPanel = mainPanelGroup;
        mainMenuPanelTransform = mainPanelRect;
        levelSelectPanel = levelPanelGroup;
        levelSelectPanelTransform = levelPanelRect;
        settingsPanel = settingsPanelGroup;
        settingsPanelTransform = settingsPanelRect;
        levelSelectController = levelSelect;
        settingsController = settings;
        continueButton = continueAction;
        levelSelectButton = levelSelectAction;
        settingsButton = settingsAction;
        exitButton = exitAction;
        titleText = titleLabel;
        subtitleText = subtitleLabel;
    }

    private void Awake()
    {
        WireButtons();
        ApplyDefaultTexts();
        ShowMainMenuImmediate();
        RefreshPanels();
    }

    private void OnEnable()
    {
        WireButtons();
        RefreshPanels();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    private void Update()
    {
        if (_isLoadingLevel || !WasEscapePressedThisFrame())
        {
            return;
        }

        if (IsPanelVisible(settingsPanel) || IsPanelVisible(levelSelectPanel))
        {
            ReturnToMainMenu();
        }
    }

    public void ShowMainMenuImmediate()
    {
        SetPanelImmediate(mainMenuPanel, mainMenuPanelTransform, true);
        SetPanelImmediate(levelSelectPanel, levelSelectPanelTransform, false);
        SetPanelImmediate(settingsPanel, settingsPanelTransform, false);
    }

    public void OpenLevelSelect()
    {
        levelSelectController?.RefreshCards();
        ShowOnly(levelSelectPanel);
    }

    public void OpenSettings()
    {
        settingsController?.RefreshUI();
        ShowOnly(settingsPanel);
    }

    public void ReturnToMainMenu()
    {
        ShowOnly(mainMenuPanel);
    }

    public void LoadContinueLevel()
    {
        if (levelProgressManager == null)
        {
            return;
        }

        StartLevel(levelProgressManager.GetContinueLevelIndex());
    }

    public void StartLevel(int levelIndex)
    {
        if (_isLoadingLevel || levelProgressManager == null)
        {
            return;
        }

        if (!LevelProgressManager.IsLevelUnlocked(levelIndex))
        {
            return;
        }

        LevelProgressManager.LevelMenuEntry level = levelProgressManager.GetLevel(levelIndex);
        if (level == null || string.IsNullOrWhiteSpace(level.sceneName))
        {
            Debug.LogWarning($"Bootstrap menu: scene is not configured for level {levelIndex}.");
            return;
        }

        if (!BootstrapSceneLoader.Load(level.sceneName, level.additionalScenes))
        {
            return;
        }

        _isLoadingLevel = true;
        LevelProgressManager.SetLastPlayedLevel(levelIndex);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void RefreshPanels()
    {
        levelSelectController?.RefreshCards();
        settingsController?.RefreshUI();

        if (continueButton == null || levelProgressManager == null)
        {
            return;
        }

        continueButton.interactable = levelProgressManager.HasConfiguredScene(levelProgressManager.GetContinueLevelIndex());
    }

    private void ApplyDefaultTexts()
    {
        if (titleText != null && string.IsNullOrWhiteSpace(titleText.text))
        {
            titleText.text = "C# QUEST";
        }

        if (subtitleText != null && string.IsNullOrWhiteSpace(subtitleText.text))
        {
            subtitleText.text = "обучающая игра по программированию";
        }
    }

    private void WireButtons()
    {
        UnwireButtons();

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(LoadContinueLevel);
        }

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.AddListener(OpenLevelSelect);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void UnwireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(LoadContinueLevel);
        }

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.RemoveListener(OpenLevelSelect);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
        }
    }

    private void ShowOnly(CanvasGroup targetPanel)
    {
        AnimatePanel(mainMenuPanel, mainMenuPanelTransform, targetPanel == mainMenuPanel);
        AnimatePanel(levelSelectPanel, levelSelectPanelTransform, targetPanel == levelSelectPanel);
        AnimatePanel(settingsPanel, settingsPanelTransform, targetPanel == settingsPanel);

        if (targetPanel == levelSelectPanel)
        {
            levelSelectController?.RefreshCards();
        }

        if (targetPanel == settingsPanel)
        {
            settingsController?.RefreshUI();
        }
    }

    private void AnimatePanel(CanvasGroup panel, RectTransform panelTransform, bool isVisible)
    {
        if (panel == null || panelTransform == null)
        {
            return;
        }

        panel.DOKill();
        panelTransform.DOKill();

        if (isVisible)
        {
            panel.gameObject.SetActive(true);
            panel.alpha = 0f;
            panelTransform.localScale = Vector3.one * hiddenPanelScale;
            panelTransform.anchoredPosition = new Vector2(0f, hiddenPanelOffsetY);
        }

        panel.interactable = false;
        panel.blocksRaycasts = false;

        Sequence transition = DOTween.Sequence();
        transition.Join(panel.DOFade(isVisible ? 1f : 0f, transitionDuration));
        transition.Join(panelTransform.DOScale(isVisible ? 1f : hiddenPanelScale, transitionDuration));
        transition.Join(panelTransform.DOAnchorPos(isVisible ? Vector2.zero : new Vector2(0f, hiddenPanelOffsetY), transitionDuration));
        transition.SetEase(isVisible ? Ease.OutCubic : Ease.InCubic);
        transition.SetUpdate(true);
        transition.OnComplete(() =>
        {
            panel.interactable = isVisible;
            panel.blocksRaycasts = isVisible;

            if (!isVisible)
            {
                panel.gameObject.SetActive(false);
            }
        });
    }

    private void SetPanelImmediate(CanvasGroup panel, RectTransform panelTransform, bool isVisible)
    {
        if (panel == null || panelTransform == null)
        {
            return;
        }

        panel.DOKill();
        panelTransform.DOKill();
        panel.gameObject.SetActive(isVisible);
        panel.alpha = isVisible ? 1f : 0f;
        panel.interactable = isVisible;
        panel.blocksRaycasts = isVisible;
        panelTransform.localScale = isVisible ? Vector3.one : Vector3.one * hiddenPanelScale;
        panelTransform.anchoredPosition = isVisible ? Vector2.zero : new Vector2(0f, hiddenPanelOffsetY);
    }

    private static bool IsPanelVisible(CanvasGroup panel)
    {
        return panel != null && panel.gameObject.activeInHierarchy && panel.alpha > 0.5f;
    }

    private static bool WasEscapePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }

        return Input.GetKeyDown(KeyCode.Escape);
    }
}

[DisallowMultipleComponent]
internal sealed class BootstrapSceneLoader : MonoBehaviour
{
    public static bool Load(string mainSceneName, IEnumerable<string> additionalScenes)
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogWarning("Bootstrap menu: main scene name is empty.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
        {
            Debug.LogError($"Bootstrap menu: scene '{mainSceneName}' is missing in build settings.");
            return false;
        }

        if (Object.FindFirstObjectByType<BootstrapSceneLoader>() != null)
        {
            return false;
        }

        GameObject loaderObject = new GameObject("BootstrapSceneLoader");
        Object.DontDestroyOnLoad(loaderObject);

        BootstrapSceneLoader loader = loaderObject.AddComponent<BootstrapSceneLoader>();
        loader.StartLoading(mainSceneName, additionalScenes);
        return true;
    }

    private string _mainSceneName;
    private readonly List<string> _additionalScenes = new List<string>();

    private void StartLoading(string mainSceneName, IEnumerable<string> additionalScenes)
    {
        _mainSceneName = mainSceneName;
        _additionalScenes.Clear();

        if (additionalScenes != null)
        {
            foreach (string sceneName in additionalScenes)
            {
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    _additionalScenes.Add(sceneName);
                }
            }
        }

        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        yield return LoadScene(_mainSceneName, LoadSceneMode.Single);

        for (int i = 0; i < _additionalScenes.Count; i++)
        {
            string sceneName = _additionalScenes[i];
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"Bootstrap menu: additional scene '{sceneName}' is missing in build settings.");
                continue;
            }

            yield return LoadScene(sceneName, LoadSceneMode.Additive);
        }

        Scene mainScene = SceneManager.GetSceneByName(_mainSceneName);
        if (mainScene.IsValid() && mainScene.isLoaded)
        {
            SceneManager.SetActiveScene(mainScene);
        }

        Destroy(gameObject);
    }

    private static IEnumerator LoadScene(string sceneName, LoadSceneMode loadMode)
    {
        if (loadMode == LoadSceneMode.Additive && IsSceneLoaded(sceneName))
        {
            yield break;
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, loadMode);
        if (loadOperation == null)
        {
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }
}
