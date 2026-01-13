using UnityEngine;

public class AssaultRobot : Robot
{
    public override void TryEngageCombat(EnemyUnit enemy)
    {
        Debug.Log($"{gameObject.name} (атакующий робот) вступает в бой с {enemy.gameObject.name}");

        Health enemyHealth = enemy.GetComponent<Health>();
        
        if (enemyHealth != null && enemyHealth.IsAlive)
        {
            combatSystem.StartCombat(enemyHealth);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Враг не имеет компонента Health или уже мёртв");
        }
    }
}
