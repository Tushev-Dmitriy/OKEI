using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4FlowSetupModule : MonoBehaviour
{
    public void RunAwake(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.ResolveReferences();
        flow.HideLevelUpWindowAtStartup();
        flow.NormalizeLocalizedHintText();
        flow.EnsureSquadHud();
        flow.DisableLegacySceneHelpers();
        flow.BuildSections();
        flow.CacheSceneObjects();
        flow.EnsureRuntimeFinalSectionLayout();
        flow.CacheSceneObjects();
        flow.PrepareStaticScene();
    }

    public void RunStart(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.LoadProgressStage();
        flow.RefreshSectionFromProgress();
    }
}
