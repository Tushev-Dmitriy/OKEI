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

    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private CanvasGroup pausePanelGroup;
    [SerializeField] private RectTransform pausePanel;
    [SerializeField] private CanvasGroup settingsPanelGroup;
    [SerializeField] private RectTransform settingsPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitToMenuButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Image fullscreenGraphic;
    [SerializeField] private TMP_Text fullscreenValueText;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private static GameplayPauseMenuController _instance;
    private readonly List<Vector2Int> _availableResolutions = new List<Vector2Int>();
    private bool _isOpen;
    private bool _isSettingsOpen;
    private bool _isRefreshingUi;
    private bool _isBound;
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

        UnbindControls(true);
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == BootstrapSceneName || !WasEscapePressedThisFrame())
        {
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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();

        if (scene.name == BootstrapSceneName)
        {
            HideImmediate(true);
            SetGameplayInputEnabled(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        HideImmediate(false);
        SetGameplayInputEnabled(true);
    }

    private void OpenPause()
    {
        ResolveReferences();
        RefreshSettingsUi();

        _isOpen = true;
        _isSettingsOpen = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetGameplayInputEnabled(false);

        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;
        rootGroup.alpha = 0f;
        rootGroup.DOKill();
        rootGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true);

        SetPanelImmediate(pausePanelGroup, pausePanel, true);
        SetPanelImmediate(settingsPanelGroup, settingsPanel, false);
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

        _isOpen = false;
        _isSettingsOpen = false;

        rootGroup.DOKill();
        rootGroup.DOFade(0f, 0.15f)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() => HideImmediate(false));
    }

    private void ExitToMenu()
    {
        _isOpen = false;
        _isSettingsOpen = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetGameplayInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.AddListener(ExitToMenu);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(CloseSettingsPressed);
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

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.RemoveListener(ExitToMenu);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(CloseSettingsPressed);
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

    private void CloseSettingsPressed()
    {
        ShowSettings(false);
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

        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
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

        if (continueButton == null)
        {
            continueButton = FindDeepComponent<Button>(transform, "ContinueButton");
        }

        if (settingsButton == null)
        {
            settingsButton = FindDeepComponent<Button>(transform, "SettingsButton");
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
