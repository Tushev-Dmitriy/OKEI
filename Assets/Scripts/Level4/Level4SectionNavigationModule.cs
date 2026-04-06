using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4SectionNavigationModule : MonoBehaviour
{
    internal SectionDefinition DetermineCurrentSection(Level4FlowController flow)
    {
        if (flow == null)
            return null;

        return flow.ProgressStageValue switch
        {
            0 => GetSection(flow, Level4FlowController.SectionId.Base),
            1 => GetSection(flow, Level4FlowController.SectionId.Attacker),
            2 => GetSection(flow, Level4FlowController.SectionId.Healer),
            3 => GetSection(flow, Level4FlowController.SectionId.Defender),
            _ => GetSection(flow, Level4FlowController.SectionId.Final)
        };
    }

    internal SectionDefinition GetSection(Level4FlowController flow, Level4FlowController.SectionId id)
    {
        if (flow == null)
            return null;

        return flow.Sections.FirstOrDefault(section => section.Id == id);
    }

    internal void ApplySectionLayout(Level4FlowController flow, SectionDefinition section)
    {
        if (flow == null || section == null)
            return;

        Transform spawnTransform = null;
        bool hasSectionSpawn = flow.TryGetSceneTransformForModule(section.SpawnPointName, out spawnTransform);
        if (!hasSectionSpawn)
            flow.TryGetSceneTransformForModule("RobotSpawnPos", out spawnTransform);

        RobotSpawner spawner = flow.Spawner;
        if (spawner != null && spawner.SpawnPoint != null && spawnTransform != null)
        {
            spawner.SpawnPoint.position = spawnTransform.position;
            spawner.SpawnPoint.rotation = spawnTransform.rotation;
        }
    }

    internal void EnterSection(Level4FlowController flow, SectionDefinition section, string statusOverride)
    {
        if (flow == null)
            return;

        flow.CurrentSectionDef = section;
        ApplySectionLayout(flow, flow.CurrentSectionDef);
        flow.ResetSectionStateForModule(flow.CurrentSectionDef);
        flow.StatusOverride = statusOverride;
        SetSelectionForSection(flow, flow.CurrentSectionDef);

        if (flow.CurrentSectionDef != null && flow.CurrentSectionDef.Id == Level4FlowController.SectionId.Final && !flow.SquadHintShownThisSession)
            flow.ShowSquadModeHintForModule(showReminderOnly: true);

        flow.RefreshStatus();
    }

    internal void SetSelectionForSection(Level4FlowController flow, SectionDefinition section)
    {
        if (flow == null || flow.SelectionUIRef == null || section == null)
            return;

        flow.SelectionUIRef.SetSpawnOnSelectionEnabled(section.Id != Level4FlowController.SectionId.Final);

        RobotType selectionType = section.PreferredSelectionType != RobotType.None
            ? section.PreferredSelectionType
            : section.RequiredRobotType;

        if (selectionType != RobotType.None)
            flow.SelectionUIRef.SetSelectedRobot(selectionType, false);
    }
}

