using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4ProgressModule : MonoBehaviour
{
    public void TrySaveProgress(Level4FlowController flow)
    {
        if (flow == null)
            return;

        PlayerSaver saver = FindFirstObjectByType<PlayerSaver>();
        if (saver != null)
        {
            saver.SavePlayerData();
            return;
        }

        GameplaySaveManager saveManager = FindFirstObjectByType<GameplaySaveManager>();
        if (saveManager != null)
        {
            saveManager.SaveNow();
        }
    }

    public void LoadProgressStage(Level4FlowController flow)
    {
        if (flow == null)
            return;

        PlayerSaveSystem.Load(out SaveData saveData);
        int savedStage = saveData?.gameplayProgress != null
            ? saveData.gameplayProgress.level4ProgressStage
            : PlayerPrefs.GetInt(flow.ProgressStagePrefsKeyName, 0);

        flow.ProgressStage = Mathf.Clamp(savedStage, 0, 4);
        ClampProgressStageToUnlocks(flow);
    }

    public void SetProgressStage(Level4FlowController flow, int stage)
    {
        if (flow == null)
            return;

        flow.ProgressStage = Mathf.Clamp(stage, 0, 4);

        PlayerSaveSystem.Load(out SaveData saveData);
        saveData ??= new SaveData();
        saveData.gameplayProgress ??= new GameplayProgressData();
        saveData.gameplayProgress.level4ProgressStage = flow.ProgressStage;
        PlayerSaveSystem.Save(saveData);
    }

    public void ClampProgressStageToUnlocks(Level4FlowController flow)
    {
        if (flow == null)
            return;

        int highestUnlockedStage = 0;

        if (flow.UnlockManager != null)
        {
            if (flow.UnlockManager.IsRobotUnlocked(RobotType.Attacker))
                highestUnlockedStage = Mathf.Max(highestUnlockedStage, 1);
            if (flow.UnlockManager.IsRobotUnlocked(RobotType.Healer))
                highestUnlockedStage = Mathf.Max(highestUnlockedStage, 2);
            if (flow.UnlockManager.IsRobotUnlocked(RobotType.Defender))
                highestUnlockedStage = Mathf.Max(highestUnlockedStage, 3);
        }

        int maxAllowedStage = highestUnlockedStage >= 3 ? 4 : highestUnlockedStage;
        int minAllowedStage = highestUnlockedStage >= 3 ? 4 : highestUnlockedStage;

        flow.ProgressStage = Mathf.Clamp(flow.ProgressStage, minAllowedStage, maxAllowedStage);
    }

    public bool IsSquadModeUnlocked(Level4FlowController flow)
    {
        if (flow == null)
            return false;

        if (flow.FinalSectionUnlocked || flow.ProgressStage >= 4)
            return true;

        if (flow.UnlockManager == null)
            return false;

        return flow.UnlockManager.IsRobotUnlocked(RobotType.Attacker)
            && flow.UnlockManager.IsRobotUnlocked(RobotType.Healer)
            && flow.UnlockManager.IsRobotUnlocked(RobotType.Defender);
    }
}
