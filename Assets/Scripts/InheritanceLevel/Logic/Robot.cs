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

    public RobotType RobotType => config != null ? config.robotType : RobotType.None;

    public static event System.Action<RobotType> OnRobotDied;

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
        
        health.TakeDamage(health.MaxHealth, transform.position);
    }

    protected virtual void OnDeath()
    {
        OnRobotDied?.Invoke(RobotType);
        Die();
    }

    protected void Die()
    {
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


