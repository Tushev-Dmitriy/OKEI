using UnityEngine;

public class AssaultRobot : Robot
{
    public override void TryEngageCombat(EnemyUnit enemy)
    {

        Health enemyHealth = enemy.GetComponent<Health>();
        
        if (enemyHealth != null && enemyHealth.IsAlive)
        {
            combatSystem.StartCombat(enemyHealth);
        }
        else
        {
        }
    }
}


