using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyUnit : MonoBehaviour
{
    [Header("Enemy Attack Settings")]
    [SerializeField] private float enemyDamage = 10f;
    [SerializeField] private ParticleSystem attackEffect;

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

            Health robotHealth = incomingRobot.GetComponent<Health>();
            if (robotHealth != null && robotHealth.IsAlive)
            {
                Vector3 hitPosition = other.transform.position;
                if (other.contactCount > 0)
                {
                    hitPosition = other.GetContact(0).point;
                }
                robotHealth.TakeDamage(enemyDamage, hitPosition);
                if (attackEffect != null)
                {
                    attackEffect.Play();
                }
            }
        }
    }

    private void OnDeath()
    {
        StopAttackEffect();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
        StopAttackEffect();
    }

    private void StopAttackEffect()
    {
        if (attackEffect != null && attackEffect.isPlaying)
        {
            attackEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}

