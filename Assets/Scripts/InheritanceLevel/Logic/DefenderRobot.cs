using UnityEngine;

public class DefenderRobot : Robot
{
    public override void TryEngageCombat(EnemyUnit enemy)
    {
        var enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null && enemyHealth.IsAlive)
        {
            combatSystem.StartCombat(enemyHealth);
        }
    }
}
