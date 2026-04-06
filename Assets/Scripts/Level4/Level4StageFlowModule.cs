using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4StageFlowModule : MonoBehaviour
{
    public void AdvanceStage(Level4FlowController flow, string message)
    {
        if (flow == null || flow.StageTransitionLocked)
            return;

        flow.StageTransitionLocked = true;
        flow.StageIndexMutable++;
        flow.StatusOverride = message;
        flow.RefreshStatus();
        flow.ScheduleUnlockStageTransitionForModule();
    }

    public void UnlockStageTransition(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.StageTransitionLocked = false;
        flow.StatusOverride = null;
        flow.RefreshStatus();
    }
}
