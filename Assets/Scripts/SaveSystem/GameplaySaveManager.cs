using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
public sealed class GameplaySaveManager : MonoBehaviour
{
    private const float AutoSaveInterval = 10f;
    private const float RestoreWaitTimeout = 2.5f;

    private SaveData _cachedSaveData;
    private Coroutine _restoreRoutine;
    private bool _isRestoring;
    private float _restoreSaveBlockUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        EnsureRuntimeInstance();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RestoreInitialScene()
    {
        GameplaySaveManager saveManager = EnsureRuntimeInstance();
        saveManager.ReloadCachedSaveData();
        saveManager.QueueRestoreActiveScene();
    }

    private static GameplaySaveManager EnsureRuntimeInstance()
    {
        if (FindFirstObjectByType<GameplaySaveManager>() != null)
        {
            return FindFirstObjectByType<GameplaySaveManager>();
        }

        GameObject root = new GameObject(nameof(GameplaySaveManager));
        DontDestroyOnLoad(root);
        return root.AddComponent<GameplaySaveManager>();
    }

    private void Awake()
    {
        if (FindObjectsByType<GameplaySaveManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ReloadCachedSaveData();
    }

    private void Start()
    {
        QueueRestoreActiveScene();
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
    }

    private void OnApplicationQuit()
    {
        SaveNow();
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

    public static void RestorePlayerForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        GameplaySaveManager saveManager = FindFirstObjectByType<GameplaySaveManager>();
        if (saveManager != null)
        {
            saveManager.ReloadCachedSaveData();
            RestorePlayer(saveManager._cachedSaveData, sceneName);
            return;
        }

        PlayerSaveSystem.Load(out SaveData data);
        if (data != null)
        {
            RestorePlayer(data, sceneName);
        }
    }

    public static void RestoreSceneObjectsForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PlayerSaveSystem.Load(out SaveData data);
        if (data == null)
        {
            return;
        }

        RestoreSceneObjects(data, sceneName);
        RestoreRobotProgress(data);
    }

    public static void ClearSceneProgress(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        PlayerSaveSystem.Load(out SaveData data);
        if (data == null)
        {
            return;
        }

        bool changed = false;

        if (data.player != null && string.Equals(data.player.level, sceneName, System.StringComparison.Ordinal))
        {
            data.player = null;
            changed = true;
        }

        if (data.playerLevels != null)
        {
            int removedCount = data.playerLevels.RemoveAll(levelData =>
                levelData == null || string.Equals(levelData.level, sceneName, System.StringComparison.Ordinal));
            changed |= removedCount > 0;
        }

        if (data.sceneObjects != null)
        {
            int removedCount = data.sceneObjects.RemoveAll(state =>
                state != null && string.Equals(state.sceneName, sceneName, System.StringComparison.Ordinal));
            changed |= removedCount > 0;
        }

        if (!changed)
        {
            return;
        }

        PlayerSaveSystem.Save(data);

        GameplaySaveManager saveManager = FindFirstObjectByType<GameplaySaveManager>();
        if (saveManager != null)
        {
            saveManager._cachedSaveData = data;
        }
    }

    public void SaveNow()
    {
        if (_isRestoring && Time.unscaledTime < _restoreSaveBlockUntil)
        {
            return;
        }

        if (_isRestoring && Time.unscaledTime >= _restoreSaveBlockUntil)
        {
            _isRestoring = false;
            _restoreRoutine = null;
        }

        SaveData data = LoadOrCreate();
        string sceneName = SceneManager.GetActiveScene().name;
        if (GameplayDebugHotkeys.IsTransientDebugActiveForScene(sceneName))
        {
            return;
        }

        CapturePlayer(data, sceneName);
        CapturePlayerRuntime(data);
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
        ReloadCachedSaveData();
        QueueRestoreActiveScene();
    }

    private void ReloadCachedSaveData()
    {
        PlayerSaveSystem.Load(out _cachedSaveData);
        _cachedSaveData ??= CreateDefaultSaveData();
    }

    private void QueueRestoreActiveScene()
    {
        if (_restoreRoutine != null)
        {
            StopCoroutine(_restoreRoutine);
            _restoreRoutine = null;
        }

        _isRestoring = true;
        _restoreSaveBlockUntil = Time.unscaledTime + RestoreWaitTimeout + 1f;
        _restoreRoutine = StartCoroutine(RestoreActiveSceneWhenReady());
    }

    private IEnumerator RestoreActiveSceneWhenReady()
    {
        yield return null;

        ReloadCachedSaveData();
        string sceneName = SceneManager.GetActiveScene().name;

        RestoreSceneObjects(_cachedSaveData, sceneName);
        RestoreRobotProgress(_cachedSaveData);

        float deadline = Time.unscaledTime + RestoreWaitTimeout;
        while (FindPlayerTransform() == null && Time.unscaledTime < deadline)
        {
            yield return null;
        }

        RestorePlayer(_cachedSaveData, sceneName);
        RestorePlayerRuntime(_cachedSaveData);
        RestoreSceneObjects(_cachedSaveData, sceneName);
        RestoreRobotProgress(_cachedSaveData);

        yield return null;
        RestorePlayer(_cachedSaveData, sceneName);
        RestorePlayerRuntime(_cachedSaveData);
        RestoreSceneObjects(_cachedSaveData, sceneName);
        RestoreRobotProgress(_cachedSaveData);

        if (sceneName == "Level2")
        {
            yield return new WaitForSeconds(0.25f);
            ReloadCachedSaveData();
            RestoreSceneObjects(_cachedSaveData, sceneName);
        }

        _isRestoring = false;
        _restoreRoutine = null;
        _restoreSaveBlockUntil = 0f;
        GameplayCursorPolicy.ApplyForActiveScene(!GameplayCursorPolicy.ActiveSceneNeedsFreeCursor());
    }

    private void RestoreActiveScene()
    {
        if (_cachedSaveData == null)
        {
            return;
        }

        _isRestoring = true;
        ReloadCachedSaveData();
        string sceneName = SceneManager.GetActiveScene().name;
        RestorePlayer(_cachedSaveData, sceneName);
        RestorePlayerRuntime(_cachedSaveData);
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
        ThirdPersonController controller = FindPlayerController();
        if (controller == null)
        {
            return;
        }

        Vector3 playerPosition = GetPlayerWorldPosition(controller);
        Vector3 playerRotation = GetPlayerWorldRotation(controller);

        PlayerLevelData levelData = new PlayerLevelData
        {
            level = sceneName,
            position = ToVector3Data(playerPosition),
            rotation = ToVector3Data(playerRotation)
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
        ThirdPersonController controller = FindPlayerController();
        if (controller == null)
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

        CharacterController characterController = controller.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = controller.GetComponentInChildren<CharacterController>();
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        ApplyPlayerWorldPose(controller, FromVector3Data(levelData.position), FromVector3Data(levelData.rotation));

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

    private static void CapturePlayerRuntime(SaveData data)
    {
        ThirdPersonController controller = FindPlayerController();
        if (controller == null)
            return;

        data.playerRuntime = new PlayerRuntimeData
        {
            moveSpeed = controller.MoveSpeed,
            jumpHeight = controller.JumpHeight,
            gravity = controller.Gravity,
            size = controller.Size
        };
    }

    private static void RestorePlayerRuntime(SaveData data)
    {
        if (data?.playerRuntime == null)
            return;

        ThirdPersonController controller = FindPlayerController();
        if (controller == null)
            return;

        ApplyPlayerRuntimeParameter(controller, PlayerParamType.MoveSpeed, data.playerRuntime.moveSpeed);
        ApplyPlayerRuntimeParameter(controller, PlayerParamType.JumpHeight, data.playerRuntime.jumpHeight);
        ApplyPlayerRuntimeParameter(controller, PlayerParamType.Gravity, data.playerRuntime.gravity);
        ApplyPlayerRuntimeParameter(controller, PlayerParamType.Size, data.playerRuntime.size);
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
        return FindPlayerController()?.transform;
    }

    private static ThirdPersonController FindPlayerController()
    {
        return Object.FindFirstObjectByType<ThirdPersonController>();
    }

    private static Vector3 GetPlayerWorldPosition(ThirdPersonController controller)
    {
        return controller != null ? controller.transform.position : Vector3.zero;
    }

    private static Vector3 GetPlayerWorldRotation(ThirdPersonController controller)
    {
        return controller != null ? controller.transform.eulerAngles : Vector3.zero;
    }

    private static void ApplyPlayerWorldPose(ThirdPersonController controller, Vector3 targetPosition, Vector3 targetEulerAngles)
    {
        if (controller == null)
            return;

        Transform playerTransform = controller.transform;
        Transform rootTransform = playerTransform.root;
        if (rootTransform == playerTransform)
        {
            playerTransform.SetPositionAndRotation(targetPosition, Quaternion.Euler(targetEulerAngles));
            return;
        }

        Vector3 positionDelta = targetPosition - playerTransform.position;
        rootTransform.position += positionDelta;

        Vector3 currentEulerAngles = playerTransform.eulerAngles;
        Vector3 rotationDelta = new Vector3(
            Mathf.DeltaAngle(currentEulerAngles.x, targetEulerAngles.x),
            Mathf.DeltaAngle(currentEulerAngles.y, targetEulerAngles.y),
            Mathf.DeltaAngle(currentEulerAngles.z, targetEulerAngles.z));
        rootTransform.eulerAngles += rotationDelta;
    }

    private static void ApplyPlayerRuntimeParameter(ThirdPersonController controller, PlayerParamType paramType, float value)
    {
        if (controller == null)
            return;

        switch (paramType)
        {
            case PlayerParamType.MoveSpeed:
            case PlayerParamType.JumpHeight:
            case PlayerParamType.Size:
                if (value <= 0f)
                    return;
                break;
            case PlayerParamType.Gravity:
                if (value >= 0f)
                    return;
                break;
        }

        controller.SendMessage(
            "OnParamChanged",
            new PlayerParamChangedSignal
            {
                ParamType = paramType,
                Value = value
            },
            SendMessageOptions.DontRequireReceiver);
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
