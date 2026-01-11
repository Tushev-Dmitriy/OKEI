using UnityEngine;

public class AssaultRobot : Robot
{
    public override void TryEngageCombat(EnemyUnit enemy)
    {
        Debug.Log($"Враг уничтожен атакующим роботом");

        enemy.TakeDamage();
    }
}
