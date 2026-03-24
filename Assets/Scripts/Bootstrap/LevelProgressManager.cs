using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelProgressManager : MonoBehaviour
{
    private const string MaxUnlockedLevelKey = "BootstrapMenu.MaxUnlockedLevel";
    private const string LastPlayedLevelKey = "BootstrapMenu.LastPlayedLevel";

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
        int configuredLevelCount = Mathf.Max(1, levels.Count);
        int initialUnlockedLevel = Mathf.Clamp(defaultUnlockedLevel, 1, configuredLevelCount);
        int currentUnlockedLevel = initialUnlockedLevel;
        int currentLastPlayedLevel = Mathf.Clamp(PlayerPrefs.GetInt(LastPlayedLevelKey, initialUnlockedLevel), 1, currentUnlockedLevel);

        PlayerPrefs.SetInt(MaxUnlockedLevelKey, currentUnlockedLevel);
        PlayerPrefs.SetInt(LastPlayedLevelKey, currentLastPlayedLevel);
        PlayerPrefs.Save();
    }

    public static int GetMaxUnlockedLevel()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(MaxUnlockedLevelKey, 1));
    }

    public static int GetLastPlayedLevel()
    {
        int fallbackLevel = GetMaxUnlockedLevel();
        return Mathf.Max(1, PlayerPrefs.GetInt(LastPlayedLevelKey, fallbackLevel));
    }

    public static void SetLastPlayedLevel(int levelIndex)
    {
        int sanitizedLevel = Mathf.Max(1, levelIndex);
        PlayerPrefs.SetInt(LastPlayedLevelKey, sanitizedLevel);
        PlayerPrefs.Save();
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

        PlayerPrefs.SetInt(MaxUnlockedLevelKey, levelIndex);
        PlayerPrefs.Save();
    }

    public static void CompleteLevel(int completedLevelIndex)
    {
        if (completedLevelIndex <= 0)
        {
            return;
        }

        SetLastPlayedLevel(completedLevelIndex);
        UnlockLevel(completedLevelIndex + 1);
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(MaxUnlockedLevelKey);
        PlayerPrefs.DeleteKey(LastPlayedLevelKey);
        PlayerPrefs.Save();
    }

    public static bool IsLevelUnlocked(int levelIndex)
    {
        return levelIndex > 0 && levelIndex <= GetMaxUnlockedLevel();
    }

    public static bool IsLevelCompleted(int levelIndex)
    {
        return levelIndex > 0 && levelIndex < GetMaxUnlockedLevel();
    }
}
