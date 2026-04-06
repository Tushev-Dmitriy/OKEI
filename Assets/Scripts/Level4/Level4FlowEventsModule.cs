using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4FlowEventsModule : MonoBehaviour
{
    public void RunOnEnable(Level4FlowController flow)
    {
        if (flow == null)
            return;

        if (flow.Spawner != null)
        {
            flow.Spawner.OnSpawnRequested += flow.ValidateSpawnRequest;
            flow.Spawner.OnRobotSpawned += flow.HandleRobotSpawned;
            flow.Spawner.OnSpawnDenied += flow.HandleSpawnDenied;
        }

        RobotSelectionUI.OnAnyRobotButtonClicked += flow.HandleRobotSelectionButtonClicked;

        if (flow.UnlockManager != null)
        {
            flow.UnlockManager.OnProgressApplied += flow.HandleProgressApplied;
        }

        EnemyUnit.OnEnemyDied += flow.HandleEnemyDied;
    }

    public void RunOnDisable(Level4FlowController flow)
    {
        if (flow == null)
            return;

        EnemyUnit.OnEnemyDied -= flow.HandleEnemyDied;

        if (flow.Spawner != null)
        {
            flow.Spawner.OnSpawnRequested -= flow.ValidateSpawnRequest;
            flow.Spawner.OnRobotSpawned -= flow.HandleRobotSpawned;
            flow.Spawner.OnSpawnDenied -= flow.HandleSpawnDenied;
        }

        RobotSelectionUI.OnAnyRobotButtonClicked -= flow.HandleRobotSelectionButtonClicked;

        if (flow.UnlockManager != null)
        {
            flow.UnlockManager.OnProgressApplied -= flow.HandleProgressApplied;
        }

        if (flow.SquadHudClearButton != null)
        {
            flow.SquadHudClearButton.onClick.RemoveListener(flow.HandleSquadHudClearClicked);
        }

        flow.StopAllEnemyRespawnScaleCoroutines();
        flow.CleanupAttempt(destroyPlayerRobot: true);
        flow.PrepareStaticScene();
    }
}
