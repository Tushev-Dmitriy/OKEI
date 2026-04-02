using UnityEngine;

public class DefenderRobot : Robot
{
    [SerializeField] private float incomingDamageMultiplier = 0.55f;

    public override void TryEngageCombat(EnemyUnit enemy)
    {
        var enemyHealth = enemy.GetComponent<Health>();
        if (enemyHealth != null && enemyHealth.IsAlive)
        {
            combatSystem.StartCombat(enemyHealth);
        }
    }

    public override float ModifyIncomingDamage(float damage)
    {
        return damage * incomingDamageMultiplier;
    }
}
