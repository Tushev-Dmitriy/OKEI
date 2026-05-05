using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class Level4SectionLifecycleModule : MonoBehaviour
{
    internal void SpawnEscortRobot(Level4FlowController flow)
    {
        if (flow == null || flow.Spawner == null || flow.CurrentSectionDef == null || string.IsNullOrWhiteSpace(flow.CurrentSectionDef.EscortSpawnPointName))
            return;

        if (!flow.TryGetSceneTransformForModule(flow.CurrentSectionDef.EscortSpawnPointName, out Transform escortSpawn))
            return;

        try
        {
            flow.SuppressSpawnedRobotHandling = true;
            flow.EscortRobotMutable = flow.Spawner.SpawnRobotOfType(
                RobotType.Attacker,
                escortSpawn.position,
                escortSpawn.rotation,
                registerAsCurrent: false,
                bypassValidation: true);
        }
        finally
        {
            flow.SuppressSpawnedRobotHandling = false;
        }

        if (flow.EscortRobotMutable == null)
            return;

        flow.SubscribeEscortRobotDeathForModule(flow.EscortRobotMutable);
        flow.ApplyPlayerRobotTuningForModule(flow.EscortRobotMutable);
        flow.EscortRobotMutable.SetAutonomousMode(false);

        if (flow.EscortRobotMutable.Health != null)
        {
            float desiredHealth = flow.EscortRobotMutable.Health.MaxHealth * Mathf.Clamp01(flow.CurrentSectionDef.EscortStartHealthRatio);
            float damageAmount = Mathf.Max(0f, flow.EscortRobotMutable.Health.CurrentHealth - desiredHealth);
            if (damageAmount > 0f)
                flow.EscortRobotMutable.Health.TakeDamage(damageAmount, escortSpawn.position);
        }
    }

    internal void AdvanceAfterRequiredRobotTest(Level4FlowController flow, string message, bool openGate)
    {
        if (flow == null || flow.CurrentSectionDef == null)
            return;

        Level4FlowController.SectionId completedSection = flow.CurrentSectionDef.Id;
        RobotType unlockType = flow.CurrentSectionDef.UnlockOnSuccess;

        if (openGate)
            flow.SetGateClosedForModule(flow.CurrentSectionDef.ExitGateName, closed: false);

        CleanupAttempt(flow, destroyPlayerRobot: true);

        if (completedSection == Level4FlowController.SectionId.Defender)
        {
            flow.SetProgressStageForModule(4);
            flow.FinalSectionUnlockedValue = true;
            flow.EnterSectionForModule(flow.GetSectionForModule(Level4FlowController.SectionId.Final), message);
            flow.ShowSquadModeHintForModule(showReminderOnly: false);
            flow.TrySaveProgressForModule();
            return;
        }

        if (flow.UnlockManager != null && unlockType != RobotType.None)
        {
            flow.SuppressProgressRefresh = true;
            flow.UnlockManager.UnlockRobot(unlockType);
            flow.SuppressProgressRefresh = false;
            flow.TrySaveProgressForModule();
        }

        flow.SetProgressStageForModule(Mathf.Min(4, flow.ProgressStage + 1));
        flow.EnterSectionForModule(flow.DetermineCurrentSectionForModule(), message);
    }

    internal void CompleteCurrentSection(Level4FlowController flow)
    {
        if (flow == null || flow.CurrentSectionDef == null || flow.LevelCompleted)
            return;

        if (flow.CurrentSectionIsFinal)
        {
            flow.SetGateClosedForModule(flow.CurrentSectionDef.ExitGateName, closed: false);
            CleanupAttempt(flow, destroyPlayerRobot: true);
            flow.LevelCompletedValue = true;
            flow.TrySaveProgressForModule();
            flow.StatusOverride = flow.CurrentSectionDef.SuccessText;
            flow.RefreshStatus();

            bool transitionStarted = flow.TryStartCompletionTransitionForModule();
            if (!transitionStarted)
            {
                LevelProgressManager.CompleteLevel(flow.CompletedLevelIndex);
                flow.TrySaveProgressForModule();

                int targetBuildIndex = flow.CompletionTargetSceneBuildIndex;
                if (targetBuildIndex >= 0)
                    SceneManager.LoadScene(targetBuildIndex, LoadSceneMode.Single);
            }

            return;
        }

        AdvanceAfterRequiredRobotTest(flow, flow.CurrentSectionDef.SuccessText, openGate: true);
    }

    internal void FailCurrentSection(Level4FlowController flow, string message)
    {
        if (flow == null)
            return;

        CleanupAttempt(flow, destroyPlayerRobot: true);
        flow.EnterSectionForModule(flow.CurrentSectionDef, message);
    }

    internal void RefreshSectionFromProgress(Level4FlowController flow)
    {
        if (flow == null)
            return;

        CleanupAttempt(flow, destroyPlayerRobot: true);
        flow.ClampProgressStageToUnlocksForModule();
        flow.FinalSectionUnlockedValue = flow.ProgressStage >= 4;
        flow.EnterSectionForModule(flow.DetermineCurrentSectionForModule(), null);
    }

    internal void ResetSectionState(Level4FlowController flow, SectionDefinition section)
    {
        if (flow == null)
            return;

        flow.CloseAllSectionGatesForModule();
        flow.RestorePlacedSceneEnemiesForModule();
        flow.CancelSquadDeploymentForModule();
        flow.PlannedFinalSquad.Clear();
        flow.IsFinalDeploying = false;

        if (flow.EscortRobotMutable != null)
        {
            flow.UnsubscribeEscortRobotDeathForModule(flow.EscortRobotMutable);
            Destroy(flow.EscortRobotMutable.gameObject);
            flow.EscortRobotMutable = null;
        }

        flow.ActiveWaveIndexValue = -1;
        flow.StageIndexMutable = 0;
        flow.FinalRunStartedMutable = false;
        flow.SquadPulseTimer = 0f;
    }

    internal void CleanupAttempt(Level4FlowController flow, bool destroyPlayerRobot)
    {
        if (flow == null)
            return;

        flow.CancelUnlockStageTransitionInvokeForModule();
        flow.CancelSquadDeploymentForModule();

        flow.IsFinalDeploying = false;
        flow.AttemptActiveValue = false;
        flow.StageTransitionLocked = false;
        flow.StageIndexMutable = 0;
        flow.ActiveWaveIndexValue = -1;
        flow.PlayerPulseTimer = 0f;
        flow.EscortPulseTimer = 0f;
        flow.FinalCommittedAttackers = 0;
        flow.FinalCommittedHealers = 0;
        flow.FinalCommittedDefenders = 0;
        flow.FinalCommittedBases = 0;
        flow.FinalCommittedTotal = 0;
        flow.SquadPulseTimer = 0f;
        flow.FinalRunStartedMutable = false;
        flow.PlannedFinalSquad.Clear();

        if (flow.PlayerRobotMutable != null)
        {
            flow.UnsubscribePlayerRobotDeathForModule(flow.PlayerRobotMutable);

            if (destroyPlayerRobot)
            {
                if (flow.Spawner != null)
                    flow.Spawner.ClearCurrentRobot(flow.PlayerRobotMutable);

                if (flow.PlayerRobotMutable.IsAlive)
                    Destroy(flow.PlayerRobotMutable.gameObject);
            }

            flow.PlayerRobotMutable = null;
        }

        if (flow.EscortRobotMutable != null)
        {
            flow.UnsubscribeEscortRobotDeathForModule(flow.EscortRobotMutable);
            if (flow.EscortRobotMutable.IsAlive)
                Destroy(flow.EscortRobotMutable.gameObject);
            flow.EscortRobotMutable = null;
        }

        for (int i = 0; i < flow.FinalSquad.Count; i++)
        {
            Robot squadRobot = flow.FinalSquad[i];
            if (squadRobot == null)
                continue;

            flow.UnsubscribeFinalRobotDeathForModule(squadRobot);

            if (destroyPlayerRobot)
            {
                if (flow.Spawner != null)
                    flow.Spawner.ClearCurrentRobot(squadRobot);

                if (squadRobot.IsAlive)
                    Destroy(squadRobot.gameObject);
            }
        }

        flow.FinalSquad.Clear();
        flow.RestorePlacedSceneEnemiesForModule();
        flow.ActiveEnemies.Clear();
    }
}

