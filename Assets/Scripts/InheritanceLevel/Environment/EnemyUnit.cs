using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyUnit : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += OnDeath;
    }

    private void OnCollisionEnter(Collision other)
    {
        Robot incomingRobot = other.gameObject.GetComponent<Robot>();

        if (incomingRobot != null)
        {
            incomingRobot.TryEngageCombat(this);
        }
    }

    public void TakeDamage()
    {
        Debug.Log($"{gameObject.name} (враг) получил урон через устаревший метод");
        health.TakeDamage(health.MaxHealth);
    }

    private void OnDeath()
    {
        Debug.Log($"{gameObject.name} (враг) повержен");
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
    }
}
