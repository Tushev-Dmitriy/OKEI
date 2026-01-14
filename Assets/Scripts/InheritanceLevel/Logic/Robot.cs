using UnityEngine;

[RequireComponent(typeof(RobotVisualController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CombatSystem))]
public class Robot : MonoBehaviour
{
    protected RobotConfigSO config;
    protected bool isAutonomous = false;
    protected RobotVisualController visualController;
    protected Health health;
    protected CombatSystem combatSystem;

    protected virtual void Awake()
    {
        visualController = GetComponent<RobotVisualController>();
        health = GetComponent<Health>();
        combatSystem = GetComponent<CombatSystem>();

        health.OnDeath += OnDeath;
    }

    public void Initialize(RobotConfigSO settings)
    {
        config = settings;
        visualController.InitializeVisuals(config);


        float maxHealth = config.maxHealth;
        float damagePerHit = config.damagePerHit;

        if (config.activeModules != null)
        {
            foreach (var module in config.activeModules)
            {
                switch (module)
                {
                    case VisualModuleType.Blaster:
                        damagePerHit += 5f;
                        break;
                    case VisualModuleType.Shield:
                        maxHealth += 25f;
                        break;
                }
            }
        }

        health.Initialize(maxHealth);
        combatSystem.InitializeCombat(damagePerHit, config.attackInterval);
    }

    public void ActivateAutonomousMode()
    {
        isAutonomous = true;
    }

    protected virtual void Update()
    {
        if (isAutonomous && config != null && health.IsAlive)
        {
            Move();
        }
    }

    protected virtual void Move()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.MovePosition(rb.position + transform.forward * config.moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.Translate(Vector3.forward * config.moveSpeed * Time.deltaTime);
        }
    }

    public virtual void TryEngageCombat(EnemyUnit enemy)
    {
        Debug.Log($"{gameObject.name} (базовый робот) не может сражаться");
        
        health.TakeDamage(health.MaxHealth);
    }

    protected virtual void OnDeath()
    {
        Debug.Log($"{gameObject.name} уничтожен в бою");
        Die();
    }

    protected void Die()
    {
        Debug.Log($"{gameObject.name} удаляется из игры");
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
    }
}
