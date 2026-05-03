using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
internal sealed class SceneTransitionService : MonoBehaviour
{
    private const float OverlayFadeDuration = 0.28f;
    private const float OverlayOutDuration = 0.32f;
    private const float PanelFadeOffset = 24f;
    private const float DimAlpha = 1f;
    private const string RunnerName = "SceneTransitionService";

    private static SceneTransitionService _instance;

    private Canvas _overlayCanvas;
    private CanvasGroup _overlayCanvasGroup;
    private RectTransform _overlayPanel;
    private CanvasGroup _overlayPanelGroup;
    private Image _progressFill;
    private TMP_Text _statusText;
    private TMP_Text _hintText;
    private bool _isRunning;
    private bool _portalTransitionCancelable;
    private bool _portalTransitionCanceled;
    private Action _portalCommittedCallback;
    private Action _portalCanceledCallback;

    public static bool IsRunning => _instance != null && _instance._isRunning;

    public static SceneTransitionService GetOrCreate()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameObject root = new GameObject(RunnerName);
        DontDestroyOnLoad(root);
        _instance = root.AddComponent<SceneTransitionService>();
        _instance.CreateOverlay();
        return _instance;
    }

    public static IEnumerator LoadScenes(
        string mainSceneName,
        IEnumerable<string> additionalScenes,
        string initialStatus,
        string mainSceneStatus,
        string additionalScenePrefix,
        string finalStatus,
        string hintText,
        bool lockCursorAfterLoad)
    {
        yield return GetOrCreate().LoadScenesRoutine(
            mainSceneName,
            additionalScenes,
            initialStatus,
            mainSceneStatus,
            additionalScenePrefix,
            finalStatus,
            hintText,
            lockCursorAfterLoad);
    }

    public static IEnumerator LoadBuildIndex(
        int buildIndex,
        string loadingStatus,
        string finalStatus,
        string hintText,
        bool lockCursorAfterLoad)
    {
        yield return GetOrCreate().LoadBuildIndexRoutine(
            buildIndex,
            loadingStatus,
            finalStatus,
            hintText,
            lockCursorAfterLoad);
    }

    public static bool StartPortalTransition(
        int buildIndex,
        int completedLevelIndex,
        float fadeInDuration,
        float fadeOutDuration,
        string loadingStatus,
        string finalStatus,
        string hintText,
        bool lockCursorAfterLoad,
        Action onCommitted,
        Action onCanceled)
    {
        SceneTransitionService service = GetOrCreate();
        if (service._isRunning)
        {
            return false;
        }

        service.StartCoroutine(service.RunPortalTransitionRoutine(
            buildIndex,
            completedLevelIndex,
            fadeInDuration,
            fadeOutDuration,
            loadingStatus,
            finalStatus,
            hintText,
            lockCursorAfterLoad,
            onCommitted,
            onCanceled));
        return true;
    }

    public static void CancelPortalTransition()
    {
        if (_instance == null || !_instance._portalTransitionCancelable)
        {
            return;
        }

        _instance._portalTransitionCanceled = true;
    }

    private IEnumerator LoadScenesRoutine(
        string mainSceneName,
        IEnumerable<string> additionalScenes,
        string initialStatus,
        string mainSceneStatus,
        string additionalScenePrefix,
        string finalStatus,
        string hintText,
        bool lockCursorAfterLoad)
    {
        while (_isRunning)
        {
            yield return null;
        }

        _isRunning = true;
        yield return ShowOverlay(initialStatus, hintText);
        SetProgress(0.08f, true);

        yield return LoadScene(mainSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        SetStatus(mainSceneStatus);

        List<string> additiveScenes = new List<string>();
        if (additionalScenes != null)
        {
            foreach (string sceneName in additionalScenes)
            {
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    additiveScenes.Add(sceneName);
                }
            }
        }

        SetProgress(additiveScenes.Count > 0 ? 0.55f : 0.82f);

        for (int i = 0; i < additiveScenes.Count; i++)
        {
            string sceneName = additiveScenes[i];
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"Scene transition: additional scene '{sceneName}' is missing in build settings.");
                continue;
            }

            SetStatus($"{additionalScenePrefix}{sceneName}");
            yield return LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            SetProgress(0.55f + (0.27f * (i + 1) / additiveScenes.Count));
        }

        UnityEngine.SceneManagement.Scene mainScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(mainSceneName);
        if (mainScene.IsValid() && mainScene.isLoaded)
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(mainScene);
        }

        SetStatus(finalStatus);
        SetProgress(1f);
        ApplyCursorState(lockCursorAfterLoad);
        yield return null;
        yield return HideOverlay();
        _isRunning = false;
    }

    private IEnumerator LoadBuildIndexRoutine(
        int buildIndex,
        string loadingStatus,
        string finalStatus,
        string hintText,
        bool lockCursorAfterLoad)
    {
        while (_isRunning)
        {
            yield return null;
        }

        _isRunning = true;
        yield return ShowOverlay(loadingStatus, hintText);
        SetProgress(0.12f, true);

        AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
        if (loadOperation != null)
        {
            while (!loadOperation.isDone)
            {
                float normalizedProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
                SetProgress(Mathf.Lerp(0.18f, 0.92f, normalizedProgress));
                yield return null;
            }
        }

        SetStatus(finalStatus);
        SetProgress(1f);
        ApplyCursorState(lockCursorAfterLoad);
        yield return null;
        yield return HideOverlay();
        _isRunning = false;
    }

    private IEnumerator RunPortalTransitionRoutine(
        int buildIndex,
        int completedLevelIndex,
        float fadeInDuration,
        float fadeOutDuration,
        string loadingStatus,
        string finalStatus,
        string hintText,
        bool lockCursorAfterLoad,
        Action onCommitted,
        Action onCanceled)
    {
        while (_isRunning)
        {
            yield return null;
        }

        _isRunning = true;
        _portalTransitionCancelable = true;
        _portalTransitionCanceled = false;
        _portalCommittedCallback = onCommitted;
        _portalCanceledCallback = onCanceled;

        yield return ShowOverlay(loadingStatus, hintText, Mathf.Max(0.01f, fadeInDuration));

        if (_portalTransitionCanceled)
        {
            yield return HideOverlay(Mathf.Max(0.01f, fadeOutDuration));
            _portalTransitionCancelable = false;
            _isRunning = false;
            _portalCanceledCallback?.Invoke();
            _portalCommittedCallback = null;
            _portalCanceledCallback = null;
            yield break;
        }

        _portalTransitionCancelable = false;

        if (completedLevelIndex > 0)
        {
            LevelProgressManager.CompleteLevel(completedLevelIndex);
        }

        _portalCommittedCallback?.Invoke();
        _portalCommittedCallback = null;

        SetStatus(finalStatus);
        SetProgress(0.15f, true);

        AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Single);
        if (loadOperation != null)
        {
            while (!loadOperation.isDone)
            {
                float normalizedProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
                SetProgress(Mathf.Lerp(0.2f, 0.94f, normalizedProgress));
                yield return null;
            }
        }

        SetProgress(1f);
        ApplyCursorState(lockCursorAfterLoad);
        yield return null;
        yield return HideOverlay(Mathf.Max(0.01f, fadeOutDuration));
        _portalCanceledCallback = null;
        _isRunning = false;
    }

    private static IEnumerator LoadScene(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadMode)
    {
        if (loadMode == UnityEngine.SceneManagement.LoadSceneMode.Additive)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                yield break;
            }
        }

        AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, loadMode);
        if (loadOperation == null)
        {
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator ShowOverlay(string status, string hint)
    {
        yield return ShowOverlay(status, hint, OverlayFadeDuration);
    }

    private IEnumerator ShowOverlay(string status, string hint, float duration)
    {
        EnsureOverlay();

        _overlayCanvas.gameObject.SetActive(true);
        _overlayCanvasGroup.DOKill();
        _overlayPanelGroup.DOKill();
        _overlayPanel.DOKill();
        _overlayCanvasGroup.alpha = 0f;
        _overlayCanvasGroup.interactable = true;
        _overlayCanvasGroup.blocksRaycasts = true;
        _overlayPanel.localScale = Vector3.one * 0.96f;
        _overlayPanel.anchoredPosition = new Vector2(0f, PanelFadeOffset);
        _overlayPanelGroup.alpha = 0f;
        _progressFill.fillAmount = 0f;

        SetStatus(status);
        SetHint(hint);

        yield return null;

        Tween canvasFadeTween = _overlayCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        Tween fadeTween = _overlayPanelGroup.DOFade(1f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        Tween scaleTween = _overlayPanel.DOScale(1f, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        Tween moveTween = _overlayPanel.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic).SetUpdate(true);

        while ((canvasFadeTween.IsActive() && canvasFadeTween.IsPlaying()) ||
               (fadeTween.IsActive() && fadeTween.IsPlaying()))
        {
            yield return null;
        }

        canvasFadeTween.Kill(false);
        fadeTween.Kill(false);
        scaleTween.Kill(false);
        moveTween.Kill(false);
    }

    private IEnumerator HideOverlay()
    {
        yield return HideOverlay(OverlayOutDuration);
    }

    private IEnumerator HideOverlay(float duration)
    {
        EnsureOverlay();

        _overlayCanvasGroup.DOKill();
        _overlayPanelGroup.DOKill();
        _overlayPanel.DOKill();
        Tween panelFadeTween = _overlayPanelGroup.DOFade(0f, duration * 0.75f).SetEase(Ease.InQuad).SetUpdate(true);
        Tween scaleTween = _overlayPanel.DOScale(1.02f, duration).SetEase(Ease.InQuad).SetUpdate(true);
        Tween fadeTween = _overlayCanvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad).SetUpdate(true);

        while (fadeTween.IsActive() && fadeTween.IsPlaying())
        {
            yield return null;
        }

        panelFadeTween.Kill(false);
        scaleTween.Kill(false);
        _overlayCanvasGroup.blocksRaycasts = false;
        _overlayCanvasGroup.interactable = false;
        _overlayCanvas.gameObject.SetActive(false);
    }

    private void SetProgress(float targetFill, bool immediate = false)
    {
        EnsureOverlay();
        _progressFill.DOKill();
        float clampedFill = Mathf.Clamp01(targetFill);

        if (immediate)
        {
            _progressFill.fillAmount = clampedFill;
            return;
        }

        _progressFill.DOFillAmount(clampedFill, 0.24f).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    private void SetStatus(string text)
    {
        EnsureOverlay();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _statusText.DOKill();
        _statusText.text = text;
        _statusText.alpha = 1f;
    }

    private void SetHint(string text)
    {
        EnsureOverlay();
        _hintText.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
    }

    private static void ApplyCursorState(bool lockCursorAfterLoad)
    {
        if (GameplayCursorPolicy.ActiveSceneNeedsFreeCursor())
        {
            GameplayCursorPolicy.ApplyForActiveScene(false);
            return;
        }

        if (lockCursorAfterLoad)
        {
            GameplayCursorPolicy.ApplyLockedCursor();
            return;
        }

        GameplayCursorPolicy.ApplyFreeCursor();
    }

    private void EnsureOverlay()
    {
        if (_overlayCanvas == null)
        {
            CreateOverlay();
        }
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new GameObject("SceneTransitionOverlay");
        int uiLayer = LayerMask.NameToLayer("UI");
        canvasObject.layer = uiLayer >= 0 ? uiLayer : 0;
        canvasObject.transform.SetParent(transform, false);

        _overlayCanvas = canvasObject.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        _overlayCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        _overlayCanvasGroup.alpha = 0f;
        _overlayCanvasGroup.interactable = true;
        _overlayCanvasGroup.blocksRaycasts = true;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        Image dim = CreateImage("Dim", canvasRect, new Color(0f, 0f, 0f, DimAlpha));
        Stretch(dim.rectTransform);

        _overlayPanel = CreatePanel(canvasRect);
        _overlayPanelGroup = _overlayPanel.GetComponent<CanvasGroup>();

        TMP_Text titleText = CreateLabel("TitleText", _overlayPanel, 72, Color.white, "C# QUEST");
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 48f);
        titleText.rectTransform.sizeDelta = new Vector2(640f, 90f);

        _statusText = CreateLabel("StatusText", _overlayPanel, 34, new Color(0.88f, 0.85f, 0.79f, 1f), string.Empty);
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.rectTransform.anchoredPosition = new Vector2(0f, -18f);
        _statusText.rectTransform.sizeDelta = new Vector2(560f, 56f);

        Image progressTrack = CreateImage("ProgressTrack", _overlayPanel, new Color(0.12f, 0.13f, 0.15f, 0.92f));
        progressTrack.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        progressTrack.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        progressTrack.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        progressTrack.rectTransform.anchoredPosition = new Vector2(0f, -92f);
        progressTrack.rectTransform.sizeDelta = new Vector2(620f, 18f);

        _progressFill = CreateImage("ProgressFill", progressTrack.rectTransform, new Color(0.90f, 0.84f, 0.72f, 1f));
        _progressFill.type = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillOrigin = 0;
        _progressFill.fillAmount = 0f;
        Stretch(_progressFill.rectTransform);

        _hintText = CreateLabel("HintText", _overlayPanel, 26, new Color(0.64f, 0.66f, 0.69f, 1f), string.Empty);
        _hintText.alignment = TextAlignmentOptions.Center;
        _hintText.rectTransform.anchoredPosition = new Vector2(0f, -144f);
        _hintText.rectTransform.sizeDelta = new Vector2(620f, 40f);

        _overlayCanvas.gameObject.SetActive(false);
    }

    private static RectTransform Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return rectTransform;
    }

    private static RectTransform CreatePanel(RectTransform parent)
    {
        Image panel = CreateImage("Panel", parent, new Color(0.10f, 0.11f, 0.13f, 0.9f));
        panel.gameObject.AddComponent<CanvasGroup>();
        RectTransform rectTransform = panel.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(780f, 340f);
        return rectTransform;
    }

    private static Image CreateImage(string objectName, RectTransform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateLabel(string objectName, RectTransform parent, float fontSize, Color color, string text)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = color;
        label.text = text;
        return label;
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

        if (SceneTransitionService.IsRunning || FindFirstObjectByType<BootstrapSceneLoader>() != null)
        {
            return false;
        }

        GameObject loaderObject = new GameObject("BootstrapSceneLoader");
        DontDestroyOnLoad(loaderObject);

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
        yield return SceneTransitionService.LoadScenes(
            _mainSceneName,
            _additionalScenes,
            "Подготовка уровня",
            "Загрузка основного уровня",
            "Подключение: ",
            "Почти готово",
            "Подключаем сцену и готовим интерфейс",
            true);

        Destroy(gameObject);
    }
}
