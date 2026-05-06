using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class GameplayPauseMenuController : MonoBehaviour
{
    private const string BootstrapSceneName = "Bootstrap";
    private const float PausePanelHeight = 650f;
    private const float ContinueButtonY = 95f;
    private const float SettingsButtonY = -5f;
    private const float TutorialButtonY = -105f;
    private const float ExitToMenuButtonY = -205f;

    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private CanvasGroup pausePanelGroup;
    [SerializeField] private RectTransform pausePanel;
    [SerializeField] private CanvasGroup settingsPanelGroup;
    [SerializeField] private RectTransform settingsPanel;
    [SerializeField] private CanvasGroup tutorialPanelGroup;
    [SerializeField] private RectTransform tutorialPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button exitToMenuButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button tutorialContinueButton;
    [SerializeField] private Button tutorialBackButton;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Image fullscreenGraphic;
    [SerializeField] private TMP_Text fullscreenValueText;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Text tutorialTitleText;
    [SerializeField] private TMP_Text tutorialSummaryText;
    [SerializeField] private TMP_Text tutorialActionsText;

    private static GameplayPauseMenuController _instance;
    private readonly List<Vector2Int> _availableResolutions = new List<Vector2Int>();
    private bool _isOpen;
    private bool _isSettingsOpen;
    private bool _isTutorialOpen;
    private bool _isRefreshingUi;
    private bool _isBound;
    private bool _tutorialOpenedFromPauseMenu;
    private Coroutine _pendingTutorialOpenRoutine;
    private StarterAssetsInputs _activePlayerInputs;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        transform.SetParent(null, false);
        DontDestroyOnLoad(gameObject);

        ResolveReferences();
        BindControls();
        HideImmediate(true);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        CancelPendingTutorialOpen();
        UnbindControls(true);
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            QueueTutorialOpenForScene(activeScene.name);
        }
    }

    private void Update()
    {
        if (_isOpen)
        {
            EnsurePausedState();
        }

        if (SceneManager.GetActiveScene().name == BootstrapSceneName || !WasEscapePressedThisFrame())
        {
            return;
        }

        if (_pendingTutorialOpenRoutine != null && !_isOpen)
        {
            CancelPendingTutorialOpen();
            OpenPause();
            return;
        }

        if (_isTutorialOpen)
        {
            if (_tutorialOpenedFromPauseMenu)
            {
                ReturnFromTutorialToPause();
            }
            else
            {
                ShowPauseAfterTutorial();
            }

            return;
        }

        if (!_isOpen)
        {
            OpenPause();
            return;
        }

        if (_isSettingsOpen)
        {
            ShowSettings(false);
            return;
        }

        ResumeGame();
    }

    private void EnsurePausedState()
    {
        if (Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
        }

        if (!AudioListener.pause)
        {
            AudioListener.pause = true;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();

        if (scene.name == BootstrapSceneName)
        {
            CancelPendingTutorialOpen();
            HideImmediate(true);
            SetGameplayInputEnabled(false);
            GameplayCursorPolicy.ApplyForActiveScene(false);
            return;
        }

        if (!HasTutorialForScene(scene.name))
        {
            return;
        }

        CancelPendingTutorialOpen();
        HideImmediate(false);
        SetGameplayInputEnabled(true);
        QueueTutorialOpenForScene(scene.name);
    }

    private void OpenPause()
    {
        ResolveReferences();
        RefreshSettingsUi();

        _isOpen = true;
        _isSettingsOpen = false;
        _isTutorialOpen = false;
        _tutorialOpenedFromPauseMenu = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetGameplayInputEnabled(false);
        GameplayCursorPolicy.ApplyFreeCursor();

        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;
        rootGroup.alpha = 0f;
        rootGroup.DOKill();
        rootGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true);

        SetPanelImmediate(pausePanelGroup, pausePanel, true);
        SetPanelImmediate(settingsPanelGroup, settingsPanel, false);
        SetPanelImmediate(tutorialPanelGroup, tutorialPanel, false);
    }

    private void ShowSettings(bool show)
    {
        _isSettingsOpen = show;
        RefreshSettingsUi();
        AnimatePanel(pausePanelGroup, pausePanel, !show);
        AnimatePanel(settingsPanelGroup, settingsPanel, show);
    }

    private void ResumeGame()
    {
        if (!_isOpen)
        {
            return;
        }

        CancelPendingTutorialOpen();
        _isOpen = false;
        _isSettingsOpen = false;
        _isTutorialOpen = false;
        _tutorialOpenedFromPauseMenu = false;

        rootGroup.DOKill();
        rootGroup.DOFade(0f, 0.15f)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() => HideImmediate(false));
    }

    private void ExitToMenu()
    {
        CancelPendingTutorialOpen();
        _isOpen = false;
        _isSettingsOpen = false;
        _isTutorialOpen = false;
        _tutorialOpenedFromPauseMenu = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetGameplayInputEnabled(false);
        GameplayCursorPolicy.ApplyForActiveScene(false);
        HideImmediate(true);

        if (!BootstrapSceneLoader.Load(BootstrapSceneName, null))
        {
            SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
        }
    }

    private void HideImmediate(bool keepCursorState)
    {
        if (rootGroup != null)
        {
            rootGroup.DOKill();
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }

        SetPanelImmediate(pausePanelGroup, pausePanel, false);
        SetPanelImmediate(settingsPanelGroup, settingsPanel, false);
        SetPanelImmediate(tutorialPanelGroup, tutorialPanel, false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (!keepCursorState && SceneManager.GetActiveScene().name != BootstrapSceneName)
        {
            SetGameplayInputEnabled(true);
            return;
        }

        SetGameplayInputEnabled(false);
    }

    private void RefreshSettingsUi()
    {
        ResolveReferences();

        if (soundSlider == null || graphicsDropdown == null || fullscreenToggle == null || resolutionDropdown == null)
        {
            return;
        }

        BuildQualityOptions();
        BuildResolutionOptions();

        _isRefreshingUi = true;

        BootstrapMenuSaveData saveData = BootstrapMenuSaveSystem.Load();
        float volume = saveData.soundVolume;
        int qualityIndex = Mathf.Clamp(saveData.qualityIndex, 0, Mathf.Max(0, graphicsDropdown.options.Count - 1));
        bool isFullscreen = saveData.fullscreen;
        int resolutionIndex = Mathf.Clamp(saveData.resolutionIndex, 0, Mathf.Max(0, resolutionDropdown.options.Count - 1));

        soundSlider.SetValueWithoutNotify(volume);
        graphicsDropdown.SetValueWithoutNotify(qualityIndex);
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        resolutionDropdown.SetValueWithoutNotify(resolutionIndex);

        ApplyVolume(volume);
        ApplyQuality(qualityIndex);
        ApplyFullscreen(isFullscreen);
        ApplyResolution(resolutionIndex);
        UpdateFullscreenVisuals(isFullscreen);

        _isRefreshingUi = false;
    }

    private void BuildQualityOptions()
    {
        graphicsDropdown.ClearOptions();
        graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    private void BuildResolutionOptions()
    {
        _availableResolutions.Clear();
        List<string> options = new List<string>();
        HashSet<string> added = new HashSet<string>();

        Resolution[] resolutions = Screen.resolutions;
        if (resolutions.Length == 0)
        {
            Vector2Int current = new Vector2Int(Screen.width, Screen.height);
            _availableResolutions.Add(current);
            options.Add($"{current.x}x{current.y}");
        }
        else
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                Vector2Int resolution = new Vector2Int(resolutions[i].width, resolutions[i].height);
                string label = $"{resolution.x}x{resolution.y}";
                if (!added.Add(label))
                {
                    continue;
                }

                _availableResolutions.Add(resolution);
                options.Add(label);
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void BindControls()
    {
        if (_isBound)
        {
            return;
        }

        ResolveReferences();

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettingsPressed);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OpenTutorialPressed);
        }

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.AddListener(ExitToMenu);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(CloseSettingsPressed);
        }

        if (tutorialContinueButton != null)
        {
            tutorialContinueButton.onClick.AddListener(CloseTutorialAndResume);
        }

        if (tutorialBackButton != null)
        {
            tutorialBackButton.onClick.AddListener(ReturnFromTutorialToPause);
        }

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.AddListener(HandleSoundChanged);
        }

        if (graphicsDropdown != null)
        {
            graphicsDropdown.onValueChanged.AddListener(HandleGraphicsChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(HandleFullscreenChanged);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        }

        _isBound = true;
    }

    private void UnbindControls(bool clearBindings)
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettingsPressed);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveListener(OpenTutorialPressed);
        }

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.RemoveListener(ExitToMenu);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(CloseSettingsPressed);
        }

        if (tutorialContinueButton != null)
        {
            tutorialContinueButton.onClick.RemoveListener(CloseTutorialAndResume);
        }

        if (tutorialBackButton != null)
        {
            tutorialBackButton.onClick.RemoveListener(ReturnFromTutorialToPause);
        }

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveListener(HandleSoundChanged);
        }

        if (graphicsDropdown != null)
        {
            graphicsDropdown.onValueChanged.RemoveListener(HandleGraphicsChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(HandleFullscreenChanged);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
        }

        if (clearBindings)
        {
            _isBound = false;
        }
    }

    private void OpenSettingsPressed()
    {
        ResolveReferences();
        ShowSettings(true);
    }

    private void OpenTutorialPressed()
    {
        TryOpenTutorialForScene(SceneManager.GetActiveScene().name, true);
    }

    private void CloseSettingsPressed()
    {
        ShowSettings(false);
    }

    private void CloseTutorialAndResume()
    {
        ResumeGame();
    }

    private void ReturnFromTutorialToPause()
    {
        if (!_isOpen)
        {
            return;
        }

        _isTutorialOpen = false;
        _tutorialOpenedFromPauseMenu = false;
        _isSettingsOpen = false;
        GameplayCursorPolicy.ApplyFreeCursor();
        AnimatePanel(tutorialPanelGroup, tutorialPanel, false);
        AnimatePanel(settingsPanelGroup, settingsPanel, false);
        AnimatePanel(pausePanelGroup, pausePanel, true);
    }

    private void ShowPauseAfterTutorial()
    {
        if (!_isOpen)
        {
            return;
        }

        _isTutorialOpen = false;
        _tutorialOpenedFromPauseMenu = false;
        _isSettingsOpen = false;
        GameplayCursorPolicy.ApplyFreeCursor();
        rootGroup.DOKill();
        rootGroup.alpha = 1f;
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;
        SetPanelImmediate(tutorialPanelGroup, tutorialPanel, false);
        SetPanelImmediate(settingsPanelGroup, settingsPanel, false);
        SetPanelImmediate(pausePanelGroup, pausePanel, true);
    }

    private void HandleSoundChanged(float value)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyVolume(value);
        BootstrapMenuSaveSystem.Update(data => data.soundVolume = value);
    }

    private void HandleGraphicsChanged(int value)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyQuality(value);
        BootstrapMenuSaveSystem.Update(data => data.qualityIndex = value);
    }

    private void HandleFullscreenChanged(bool isFullscreen)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyFullscreen(isFullscreen);
        UpdateFullscreenVisuals(isFullscreen);
        BootstrapMenuSaveSystem.Update(data => data.fullscreen = isFullscreen);
    }

    private void HandleResolutionChanged(int value)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyResolution(value);
        BootstrapMenuSaveSystem.Update(data => data.resolutionIndex = value);
    }

    private void UpdateFullscreenVisuals(bool isFullscreen)
    {
        if (fullscreenGraphic != null)
        {
            fullscreenGraphic.color = isFullscreen
                ? new Color(0.90f, 0.84f, 0.72f, 1f)
                : new Color(0.26f, 0.28f, 0.31f, 1f);
        }

        if (fullscreenValueText != null)
        {
            fullscreenValueText.text = isFullscreen ? "On" : "Off";
        }
    }

    private void ApplyResolution(int index)
    {
        if (_availableResolutions.Count == 0)
        {
            return;
        }

        Vector2Int resolution = _availableResolutions[Mathf.Clamp(index, 0, _availableResolutions.Count - 1)];
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
    }

    private int GetCurrentResolutionIndex()
    {
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        for (int i = 0; i < _availableResolutions.Count; i++)
        {
            if (_availableResolutions[i] == current)
            {
                return i;
            }
        }

        return 0;
    }

    private static void ApplyVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private static void ApplyQuality(int value)
    {
        QualitySettings.SetQualityLevel(Mathf.Clamp(value, 0, QualitySettings.names.Length - 1), true);
    }

    private static void ApplyFullscreen(bool isFullscreen)
    {
        Screen.fullScreenMode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = isFullscreen;
    }

    private static void AnimatePanel(CanvasGroup group, RectTransform panel, bool visible)
    {
        if (group == null || panel == null)
        {
            return;
        }

        group.gameObject.SetActive(true);
        group.DOKill();
        panel.DOKill();

        if (visible)
        {
            group.alpha = 0f;
            panel.localScale = Vector3.one * 0.97f;
            panel.anchoredPosition = new Vector2(0f, 18f);
        }

        group.interactable = false;
        group.blocksRaycasts = false;

        group.DOFade(visible ? 1f : 0f, 0.18f).SetEase(visible ? Ease.OutQuad : Ease.InQuad).SetUpdate(true);
        panel.DOScale(visible ? 1f : 0.97f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true);
        panel.DOAnchorPos(visible ? Vector2.zero : new Vector2(0f, 18f), 0.18f).SetEase(Ease.OutQuad).SetUpdate(true)
            .OnComplete(() =>
            {
                group.interactable = visible;
                group.blocksRaycasts = visible;
                if (!visible)
                {
                    group.gameObject.SetActive(false);
                }
            });
    }

    private static void SetPanelImmediate(CanvasGroup group, RectTransform panel, bool visible)
    {
        if (group == null || panel == null)
        {
            return;
        }

        group.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        panel.localScale = visible ? Vector3.one : Vector3.one * 0.97f;
        panel.anchoredPosition = visible ? Vector2.zero : new Vector2(0f, 18f);
    }

    private static bool WasEscapePressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }

        return Input.GetKeyDown(KeyCode.Escape);
    }

    private void SetGameplayInputEnabled(bool enabled)
    {
        CacheActivePlayerInputs();

        if (_activePlayerInputs != null)
        {
            _activePlayerInputs.cursorLocked = enabled;
            _activePlayerInputs.cursorInputForLook = enabled;
            _activePlayerInputs.LookInput(Vector2.zero);

            if (!enabled)
            {
                _activePlayerInputs.MoveInput(Vector2.zero);
                _activePlayerInputs.JumpInput(false);
                _activePlayerInputs.SprintInput(false);
            }
        }

        GameplayCursorPolicy.ApplyForActiveScene(enabled);
    }

    private void CacheActivePlayerInputs()
    {
        if (_activePlayerInputs != null && _activePlayerInputs.gameObject.scene.IsValid())
        {
            return;
        }

        _activePlayerInputs = FindFirstObjectByType<StarterAssetsInputs>();
    }

    private void ResolveReferences()
    {
        if (rootGroup == null)
        {
            rootGroup = GetComponent<CanvasGroup>();
        }

        if (pausePanel == null && pausePanelGroup != null)
        {
            pausePanel = pausePanelGroup.transform as RectTransform;
        }

        if (settingsPanel == null && settingsPanelGroup != null)
        {
            settingsPanel = settingsPanelGroup.transform as RectTransform;
        }

        if (pausePanelGroup == null && pausePanel != null)
        {
            pausePanelGroup = pausePanel.GetComponent<CanvasGroup>();
        }

        if (settingsPanelGroup == null && settingsPanel != null)
        {
            settingsPanelGroup = settingsPanel.GetComponent<CanvasGroup>();
        }

        if (tutorialPanel == null && tutorialPanelGroup != null)
        {
            tutorialPanel = tutorialPanelGroup.transform as RectTransform;
        }

        if (tutorialPanelGroup == null && tutorialPanel != null)
        {
            tutorialPanelGroup = tutorialPanel.GetComponent<CanvasGroup>();
        }

        if (continueButton == null)
        {
            continueButton = FindDeepComponent<Button>(transform, "ContinueButton");
        }

        if (settingsButton == null)
        {
            settingsButton = FindDeepComponent<Button>(transform, "SettingsButton");
        }

        if (tutorialButton == null)
        {
            tutorialButton = FindDeepComponent<Button>(transform, "TutorialButton");
        }

        if (exitToMenuButton == null)
        {
            exitToMenuButton = FindDeepComponent<Button>(transform, "ExitToMenuButton");
        }

        Transform settingsRoot = settingsPanel != null ? settingsPanel : transform;

        if (backButton == null)
        {
            backButton = FindDeepComponent<Button>(settingsRoot, "BackButton");
        }

        if (soundSlider == null)
        {
            soundSlider = FindDeepComponent<Slider>(settingsRoot, "SoundSlider");
        }

        if (graphicsDropdown == null)
        {
            graphicsDropdown = FindDeepComponent<TMP_Dropdown>(settingsRoot, "GraphicsDropdown");
        }

        if (fullscreenToggle == null)
        {
            fullscreenToggle = FindDeepComponent<Toggle>(settingsRoot, "FullscreenToggle");
        }

        if (resolutionDropdown == null)
        {
            resolutionDropdown = FindDeepComponent<TMP_Dropdown>(settingsRoot, "ResolutionDropdown");
        }

        if (fullscreenGraphic == null && fullscreenToggle != null)
        {
            fullscreenGraphic = fullscreenToggle.targetGraphic as Image;
        }

        if (fullscreenValueText == null)
        {
            Transform fullscreenRow = FindDeepChild(settingsRoot, "FullscreenRow");
            if (fullscreenRow != null)
            {
                fullscreenValueText = FindDeepComponent<TMP_Text>(fullscreenRow, "ValueText");
            }
        }

        ApplyPauseLayout();
        EnsureTutorialUi();
    }

    private void HandleGameplaySceneReady(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName == BootstrapSceneName)
        {
            return;
        }

        QueueTutorialOpenForScene(sceneName);
    }

    private void QueueTutorialOpenForScene(string sceneName)
    {
        if (!HasTutorialForScene(sceneName))
        {
            return;
        }

        CancelPendingTutorialOpen();
        _pendingTutorialOpenRoutine = StartCoroutine(OpenTutorialForSceneWhenReady(sceneName));
    }

    private IEnumerator OpenTutorialForSceneWhenReady(string sceneName)
    {
        yield return null;
        yield return null;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != sceneName)
        {
            _pendingTutorialOpenRoutine = null;
            yield break;
        }

        if (_isOpen)
        {
            _pendingTutorialOpenRoutine = null;
            yield break;
        }

        TryOpenTutorialForScene(sceneName, false);
        _pendingTutorialOpenRoutine = null;
    }

    private void TryOpenTutorialForScene(string sceneName, bool openedFromPauseMenu)
    {
        if (!HasTutorialForScene(sceneName))
        {
            return;
        }

        if (!GameplayTutorialLibrary.TryGetForScene(sceneName, out GameplayTutorialContent content))
        {
            return;
        }

        OpenTutorial(content, openedFromPauseMenu);
    }

    private static bool HasTutorialForScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName == BootstrapSceneName)
        {
            return false;
        }

        return GameplayTutorialLibrary.TryGetForScene(sceneName, out _);
    }

    private void OpenTutorial(GameplayTutorialContent content, bool openedFromPauseMenu)
    {
        ResolveReferences();

        CancelPendingTutorialOpen();
        _isOpen = true;
        _isSettingsOpen = false;
        _isTutorialOpen = true;
        _tutorialOpenedFromPauseMenu = openedFromPauseMenu;

        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetGameplayInputEnabled(false);
        GameplayCursorPolicy.ApplyFreeCursor();

        if (tutorialTitleText != null)
        {
            tutorialTitleText.text = content.Title;
        }

        if (tutorialSummaryText != null)
        {
            tutorialSummaryText.text = $"{content.Summary}\n\n{content.Actions}";
        }

        if (tutorialActionsText != null)
        {
            tutorialActionsText.text = string.Empty;
            tutorialActionsText.gameObject.SetActive(false);
        }

        SetTutorialBackVisibility(openedFromPauseMenu);

        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;
        rootGroup.DOKill();
        SetPanelImmediate(pausePanelGroup, pausePanel, false);
        SetPanelImmediate(settingsPanelGroup, settingsPanel, false);
        SetPanelImmediate(tutorialPanelGroup, tutorialPanel, true);

        if (openedFromPauseMenu)
        {
            rootGroup.alpha = 0f;
            rootGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        else
        {
            rootGroup.alpha = 1f;
        }
    }

    private void CancelPendingTutorialOpen()
    {
        if (_pendingTutorialOpenRoutine == null)
        {
            return;
        }

        StopCoroutine(_pendingTutorialOpenRoutine);
        _pendingTutorialOpenRoutine = null;
    }

    private void SetTutorialBackVisibility(bool visible)
    {
        if (tutorialBackButton != null)
        {
            tutorialBackButton.gameObject.SetActive(visible);
        }

        if (tutorialContinueButton != null)
        {
            tutorialContinueButton.gameObject.SetActive(!visible);
        }
    }

    private void EnsureTutorialUi()
    {
        if (tutorialPanel == null)
        {
            tutorialPanel = FindDeepComponent<RectTransform>(transform, "TutorialPanel");
        }

        if (tutorialPanelGroup == null && tutorialPanel != null)
        {
            tutorialPanelGroup = tutorialPanel.GetComponent<CanvasGroup>();
        }

        if (tutorialButton == null)
        {
            tutorialButton = FindDeepComponent<Button>(transform, "TutorialButton");
        }

        if (tutorialContinueButton == null)
        {
            tutorialContinueButton = FindDeepComponent<Button>(transform, "TutorialContinueButton");
        }

        if (tutorialBackButton == null)
        {
            tutorialBackButton = FindDeepComponent<Button>(transform, "TutorialBackButton");
        }

        if (tutorialTitleText == null)
        {
            tutorialTitleText = FindDeepComponent<TMP_Text>(transform, "TutorialTitleText");
        }

        if (tutorialSummaryText == null)
        {
            tutorialSummaryText = FindDeepComponent<TMP_Text>(transform, "TutorialSummaryText");
        }

        if (tutorialActionsText == null)
        {
            tutorialActionsText = FindDeepComponent<TMP_Text>(transform, "TutorialActionsText");
        }

        ApplyPauseLayout();
    }

    private static RectTransform CreateTutorialPanel(RectTransform parent)
    {
        Image panelImage = CreateImage("TutorialPanel", parent, new Color(0.08f, 0.08f, 0.09f, 0.97f));
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 660f);
        panelRect.anchoredPosition = Vector2.zero;

        CanvasGroup panelGroup = panelImage.gameObject.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        Image innerPanel = CreateImage("TutorialInnerPanel", panelRect, new Color(0.39f, 0.36f, 0.30f, 1f));
        StretchRect(innerPanel.rectTransform, new Vector2(14f, 14f));

        TextMeshProUGUI title = CreateLabel("TutorialTitleText", innerPanel.rectTransform, 34, Color.white, string.Empty, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.TopLeft;
        title.enableAutoSizing = true;
        title.fontSizeMin = 28f;
        title.fontSizeMax = 34f;
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -25f);
        title.rectTransform.sizeDelta = new Vector2(840f, 56f);

        TextMeshProUGUI summary = CreateLabel("TutorialSummaryText", innerPanel.rectTransform, 22, new Color(0.97f, 0.97f, 0.96f, 1f), string.Empty);
        summary.alignment = TextAlignmentOptions.TopLeft;
        summary.textWrappingMode = TextWrappingModes.Normal;
        summary.overflowMode = TextOverflowModes.Overflow;
        summary.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        summary.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        summary.rectTransform.pivot = new Vector2(0.5f, 1f);
        summary.rectTransform.anchoredPosition = new Vector2(0f, -100f);
        summary.rectTransform.sizeDelta = new Vector2(832f, 390f);

        TextMeshProUGUI actions = CreateLabel("TutorialActionsText", innerPanel.rectTransform, 21, new Color(0.97f, 0.88f, 0.72f, 1f), string.Empty, FontStyles.Bold);
        actions.alignment = TextAlignmentOptions.TopLeft;
        actions.textWrappingMode = TextWrappingModes.Normal;
        actions.overflowMode = TextOverflowModes.Overflow;
        actions.rectTransform.anchorMin = new Vector2(0f, 1f);
        actions.rectTransform.anchorMax = new Vector2(1f, 1f);
        actions.rectTransform.pivot = new Vector2(0f, 1f);
        actions.rectTransform.offsetMin = new Vector2(28f, -120f);
        actions.rectTransform.offsetMax = new Vector2(-28f, -120f);
        actions.gameObject.SetActive(false);

        CreateButton("TutorialContinueButton", innerPanel.rectTransform, "Продолжить игру", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 28f), new Vector2(280f, 68f));
        CreateButton("TutorialBackButton", innerPanel.rectTransform, "Назад в паузу", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 28f), new Vector2(280f, 68f));

        panelImage.gameObject.SetActive(false);
        return panelRect;
    }

    private Button CreatePauseTutorialButton()
    {
        Button templateButton = settingsButton != null ? settingsButton : continueButton;
        if (templateButton == null || pausePanel == null)
        {
            return null;
        }

        Button clonedButton = Instantiate(templateButton, pausePanel);
        clonedButton.name = "TutorialButton";

        RectTransform clonedRect = clonedButton.transform as RectTransform;
        RectTransform settingsRect = settingsButton != null ? settingsButton.transform as RectTransform : null;
        RectTransform exitRect = exitToMenuButton != null ? exitToMenuButton.transform as RectTransform : null;
        if (clonedRect != null)
        {
            clonedRect.anchorMin = settingsRect != null ? settingsRect.anchorMin : clonedRect.anchorMin;
            clonedRect.anchorMax = settingsRect != null ? settingsRect.anchorMax : clonedRect.anchorMax;
            clonedRect.pivot = settingsRect != null ? settingsRect.pivot : clonedRect.pivot;
            clonedRect.sizeDelta = settingsRect != null ? settingsRect.sizeDelta : clonedRect.sizeDelta;
        }

        TMP_Text[] labels = clonedButton.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label != null && label.text == "НАСТРОЙКИ")
            {
                label.text = "ОБУЧЕНИЕ";
                break;
            }
        }

        int insertIndex = exitToMenuButton != null ? exitToMenuButton.transform.GetSiblingIndex() : clonedButton.transform.GetSiblingIndex();
        clonedButton.transform.SetSiblingIndex(insertIndex);
        return clonedButton;
    }

    private void ApplyPauseLayout()
    {
        if (pausePanel != null)
        {
            Vector2 panelSize = pausePanel.sizeDelta;
            pausePanel.sizeDelta = new Vector2(panelSize.x, PausePanelHeight);
        }

        SetButtonY(continueButton, ContinueButtonY);
        SetButtonY(settingsButton, SettingsButtonY);
        SetButtonY(tutorialButton, TutorialButtonY);
        SetButtonY(exitToMenuButton, ExitToMenuButtonY);
    }

    private static void SetButtonY(Button button, float anchoredY)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, anchoredY);
    }

    private static Button CreateButton(
        string objectName,
        RectTransform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.90f, 0.84f, 0.72f, 1f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.34f, 0.28f, 0.20f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI buttonLabel = CreateLabel($"{objectName}Label", rectTransform, 28, new Color(0.12f, 0.13f, 0.15f, 1f), label, FontStyles.Bold);
        buttonLabel.alignment = TextAlignmentOptions.Center;
        StretchRect(buttonLabel.rectTransform, new Vector2(18f, 10f));

        return buttonObject.GetComponent<Button>();
    }

    private static Image CreateImage(string objectName, RectTransform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateLabel(string objectName, RectTransform parent, float fontSize, Color color, string text)
    {
        return CreateLabel(objectName, parent, fontSize, color, text, FontStyles.Normal);
    }

    private static TextMeshProUGUI CreateLabel(string objectName, RectTransform parent, float fontSize, Color color, string text, FontStyles fontStyles)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = color;
        label.text = text;
        label.fontStyle = fontStyles;
        return label;
    }

    private static void StretchRect(RectTransform rectTransform, Vector2 padding)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(padding.x, padding.y);
        rectTransform.offsetMax = new Vector2(-padding.x, -padding.y);
    }

    private static T FindDeepComponent<T>(Transform root, string objectName) where T : Component
    {
        Transform child = FindDeepChild(root, objectName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

internal readonly struct GameplayTutorialContent
{
    public GameplayTutorialContent(string title, string summary, string actions, string reminder)
    {
        Title = title;
        Summary = summary;
        Actions = actions;
        Reminder = reminder;
    }

    public string Title { get; }
    public string Summary { get; }
    public string Actions { get; }
    public string Reminder { get; }
}

internal static class GameplayTutorialLibrary
{
    public static bool TryGetForScene(string sceneName, out GameplayTutorialContent content)
    {
        switch (sceneName)
        {
            case "Level1":
                content = new GameplayTutorialContent(
                    "Обучение: Уровень 1",
                    "Первый уровень знакомит с условиями, значениями и дверями. Игрок перемещается по сцене, ищет нужные объекты и открывает проходы, выполняя правильные условия.",
                    "Управление:\n• WASD — движение;\n• Space — прыжок;\n• Shift — ускорение;\n• Q — инвентарь.\n\nЧто делать:\n• исследуй уровень и читай подсказки у дверей;\n• подбирай нужные предметы и значения;\n• используй инвентарь, если нужно вставить объект в слот;\n• открывай двери правильными комбинациями и иди к следующей зоне.",
                    "Если не понимаешь, куда идти дальше, ориентируйся на ближайшую закрытую дверь и ее условие."
                );
                return true;

            case "Level2":
                content = new GameplayTutorialContent(
                    "Обучение: Уровень 2",
                    "Этот уровень показывает циклы через управление шлюзом. Здесь основная работа идет не перемещением по сцене, а через UI системы шлюза.",
                    "Управление:\n• работа идет через UI;\n• нажимай кнопки и переключатели в интерфейсе шлюза;\n• следи за подсказками и состоянием систем.\n\nЧто делать:\n• управляй питанием, охлаждением, давлением, уровнем воды и воротами;\n• повторяй действия нужное число раз или удерживай процесс, когда этого требует задача;\n• проведи корабль через шлюз и доведи сценарий до завершения.",
                    "Подсказка по следующему шагу должна читаться прямо в интерфейсе уровня."
                );
                return true;

            case "Level3":
                content = new GameplayTutorialContent(
                    "Обучение: Уровень 3",
                    "Третий уровень посвящен артефактам и параметрам игрока. Нужно исследовать сцену, собирать артефакты и при необходимости менять характеристики героя через терминалы.",
                    "Управление:\n• WASD — движение;\n• Space — прыжок;\n• Shift — ускорение.\n\nЧто делать:\n• ищи и собирай все артефакты на уровне;\n• используй терминалы, чтобы менять параметры персонажа;\n• если проход кажется слишком высоким или дальним, попробуй сначала изменить характеристики героя;\n• после сбора всех артефактов откроется финальный путь.",
                    "Если застрял, проверь, не пропущен ли артефакт или терминал рядом."
                );
                return true;

            case "Level4":
                content = new GameplayTutorialContent(
                    "Обучение: Уровень 4",
                    "Четвертый уровень объясняет наследование через разные типы роботов. Здесь игра также идет через UI выбора и управления роботами, а не через обычное передвижение персонажа.",
                    "Управление:\n• работа идет через UI;\n• выбирай нужный тип робота кнопками интерфейса;\n• следи за статусом, подсказками и составом отряда.\n\nЧто делать:\n• проходи секции подходящим типом робота;\n• открывай новые специализации: атака, лечение и защита;\n• в финале собери отряд из 5 роботов и проведи его через последнюю секцию.",
                    "Текст статуса и подсказки на экране показывают, какой робот нужен прямо сейчас."
                );
                return true;
        }

        content = default;
        return false;
    }
}
