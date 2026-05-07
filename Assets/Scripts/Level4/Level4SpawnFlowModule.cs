using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4SpawnFlowModule : MonoBehaviour
{
    public SpawnPermissionResult ValidateSpawnRequest(Level4FlowController flow, RobotType requestedType)
    {
        if (flow == null)
            return SpawnPermissionResult.Deny("Текущая секция еще не инициализирована.");

        if (flow.LevelCompleted)
            return SpawnPermissionResult.Deny("Эта версия уровня уже пройдена.");

        if (!flow.HasCurrentSection)
            return SpawnPermissionResult.Deny("Текущая секция еще не инициализирована.");

        if (flow.UnlockManager != null && !flow.UnlockManager.IsRobotUnlocked(requestedType))
            return SpawnPermissionResult.Deny("Этот тип робота еще не открыт.");

        if (flow.CurrentSectionIsFinal)
        {
            if (flow.FinalRunStarted)
                return SpawnPermissionResult.Deny("Финальный отряд уже запущен. Дождись результата и попробуй снова.");

            return SpawnPermissionResult.Deny("В режиме отряда: выбери 5 роботов кликами по иконкам, они выйдут по очереди автоматически.");
        }

        if (flow.AttemptActive)
            return SpawnPermissionResult.Deny("Сначала заверши текущую попытку, затем запускай нового робота.");

        if (requestedType != flow.CurrentSectionRequiredRobotType)
            return SpawnPermissionResult.Deny($"Используйте этого робота: {flow.GetRobotDisplayName(flow.CurrentSectionRequiredRobotType)}");

        return SpawnPermissionResult.Allow();
    }

    public void HandleRobotSpawned(Level4FlowController flow, Robot robot)
    {
        if (flow == null || flow.SuppressSpawnedRobotHandling || robot == null || !flow.HasCurrentSection || flow.LevelCompleted)
            return;

        if (flow.CurrentSectionIsFinal)
        {
            flow.HandleFinalRobotSpawnedForModule(robot);
            return;
        }

        flow.BeginAttemptForModule(robot);
    }

    public void HandleSpawnDenied(Level4FlowController flow, RobotType robotType, string reason)
    {
        if (flow == null)
            return;

        bool isCooldown = !string.IsNullOrWhiteSpace(reason) && reason.IndexOf("cooldown", System.StringComparison.OrdinalIgnoreCase) >= 0;
        GameAudio.PlayUi(isCooldown ? AudioCueIds.Level4RobotCooldownDenied : AudioCueIds.Level4RobotSpawnDenied, 0.95f);
        flow.StatusOverride = string.IsNullOrWhiteSpace(reason)
            ? $"Сейчас нельзя запустить {flow.GetRobotDisplayName(robotType)}."
            : reason;

        TryReplayRequiredRobotUnlockHint(flow, robotType);
        flow.RefreshStatus();
    }

    public void TryReplayRequiredRobotUnlockHint(Level4FlowController flow, RobotType requestedType)
    {
        if (flow == null || flow.CurrentSectionRequiredRobotType == RobotType.None)
            return;

        RobotType requiredType = flow.CurrentSectionRequiredRobotType;
        if (requestedType == requiredType)
            return;

        if (flow.UnlockManager == null || !flow.UnlockManager.IsRobotUnlocked(requiredType))
            return;

        if (Time.unscaledTime - flow.LastUnlockHintTime < flow.UnlockHintReplayCooldownValue && flow.LastHintRobotType == requiredType)
            return;

        RobotUnlockHintUI hintUi = FindFirstObjectByType<RobotUnlockHintUI>();
        if (hintUi == null)
            return;

        hintUi.ShowHintForRobot(requiredType);
        flow.LastUnlockHintTime = Time.unscaledTime;
        flow.LastHintRobotType = requiredType;
    }

    public void ShowSquadModeHint(Level4FlowController flow, bool showReminderOnly)
    {
        if (flow == null)
            return;

        RobotUnlockHintUI hintUi = FindFirstObjectByType<RobotUnlockHintUI>();
        if (hintUi == null)
            return;

        string message = showReminderOnly ? flow.SquadReminderHintText : flow.SquadUnlockedHintText;
        hintUi.ShowSystemHint("Режим отряда", message, flow.GetRobotIconForModule(RobotType.Defender));
        flow.SquadHintShownThisSession = true;
    }

    public void HandleProgressApplied(Level4FlowController flow)
    {
        if (flow == null || flow.SuppressProgressRefresh)
            return;

        flow.LevelCompletedValue = false;
        flow.RefreshSectionFromProgressForModule();
    }
}
