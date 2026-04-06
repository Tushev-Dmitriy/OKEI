using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4EnemyCorridorModule : MonoBehaviour
{
    public void DeactivateAllSceneEnemies(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.StopAllEnemyRespawnScaleCoroutines();
        foreach (EnemyUnit enemy in flow.SceneEnemiesMap.Values)
        {
            if (enemy != null)
                enemy.gameObject.SetActive(false);
        }
    }

    public void RestorePlacedSceneEnemies(Level4FlowController flow)
    {
        if (flow == null)
            return;

        for (int i = 0; i < flow.SceneEnemiesOrdered.Count; i++)
        {
            EnemyUnit enemy = flow.SceneEnemiesOrdered[i];
            if (enemy == null)
                continue;

            float health = flow.CorridorEnemyBaseHealthValue + (flow.CorridorEnemyHealthStepValue * i);
            float damage = flow.CorridorEnemyBaseDamageValue + (flow.CorridorEnemyDamageStepValue * i);
            enemy.gameObject.SetActive(true);
            enemy.Configure(health, damage);
            enemy.SetDestroyOnDeath(false);
            flow.PlayEnemyRespawnScaleForModule(enemy);
        }
    }

    public void ActivateCorridorEnemiesForRun(Level4FlowController flow)
    {
        if (flow == null)
            return;

        flow.ActiveEnemies.Clear();
        flow.SetActiveWaveIndexValue(-1);
        RestorePlacedSceneEnemies(flow);

        for (int i = 0; i < flow.SceneEnemiesOrdered.Count; i++)
        {
            EnemyUnit enemy = flow.SceneEnemiesOrdered[i];
            if (enemy == null || enemy.Health == null)
                continue;

            flow.ActiveEnemies.Add(enemy);
        }
    }
}
