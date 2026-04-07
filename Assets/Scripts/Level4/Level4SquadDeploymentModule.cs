using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4SquadDeploymentModule : MonoBehaviour
{
    private const float SquadSpawnHoldSeconds = 1.5f;
    private Coroutine _deployCoroutine;

    public void HandleRobotSelectionButtonClicked(Level4FlowController flow, RobotType selectedType)
    {
        if (flow == null || !flow.IsInFinalSection || flow.LevelCompleted)
            return;

        if (flow.FinalRunStarted)
            return;

        if (flow.IsFinalDeploying)
        {
            flow.StatusOverride = "Отряд уже разворачивается. Дождись выхода всех роботов.";
            flow.RefreshStatus();
            return;
        }

        int limit = flow.FinalSectionSpawnLimit;
        if (flow.PlannedFinalSquad.Count >= limit)
        {
            flow.StatusOverride = "Состав уже собран. Сейчас начнется выход роботов.";
            flow.RefreshStatus();
            return;
        }

        if (!flow.AttemptActive)
            flow.BeginFinalAssemblyFromModule();

        flow.PlannedFinalSquad.Add(selectedType);
        flow.StatusOverride = $"Добавлен: {flow.GetRobotDisplayName(selectedType)} ({flow.PlannedFinalSquad.Count}/{limit})";
        flow.RefreshStatus();

        if (flow.PlannedFinalSquad.Count >= limit)
            StartPlannedSquadDeployment(flow);
    }

    public void StartPlannedSquadDeployment(Level4FlowController flow)
    {
        if (flow == null)
            return;

        if (flow.PlannedFinalSquad.Count == 0 || flow.IsFinalDeploying || flow.FinalRunStarted)
            return;

        CancelDeployment();
        _deployCoroutine = StartCoroutine(DeployPlannedFinalSquadRoutine(flow));
    }

    public IEnumerator DeployPlannedFinalSquadRoutine(Level4FlowController flow)
    {
        if (flow == null)
            yield break;

        flow.IsFinalDeploying = true;
        flow.StatusOverride = "Развертываем отряд: роботы выходят по очереди.";
        flow.RefreshStatus();

        for (int i = 0; i < flow.PlannedFinalSquad.Count; i++)
        {
            if (!flow.IsInFinalSection || flow.FinalRunStarted)
                break;

            RobotSpawner spawner = flow.Spawner;
            RobotType typeToSpawn = flow.PlannedFinalSquad[i];
            if (spawner != null && spawner.SpawnPoint != null)
            {
                spawner.SpawnRobotOfType(
                    typeToSpawn,
                    spawner.SpawnPoint.position,
                    spawner.SpawnPoint.rotation,
                    registerAsCurrent: false,
                    bypassValidation: true);
            }

            yield return new WaitForSeconds(SquadSpawnHoldSeconds);
        }

        flow.IsFinalDeploying = false;
        _deployCoroutine = null;
        flow.RefreshStatus();
    }

    public void CancelDeployment()
    {
        if (_deployCoroutine == null)
            return;

        StopCoroutine(_deployCoroutine);
        _deployCoroutine = null;
    }

    private void OnDisable()
    {
        CancelDeployment();
    }
}
