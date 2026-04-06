using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4CombatRuntimeModule : MonoBehaviour
{
    public bool AreAllActiveEnemiesDefeated(Level4FlowController flow)
    {
        if (flow == null)
            return true;

        for (int i = flow.ActiveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyUnit enemy = flow.ActiveEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Health == null || !enemy.Health.IsAlive)
                flow.ActiveEnemies.RemoveAt(i);
        }

        return flow.ActiveEnemies.Count == 0;
    }

    public float GetHealthRatio(Robot robot)
    {
        if (robot == null || robot.Health == null || robot.Health.MaxHealth <= 0f)
            return 0f;

        return robot.Health.CurrentHealth / robot.Health.MaxHealth;
    }

    public bool HasLivingFinalRobot(Level4FlowController flow)
    {
        if (flow == null)
            return false;

        for (int i = 0; i < flow.FinalSquad.Count; i++)
        {
            Robot robot = flow.FinalSquad[i];
            if (robot != null && robot.IsAlive)
                return true;
        }

        return false;
    }

    public int GetLivingFinalRobotCount(Level4FlowController flow)
    {
        if (flow == null)
            return 0;

        int count = 0;
        for (int i = 0; i < flow.FinalSquad.Count; i++)
        {
            Robot robot = flow.FinalSquad[i];
            if (robot != null && robot.IsAlive)
                count++;
        }

        return count;
    }

    public void ApplyPlayerPulseIfNeeded(Level4FlowController flow, float interval, float damage, bool enabled)
    {
        if (flow == null)
            return;

        Robot player = flow.PlayerRobotRef;
        if (!enabled || interval <= 0f || damage <= 0f || player == null || player.Health == null || !player.IsAlive)
            return;

        flow.PlayerPulseTimer -= Time.deltaTime;
        if (flow.PlayerPulseTimer > 0f)
            return;

        float adjustedDamage = player.ModifyIncomingDamage(damage);
        player.Health.TakeDamage(adjustedDamage, player.transform.position);
        flow.PlayerPulseTimer = interval;
        flow.RefreshStatus();
    }

    public void ApplyEscortPulseIfNeeded(Level4FlowController flow, float interval, float damage, bool enabled)
    {
        if (flow == null)
            return;

        Robot escort = flow.EscortRobotRef;
        if (!enabled || interval <= 0f || damage <= 0f || escort == null || escort.Health == null || !escort.IsAlive)
            return;

        flow.EscortPulseTimer -= Time.deltaTime;
        if (flow.EscortPulseTimer > 0f)
            return;

        float adjustedDamage = escort.ModifyIncomingDamage(damage);
        escort.Health.TakeDamage(adjustedDamage, escort.transform.position);
        flow.EscortPulseTimer = interval;
        flow.RefreshStatus();
    }

    public void ApplyFinalSquadPulseIfNeeded(Level4FlowController flow, float interval, float damage, bool enabled)
    {
        if (flow == null || !enabled || interval <= 0f || damage <= 0f || !flow.FinalRunStartedValue)
            return;

        flow.SquadPulseTimer -= Time.deltaTime;
        if (flow.SquadPulseTimer > 0f)
            return;

        for (int i = 0; i < flow.FinalSquad.Count; i++)
        {
            Robot robot = flow.FinalSquad[i];
            if (robot == null || !robot.IsAlive || robot.Health == null)
                continue;

            float adjustedDamage = robot.ModifyIncomingDamage(damage);
            robot.Health.TakeDamage(adjustedDamage, robot.transform.position);
        }

        flow.SquadPulseTimer = interval;
        flow.RefreshStatus();
    }
}
