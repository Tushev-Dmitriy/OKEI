using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
public sealed class GameplaySaveManager : MonoBehaviour
{
    private const float AutoSaveInterval = 3f;

    private SaveData _cachedSaveData;
    private bool _isRestoring;
    private bool _isPrimaryInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        if (FindFirstObjectByType<GameplaySaveManager>() != null)
        {
            return;
        }

        GameObject root = new GameObject(nameof(GameplaySaveManager));
        DontDestroyOnLoad(root);
        root.AddComponent<GameplaySaveManager>();
    }

    private void Awake()
    {
        if (FindObjectsByType<GameplaySaveManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        _isPrimaryInstance = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        PlayerSaveSystem.Load(out _cachedSaveData);
        _cachedSaveData ??= CreateDefaultSaveData();
    }

    private void Start()
    {
        RestoreActiveScene();
        StartCoroutine(AutoSaveRoutine());
    }

    private void Update()
    {
        if (GameplayCursorPolicy.ActiveSceneNeedsFreeCursor())
        {
            GameplayCursorPolicy.ApplyForActiveScene(false);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (_isPrimaryInstance && Application.isPlaying)
        {
            SaveNow();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveNow();
        }
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveNow();
        }
    }

    public static void SaveCurrentGame()
    {
        GameplaySaveManager saveManager = FindFirstObjectByType<GameplaySaveManager>();
        if (saveManager != null)
        {
            saveManager.SaveNow();
            return;
        }

        PlayerSaver playerSaver = FindFirstObjectByType<PlayerSaver>();
        if (playerSaver != null)
        {
            playerSaver.SavePlayerData();
        }
    }

    public void SaveNow()
    {
        if (_isRestoring)
        {
            return;
        }

        SaveData data = LoadOrCreate();
        string sceneName = SceneManager.GetActiveScene().name;

        CapturePlayer(data, sceneName);
        CaptureLastPlayedLevel(sceneName);
        CaptureInventory();
        CaptureSceneObjects(data, sceneName);
        CaptureRobotProgress(data);
        CaptureGameplayProgress(data);

        data.settings ??= new SettingsData();
        data.saveInfo = new SaveInfoData
        {
            saveVersion = "1.1",
            lastSaveTime = System.DateTime.Now.ToString("O")
        };

        _cachedSaveData = data;
        PlayerSaveSystem.Save(data);
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(AutoSaveInterval);
            SaveNow();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerSaveSystem.Load(out _cachedSaveData);
        _cachedSaveData ??= CreateDefaultSaveData();
        StartCoroutine(RestoreAfterFrame());
    }

    private IEnumerator RestoreAfterFrame()
    {
        yield return null;
        RestoreActiveScene();
    }

    private void RestoreActiveScene()
    {
        if (_cachedSaveData == null)
        {
            return;
        }

        _isRestoring = true;
        string sceneName = SceneManager.GetActiveScene().name;
        RestorePlayer(_cachedSaveData, sceneName);
        RestoreSceneObjects(_cachedSaveData, sceneName);
        RestoreRobotProgress(_cachedSaveData);
        _isRestoring = false;

        GameplayCursorPolicy.ApplyForActiveScene(!GameplayCursorPolicy.ActiveSceneNeedsFreeCursor());
    }

    private SaveData LoadOrCreate()
    {
        PlayerSaveSystem.Load(out SaveData data);
        return data ?? _cachedSaveData ?? CreateDefaultSaveData();
    }

    private static SaveData CreateDefaultSaveData()
    {
        return new SaveData
        {
            settings = new SettingsData { musicVolume = 0.5f, sfxVolume = 0.5f },
            saveInfo = new SaveInfoData { saveVersion = "1.1", lastSaveTime = System.DateTime.Now.ToString("O") },
            sceneObjects = new List<SceneObjectStateData>(),
            playerLevels = new List<PlayerLevelData>(),
            gameplayProgress = new GameplayProgressData()
        };
    }

    private static void CapturePlayer(SaveData data, string sceneName)
    {
        Transform playerTransform = FindPlayerTransform();
        if (playerTransform == null)
        {
            return;
        }

        PlayerLevelData levelData = new PlayerLevelData
        {
            level = sceneName,
            position = ToVector3Data(playerTransform.position),
            rotation = ToVector3Data(playerTransform.eulerAngles)
        };

        data.player = new PlayerData
        {
            level = sceneName,
            position = levelData.position,
            rotation = levelData.rotation
        };

        data.playerLevels ??= new List<PlayerLevelData>();
        data.playerLevels.RemoveAll(x => x == null || x.level == sceneName);
        data.playerLevels.Add(levelData);
    }

    private static void RestorePlayer(SaveData data, string sceneName)
    {
        Transform playerTransform = FindPlayerTransform();
        if (playerTransform == null)
        {
            return;
        }

        PlayerLevelData levelData = data.playerLevels?
            .LastOrDefault(x => x != null && x.level == sceneName);

        if (levelData == null && data.player != null && data.player.level == sceneName)
        {
            levelData = new PlayerLevelData
            {
                level = data.player.level,
                position = data.player.position,
                rotation = data.player.rotation
            };
        }

        if (levelData?.position == null || levelData.rotation == null)
        {
            return;
        }

        CharacterController characterController = playerTransform.GetComponentInChildren<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        playerTransform.position = FromVector3Data(levelData.position);
        playerTransform.eulerAngles = FromVector3Data(levelData.rotation);

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private static void CaptureSceneObjects(SaveData data, string sceneName)
    {
        List<SceneObjectStateData> currentStates = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<ISceneSaveable>()
            .Select(saveable =>
            {
                SceneObjectStateData state = saveable.CaptureState();
                if (state != null)
                {
                    state.sceneName = sceneName;
                }

                return state;
            })
            .Where(state => state != null && !string.IsNullOrWhiteSpace(state.id))
            .ToList();

        data.sceneObjects ??= new List<SceneObjectStateData>();
        data.sceneObjects.RemoveAll(state => state == null || state.sceneName == sceneName || string.IsNullOrWhiteSpace(state.sceneName));
        data.sceneObjects.AddRange(currentStates);
    }

    private static void RestoreSceneObjects(SaveData data, string sceneName)
    {
        if (data.sceneObjects == null || data.sceneObjects.Count == 0)
        {
            return;
        }

        var states = data.sceneObjects
            .Where(state => state != null && (state.sceneName == sceneName || string.IsNullOrWhiteSpace(state.sceneName)))
            .GroupBy(state => state.id)
            .ToDictionary(group => group.Key, group => group.Last());

        foreach (ISceneSaveable saveable in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISceneSaveable>())
        {
            if (!string.IsNullOrWhiteSpace(saveable.SaveId) && states.TryGetValue(saveable.SaveId, out SceneObjectStateData state))
            {
                saveable.RestoreState(state);
            }
        }
    }

    private static void CaptureRobotProgress(SaveData data)
    {
        RobotUnlockManager manager = Object.FindFirstObjectByType<RobotUnlockManager>();
        if (manager != null)
        {
            data.robotProgress = manager.CaptureProgress();
        }
    }

    private static void CaptureInventory()
    {
        foreach (InventorySaver inventorySaver in Object.FindObjectsByType<InventorySaver>(FindObjectsSortMode.None))
        {
            if (inventorySaver != null && inventorySaver.isActiveAndEnabled)
            {
                inventorySaver.SaveInventory();
            }
        }
    }

    private static void RestoreRobotProgress(SaveData data)
    {
        if (data.robotProgress == null)
        {
            return;
        }

        RobotUnlockManager manager = Object.FindFirstObjectByType<RobotUnlockManager>();
        if (manager != null)
        {
            manager.ApplyProgress(data.robotProgress);
        }
    }

    private static void CaptureGameplayProgress(SaveData data)
    {
        data.gameplayProgress ??= new GameplayProgressData();
        Level4FlowController level4 = Object.FindFirstObjectByType<Level4FlowController>();
        if (level4 != null)
        {
            data.gameplayProgress.level4ProgressStage = level4.ProgressStageValue;
        }
    }

    private static void CaptureLastPlayedLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !sceneName.StartsWith("Level"))
        {
            return;
        }

        string numberPart = sceneName.Substring("Level".Length);
        if (int.TryParse(numberPart, out int levelIndex) && levelIndex > 0)
        {
            LevelProgressManager.SetLastPlayedLevel(levelIndex);
        }
    }

    private static Transform FindPlayerTransform()
    {
        ThirdPersonController controller = Object.FindFirstObjectByType<ThirdPersonController>();
        return controller != null ? controller.transform.root : null;
    }

    private static Vector3Data ToVector3Data(Vector3 value)
    {
        return new Vector3Data { x = value.x, y = value.y, z = value.z };
    }

    private static Vector3 FromVector3Data(Vector3Data value)
    {
        return new Vector3(value.x, value.y, value.z);
    }
}
