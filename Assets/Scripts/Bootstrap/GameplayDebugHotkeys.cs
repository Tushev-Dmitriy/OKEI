using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameplayDebugHotkeys : MonoBehaviour
{
    private const string RuntimeObjectName = nameof(GameplayDebugHotkeys);
    private static readonly string[] SupportedScenes = { "Level1", "Level2", "Level3", "Level4" };
    private static readonly HashSet<string> TransientDebugScenes = new(StringComparer.Ordinal);
    private static readonly HashSet<string> ConsumedQuickCompleteScenes = new(StringComparer.Ordinal);

    [SerializeField] private KeyCode quickCompleteHotkey = KeyCode.F9;
    private Coroutine _level4DebugCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<GameplayDebugHotkeys>() != null)
            return;

        GameObject runtimeObject = new(RuntimeObjectName);
        DontDestroyOnLoad(runtimeObject);
        runtimeObject.AddComponent<GameplayDebugHotkeys>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(quickCompleteHotkey))
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        if (ConsumedQuickCompleteScenes.Contains(sceneName))
            return;

        if (sceneName == "Level4")
        {
            if (_level4DebugCoroutine == null)
                _level4DebugCoroutine = StartCoroutine(DebugCompleteLevel4WhenReady(sceneName));
            return;
        }

        bool applied = sceneName switch
        {
            "Level1" => DebugCompleteLevel1(),
            "Level2" => DebugCompleteLevel2(),
            "Level3" => DebugCompleteLevel3(),
            _ => false
        };

        if (applied)
            ConsumedQuickCompleteScenes.Add(sceneName);
    }

    private static bool DebugCompleteLevel1()
    {
        MarkTransientForScene("Level1");
        Door[] doors = FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Door door in doors.Where(door => door != null))
            door.SetOpen(true);

        CommitDebugProgress("Level1");

        Debug.Log($"[{nameof(GameplayDebugHotkeys)}] Level1 debug complete applied: opened {doors.Length} doors.");
        return doors.Length > 0;
    }

    private static bool DebugCompleteLevel3()
    {
        MarkTransientForScene("Level3");
        Level3ArtifactManager artifactManager = FindFirstObjectByType<Level3ArtifactManager>();
        if (artifactManager == null)
        {
            Debug.LogWarning($"[{nameof(GameplayDebugHotkeys)}] Level3 debug complete skipped: no {nameof(Level3ArtifactManager)} found.");
            return false;
        }

        artifactManager.DebugCollectAllArtifacts();
        bool teleported = TeleportLevel3PlayerToFinalPos();
        if (teleported)
            CommitDebugProgress("Level3");
        return teleported;
    }

    private static bool DebugCompleteLevel2()
    {
        MarkTransientForScene("Level2");
        LockControlSystem lockControlSystem = FindFirstObjectByType<LockControlSystem>();
        if (lockControlSystem == null)
        {
            Debug.LogWarning($"[{nameof(GameplayDebugHotkeys)}] Level2 debug complete skipped: no {nameof(LockControlSystem)} found.");
            return false;
        }

        lockControlSystem.DebugCompleteLevel();
        LevelProgressManager.CompleteLevel(2);
        CommitDebugProgress("Level2");
        return true;
    }

    private static bool DebugCompleteLevel4()
    {
        MarkTransientForScene("Level4");
        Level4FlowController flowController = FindFirstObjectByType<Level4FlowController>();
        if (flowController == null)
        {
            Debug.LogWarning($"[{nameof(GameplayDebugHotkeys)}] Level4 debug complete skipped: no {nameof(Level4FlowController)} found.");
            return false;
        }

        flowController.DebugPrepareFinalSquad();
        CommitDebugProgress("Level4");
        return true;
    }

    private IEnumerator DebugCompleteLevel4WhenReady(string sceneName)
    {
        MarkTransientForScene(sceneName);

        const int maxFrames = 120;
        int waitedFrames = 0;

        Level4FlowController flowController = null;
        RobotSelectionUI selectionUI = null;

        while (waitedFrames < maxFrames)
        {
            flowController = FindFirstObjectByType<Level4FlowController>();
            selectionUI = FindFirstObjectByType<RobotSelectionUI>();

            bool uiReady = selectionUI != null && selectionUI.GetEntryCount() > 0;
            if (flowController != null && uiReady)
                break;

            waitedFrames++;
            yield return null;
        }

        bool applied = false;
        if (flowController != null)
        {
            selectionUI = selectionUI != null ? selectionUI : FindFirstObjectByType<RobotSelectionUI>();
            if (selectionUI != null)
                selectionUI.RefreshUnlockState();

            applied = DebugCompleteLevel4();
        }
        else
        {
            Debug.LogWarning($"[{nameof(GameplayDebugHotkeys)}] Level4 debug complete skipped after wait: no {nameof(Level4FlowController)} found.");
        }

        if (applied)
            ConsumedQuickCompleteScenes.Add(sceneName);

        _level4DebugCoroutine = null;
    }

    public static bool IsTransientDebugActiveForScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && TransientDebugScenes.Contains(sceneName);
    }

    public static void ClearTransientForScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        TransientDebugScenes.Remove(sceneName);
    }

    private static void MarkTransientForScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !SupportedScenes.Contains(sceneName))
            return;

        TransientDebugScenes.Add(sceneName);
    }

    private static void CommitDebugProgress(string sceneName)
    {
        ClearTransientForScene(sceneName);
        GameplaySaveManager.SaveCurrentGame();
    }

    private static bool TeleportLevel3PlayerToFinalPos()
    {
        ThirdPersonController player = FindFirstObjectByType<ThirdPersonController>();
        if (player == null)
        {
            Debug.LogWarning($"[{nameof(GameplayDebugHotkeys)}] Level3 debug teleport skipped: no {nameof(ThirdPersonController)} found.");
            return false;
        }

        Transform finalPos = FindSceneTransform("FinalPos");
        if (finalPos == null)
        {
            Debug.LogWarning($"[{nameof(GameplayDebugHotkeys)}] Level3 debug teleport skipped: no FinalPos found in active scene.");
            return false;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;

        player.transform.SetPositionAndRotation(finalPos.position, finalPos.rotation);

        if (characterController != null)
            characterController.enabled = true;

        return true;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Scene activeScene = SceneManager.GetActiveScene();
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.gameObject.scene != activeScene)
                continue;

            if (string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }
}
