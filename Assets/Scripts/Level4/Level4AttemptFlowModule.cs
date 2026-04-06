using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4AttemptFlowModule : MonoBehaviour
{
    public void BeginAttempt(Level4FlowController flow, Robot robot)
    {
        if (flow == null)
            return;

        flow.CleanupAttemptForModule(destroyPlayerRobot: false);
        flow.CloseAllSectionGatesForModule();

        flow.AttemptActiveValue = true;
        flow.PlayerRobotMutable = robot;
        flow.StageIndexMutable = 0;
        flow.ActiveWaveIndexValue = -1;
        flow.PlayerPulseTimer = 0f;
        flow.EscortPulseTimer = 0f;
        flow.StatusOverride = null;
        flow.StageTransitionLocked = false;

        if (flow.PlayerRobotMutable != null)
        {
            flow.SubscribePlayerRobotDeathForModule(flow.PlayerRobotMutable);
            flow.ApplyPlayerRobotTuningForModule(flow.PlayerRobotMutable);
            flow.PlayerRobotMutable.SetAutonomousMode(true);
        }

        InitializeSectionAttempt(flow);
        flow.RefreshStatus();
    }

    public void InitializeSectionAttempt(Level4FlowController flow)
    {
        if (flow == null || flow.CurrentSectionDef == null)
            return;

        switch (flow.CurrentSectionDef.Id)
        {
            case Level4FlowController.SectionId.Base:
            case Level4FlowController.SectionId.Attacker:
            case Level4FlowController.SectionId.Healer:
            case Level4FlowController.SectionId.Defender:
                flow.ActivateCorridorEnemiesForRunForModule();
                break;
        }
    }
}
