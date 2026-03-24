using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    private const string VolumeKey = "BootstrapMenu.Settings.SoundVolume";
    private const string QualityKey = "BootstrapMenu.Settings.Quality";
    private const string FullscreenKey = "BootstrapMenu.Settings.Fullscreen";
    private const string ResolutionKey = "BootstrapMenu.Settings.Resolution";

    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private Button backButton;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Image fullscreenToggleGraphic;
    [SerializeField] private Sprite fullscreenOffSprite;
    [SerializeField] private Sprite fullscreenOnSprite;
    [SerializeField] private TMP_Text fullscreenValueText;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private readonly List<Vector2Int> _availableResolutions = new List<Vector2Int>();
    private bool _isRefreshingUi;
    private Sprite _runtimeFullscreenOffSprite;
    private bool _isFullscreenVisualInitialized;

    public void Configure(
        MainMenuController owner,
        TMP_Text headerLabel,
        Button backAction,
        Slider volumeSlider,
        TMP_Dropdown qualityDropdown,
        Toggle fullscreenModeToggle,
        Image fullscreenGraphic,
        Sprite offSprite,
        Sprite onSprite,
        TMP_Text fullscreenStateText,
        TMP_Dropdown screenResolutionDropdown)
    {
        mainMenuController = owner;
        headerText = headerLabel;
        backButton = backAction;
        soundSlider = volumeSlider;
        graphicsDropdown = qualityDropdown;
        fullscreenToggle = fullscreenModeToggle;
        fullscreenToggleGraphic = fullscreenGraphic;
        fullscreenOffSprite = offSprite;
        fullscreenOnSprite = onSprite;
        fullscreenValueText = fullscreenStateText;
        resolutionDropdown = screenResolutionDropdown;
    }

    private void Awake()
    {
        InitializeFullscreenVisuals();
        BuildOptions();
        WireControls();
        RefreshUI();
    }

    private void OnEnable()
    {
        InitializeFullscreenVisuals();
        BuildOptions();
        WireControls();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnwireControls();
    }

    public void RefreshUI()
    {
        BuildOptions();

        if (soundSlider == null || graphicsDropdown == null || fullscreenToggle == null || resolutionDropdown == null)
        {
            return;
        }

        _isRefreshingUi = true;

        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
        int qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, graphicsDropdown.options.Count - 1));
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        int resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, GetCurrentResolutionIndex()), 0, Mathf.Max(0, resolutionDropdown.options.Count - 1));

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

    private void BuildOptions()
    {
        PopulateQualityOptions();
        PopulateResolutionOptions();
    }

    private void PopulateQualityOptions()
    {
        if (graphicsDropdown == null)
        {
            return;
        }

        graphicsDropdown.ClearOptions();
        graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    private void PopulateResolutionOptions()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        _availableResolutions.Clear();
        List<string> options = new List<string>();
        HashSet<string> addedOptions = new HashSet<string>();

        Resolution[] availableModes = Screen.resolutions;
        if (availableModes.Length == 0)
        {
            Vector2Int current = new Vector2Int(Screen.width, Screen.height);
            _availableResolutions.Add(current);
            options.Add($"{current.x}x{current.y}");
        }
        else
        {
            for (int i = 0; i < availableModes.Length; i++)
            {
                Vector2Int candidate = new Vector2Int(availableModes[i].width, availableModes[i].height);
                string label = $"{candidate.x}x{candidate.y}";
                if (!addedOptions.Add(label))
                {
                    continue;
                }

                _availableResolutions.Add(candidate);
                options.Add(label);
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void WireControls()
    {
        UnwireControls();

        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBackPressed);
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
    }

    private void UnwireControls()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackPressed);
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
    }

    private void HandleBackPressed()
    {
        mainMenuController?.ReturnToMainMenu();
    }

    private void HandleSoundChanged(float value)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyVolume(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void HandleGraphicsChanged(int value)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyQuality(value);
        PlayerPrefs.SetInt(QualityKey, value);
        PlayerPrefs.Save();
    }

    private void HandleFullscreenChanged(bool isFullscreen)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyFullscreen(isFullscreen);
        UpdateFullscreenVisuals(isFullscreen);
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void HandleResolutionChanged(int value)
    {
        if (_isRefreshingUi)
        {
            return;
        }

        ApplyResolution(value);
        PlayerPrefs.SetInt(ResolutionKey, value);
        PlayerPrefs.Save();
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

    private void ApplyResolution(int value)
    {
        if (_availableResolutions.Count == 0)
        {
            return;
        }

        Vector2Int resolution = _availableResolutions[Mathf.Clamp(value, 0, _availableResolutions.Count - 1)];
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
    }

    private void UpdateFullscreenVisuals(bool isFullscreen)
    {
        if (fullscreenToggleGraphic != null)
        {
            Sprite targetSprite = isFullscreen
                ? (fullscreenOnSprite != null ? fullscreenOnSprite : _runtimeFullscreenOffSprite)
                : _runtimeFullscreenOffSprite;

            if (targetSprite != null)
            {
                fullscreenToggleGraphic.sprite = targetSprite;
            }
        }

        if (fullscreenValueText != null)
        {
            fullscreenValueText.text = isFullscreen ? "On" : "Off";
        }
    }

    private void InitializeFullscreenVisuals()
    {
        if (_isFullscreenVisualInitialized)
        {
            return;
        }

        _runtimeFullscreenOffSprite = fullscreenToggleGraphic != null && fullscreenToggleGraphic.sprite != null
            ? fullscreenToggleGraphic.sprite
            : fullscreenOffSprite;

        _isFullscreenVisualInitialized = true;
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
}
