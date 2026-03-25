using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelProgressManager : MonoBehaviour
{
    [Serializable]
    public class LevelMenuEntry
    {
        public string sceneName;
        public List<string> additionalScenes = new List<string>();
        public string displayName = "LEVEL";
        [TextArea(2, 3)]
        public string description = string.Empty;
        public Sprite previewIcon;
    }

    [SerializeField] private List<LevelMenuEntry> levels = new List<LevelMenuEntry>();
    [SerializeField] private int defaultUnlockedLevel = 1;

    public IReadOnlyList<LevelMenuEntry> Levels => levels;

    private void Awake()
    {
        EnsureDefaults();
    }

    public int GetConfiguredLevelCount()
    {
        return levels.Count;
    }

    public LevelMenuEntry GetLevel(int levelIndex)
    {
        int zeroBasedIndex = levelIndex - 1;
        if (zeroBasedIndex < 0 || zeroBasedIndex >= levels.Count)
        {
            return null;
        }

        return levels[zeroBasedIndex];
    }

    public bool HasConfiguredScene(int levelIndex)
    {
        LevelMenuEntry level = GetLevel(levelIndex);
        return level != null && !string.IsNullOrWhiteSpace(level.sceneName);
    }

    public int GetContinueLevelIndex()
    {
        if (levels.Count == 0)
        {
            return 1;
        }

        return Mathf.Clamp(GetMaxUnlockedLevel(), 1, levels.Count);
    }

    public void SetLevels(List<LevelMenuEntry> configuredLevels)
    {
        levels = configuredLevels ?? new List<LevelMenuEntry>();
        EnsureDefaults();
    }

    public void EnsureDefaults()
    {
        BootstrapMenuSaveSystem.LoadOrCreate(levels.Count, defaultUnlockedLevel);
    }

    public static int GetMaxUnlockedLevel()
    {
        BootstrapMenuSaveData data = BootstrapMenuSaveSystem.Load();
        return Mathf.Max(1, data.maxUnlockedLevel);
    }

    public static int GetLastPlayedLevel()
    {
        BootstrapMenuSaveData data = BootstrapMenuSaveSystem.Load();
        int fallbackLevel = Mathf.Max(1, data.maxUnlockedLevel);
        return Mathf.Clamp(data.lastPlayedLevel, 1, fallbackLevel);
    }

    public static void SetLastPlayedLevel(int levelIndex)
    {
        int sanitizedLevel = Mathf.Max(1, levelIndex);
        BootstrapMenuSaveSystem.Update(data =>
        {
            data.lastPlayedLevel = sanitizedLevel;
        });
    }

    public static void UnlockLevel(int levelIndex)
    {
        if (levelIndex <= 0)
        {
            return;
        }

        int currentMaxUnlockedLevel = GetMaxUnlockedLevel();
        if (levelIndex <= currentMaxUnlockedLevel)
        {
            return;
        }

        BootstrapMenuSaveSystem.Update(data =>
        {
            data.maxUnlockedLevel = Mathf.Max(data.maxUnlockedLevel, levelIndex);
        });
    }

    public static void CompleteLevel(int completedLevelIndex)
    {
        if (completedLevelIndex <= 0)
        {
            return;
        }

        BootstrapMenuSaveSystem.Update(data =>
        {
            data.completedLevels ??= new List<int>();
            if (!data.completedLevels.Contains(completedLevelIndex))
            {
                data.completedLevels.Add(completedLevelIndex);
                data.completedLevels.Sort();
            }

            data.lastPlayedLevel = Mathf.Max(1, completedLevelIndex);
            data.maxUnlockedLevel = Mathf.Max(data.maxUnlockedLevel, completedLevelIndex + 1);
        });
    }

    public static void ResetProgress()
    {
        BootstrapMenuSaveSystem.Update(data =>
        {
            data.maxUnlockedLevel = 1;
            data.lastPlayedLevel = 1;
            data.completedLevels = new List<int>();
        });
    }

    public static bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex > 0 && levelIndex <= GetMaxUnlockedLevel();
    }

    public static bool IsLevelCompleted(int levelIndex)
    {
        if (levelIndex <= 0)
        {
            return false;
        }

        BootstrapMenuSaveData data = BootstrapMenuSaveSystem.Load();
        return data.completedLevels != null && data.completedLevels.Contains(levelIndex);
    }
}

[Serializable]
internal sealed class BootstrapMenuSaveData
{
    public int maxUnlockedLevel = 1;
    public int lastPlayedLevel = 1;
    public List<int> completedLevels = new List<int>();
    public float soundVolume = 0.8f;
    public int qualityIndex = -1;
    public bool fullscreen = true;
    public int resolutionIndex;
}

internal static class BootstrapMenuSaveSystem
{
    private const string LegacyMaxUnlockedLevelKey = "BootstrapMenu.MaxUnlockedLevel";
    private const string LegacyLastPlayedLevelKey = "BootstrapMenu.LastPlayedLevel";
    private const string LegacyVolumeKey = "BootstrapMenu.Settings.SoundVolume";
    private const string LegacyQualityKey = "BootstrapMenu.Settings.Quality";
    private const string LegacyFullscreenKey = "BootstrapMenu.Settings.Fullscreen";
    private const string LegacyResolutionKey = "BootstrapMenu.Settings.Resolution";

    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "bootstrap_menu.json");
    private static BootstrapMenuSaveData _cachedData;
    private static bool _isLoaded;

    public static BootstrapMenuSaveData Load()
    {
        EnsureLoaded();
        return Clone(_cachedData);
    }

    public static BootstrapMenuSaveData LoadOrCreate(int configuredLevelCount, int defaultUnlockedLevel)
    {
        EnsureLoaded();
        ApplyLevelDefaults(_cachedData, configuredLevelCount, defaultUnlockedLevel);
        SaveInternal();
        return Clone(_cachedData);
    }

    public static void Update(Action<BootstrapMenuSaveData> updateAction)
    {
        EnsureLoaded();
        updateAction?.Invoke(_cachedData);
        SaveInternal();
    }

    public static void DeleteSave()
    {
        _cachedData = null;
        _isLoaded = true;

        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        ClearLegacyPlayerPrefs();
    }

    private static void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        ClearLegacyPlayerPrefs();
        _isLoaded = true;

        if (!File.Exists(SavePath))
        {
            _cachedData = CreateDefaultData();
            SaveInternal();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            _cachedData = JsonConvert.DeserializeObject<BootstrapMenuSaveData>(json) ?? CreateDefaultData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[BootstrapMenuSaveSystem] Failed to read save file: {exception.Message}");
            _cachedData = CreateDefaultData();
        }

        Sanitize(_cachedData);
    }

    private static BootstrapMenuSaveData CreateDefaultData()
    {
        return new BootstrapMenuSaveData
        {
            maxUnlockedLevel = 1,
            lastPlayedLevel = 1,
            completedLevels = new List<int>(),
            soundVolume = 0.8f,
            qualityIndex = QualitySettings.GetQualityLevel(),
            fullscreen = Screen.fullScreen,
            resolutionIndex = 0
        };
    }

    private static void ApplyLevelDefaults(BootstrapMenuSaveData data, int configuredLevelCount, int defaultUnlockedLevel)
    {
        if (data == null)
        {
            return;
        }

        int levelCount = Mathf.Max(1, configuredLevelCount);
        int initialUnlockedLevel = Mathf.Clamp(defaultUnlockedLevel, 1, levelCount);

        data.maxUnlockedLevel = Mathf.Clamp(data.maxUnlockedLevel, 1, levelCount);
        data.lastPlayedLevel = Mathf.Clamp(data.lastPlayedLevel, 1, Mathf.Max(initialUnlockedLevel, data.maxUnlockedLevel));

        if (data.maxUnlockedLevel < initialUnlockedLevel)
        {
            data.maxUnlockedLevel = initialUnlockedLevel;
        }

        if (data.lastPlayedLevel > data.maxUnlockedLevel)
        {
            data.lastPlayedLevel = data.maxUnlockedLevel;
        }

        data.completedLevels ??= new List<int>();
        data.completedLevels.RemoveAll(levelIndex => levelIndex <= 0 || levelIndex > levelCount);
        data.completedLevels.Sort();
    }

    private static void Sanitize(BootstrapMenuSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.maxUnlockedLevel = Mathf.Max(1, data.maxUnlockedLevel);
        data.lastPlayedLevel = Mathf.Clamp(data.lastPlayedLevel, 1, data.maxUnlockedLevel);
        data.completedLevels ??= new List<int>();
        data.completedLevels.RemoveAll(levelIndex => levelIndex <= 0);
        data.completedLevels.Sort();
        data.soundVolume = Mathf.Clamp01(data.soundVolume);

        if (data.qualityIndex < 0)
        {
            data.qualityIndex = QualitySettings.GetQualityLevel();
        }

        if (data.resolutionIndex < 0)
        {
            data.resolutionIndex = 0;
        }
    }

    private static BootstrapMenuSaveData Clone(BootstrapMenuSaveData source)
    {
        return new BootstrapMenuSaveData
        {
            maxUnlockedLevel = source.maxUnlockedLevel,
            lastPlayedLevel = source.lastPlayedLevel,
            completedLevels = source.completedLevels != null ? new List<int>(source.completedLevels) : new List<int>(),
            soundVolume = source.soundVolume,
            qualityIndex = source.qualityIndex,
            fullscreen = source.fullscreen,
            resolutionIndex = source.resolutionIndex
        };
    }

    private static void SaveInternal()
    {
        Sanitize(_cachedData);
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath) ?? Application.persistentDataPath);
        string json = JsonConvert.SerializeObject(_cachedData, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }

    private static void ClearLegacyPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(LegacyMaxUnlockedLevelKey);
        PlayerPrefs.DeleteKey(LegacyLastPlayedLevelKey);
        PlayerPrefs.DeleteKey(LegacyVolumeKey);
        PlayerPrefs.DeleteKey(LegacyQualityKey);
        PlayerPrefs.DeleteKey(LegacyFullscreenKey);
        PlayerPrefs.DeleteKey(LegacyResolutionKey);
        PlayerPrefs.Save();
    }
}
