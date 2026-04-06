using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4FlowUpdateModule : MonoBehaviour
{
    public void RunUpdate(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.TickCurrentSection();
    }
}
