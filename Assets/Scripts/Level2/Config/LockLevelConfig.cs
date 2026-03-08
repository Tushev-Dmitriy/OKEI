using UnityEngine;

[CreateAssetMenu(
    fileName = "LockLevelConfig_Default",
    menuName = "Level2/Lock Level Config",
    order = 0)]
public class LockLevelConfig : ScriptableObject
{
    [Header("Reload")]
    public LockReloadConfig reload;

    [Header("Core")]
    public LockCoreConfig core;

    [Header("Simulation")]
    public LockSimulationConfig simulation;

    [Header("Incident Timeline")]
    public LockIncidentTimelineConfig incidentTimeline;

    [Header("Incident Details")]
    public LockPressureLeakIncidentConfig pressureLeakIncident;
    public LockCoolingFaultIncidentConfig coolingFaultIncident;
    public LockFlowSurgeIncidentConfig flowSurgeIncident;
    public LockLiftJamIncidentConfig liftJamIncident;

    [Header("Visual")]
    public LockVisualConfig visual;

    [Header("Text")]
    public LockTextConfig text;

    [Header("Debug")]
    public LockDebugConfig debug;

    public void ApplyTo(object target)
    {
        if (target == null)
            return;

        ApplySection(reload, target);
        ApplySection(core, target);
        ApplySection(simulation, target);
        ApplySection(incidentTimeline, target);
        ApplySection(pressureLeakIncident, target);
        ApplySection(coolingFaultIncident, target);
        ApplySection(flowSurgeIncident, target);
        ApplySection(liftJamIncident, target);
        ApplySection(visual, target);
        ApplySection(text, target);
        ApplySection(debug, target);
    }

    private static void ApplySection(object section, object target)
    {
        if (section == null)
            return;

        LockConfigReflectionApplier.CopySharedFields(section, target);
    }
}
