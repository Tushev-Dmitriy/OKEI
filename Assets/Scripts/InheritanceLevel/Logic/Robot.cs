using System.Collections;
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
    private float moveSpeedMultiplier = 1f;
    [SerializeField] private float enemyDetectionRange = 18f;
    [SerializeField] private float enemyScanInterval = 0.25f;
    [SerializeField] private float engageDistance = 1.6f;
    [SerializeField, Min(0.1f)] private float deathFadeDuration = 0.65f;
    [SerializeField, Min(0f)] private float deathSinkDistance = 0.9f;
    private EnemyUnit cachedTargetEnemy;
    private float enemyScanTimer;
    private bool isDying;
    [SerializeField, Min(0.1f)] private float minRobotSpacing = 1.1f;
    [SerializeField] private LayerMask robotSpacingMask = ~0;
    private Vector3 _lastAudioPosition;

    protected virtual void Awake()
    {
        visualController = GetComponent<RobotVisualController>();
        health = GetComponent<Health>();
        combatSystem = GetComponent<CombatSystem>();

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        _lastAudioPosition = transform.position;
    }

    protected virtual void OnEnable()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (health != null)
        {
            health.OnDeath -= OnDeath;
            health.OnDeath += OnDeath;
        }
    }

    protected virtual void OnDisable()
    {
        StopMovementLoop();

        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
    }

    public RobotType RobotType => config != null ? config.robotType : RobotType.None;
    public RobotConfigSO Config => config;
    public Health Health => health;
    public CombatSystem CombatSystem => combatSystem;
    public bool IsAlive => health != null && health.IsAlive;
    public bool IsAutonomous => isAutonomous;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;

    public static event System.Action<RobotType> OnRobotDied;
    public event System.Action<Robot> Died;

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

    public void SetAutonomousMode(bool autonomous)
    {
        isAutonomous = autonomous;
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    protected virtual void Update()
    {
        if (isAutonomous && config != null && health.IsAlive && (combatSystem == null || !combatSystem.IsInCombat))
        {
            if (TryGetActiveEnemyTarget(out EnemyUnit enemy))
            {
                MoveTowardsEnemy(enemy);
                return;
            }

            Move();
        }

        UpdateMovementLoop();
    }

    protected virtual void Move()
    {
        MoveInDirection(transform.forward, GetEffectiveMoveSpeed());
    }

    protected float GetEffectiveMoveSpeed()
    {
        if (config == null)
            return 0f;

        return Mathf.Max(0f, config.moveSpeed) * moveSpeedMultiplier;
    }

    protected void MoveInDirection(Vector3 direction, float speed)
    {
        if (config == null)
            return;

        Vector3 normalizedDirection = direction.normalized;
        if (normalizedDirection.sqrMagnitude <= 0.0001f)
            return;

        float moveDistance = Mathf.Max(0f, speed) * Time.deltaTime;
        if (moveDistance <= 0f)
            return;

        Vector3 desiredPosition = transform.position + normalizedDirection * moveDistance;
        if (!CanOccupyPosition(desiredPosition))
            return;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            // Let physics resolve contacts naturally so robots can actually collide with enemies and enter combat.
            rb.MovePosition(rb.position + normalizedDirection * moveDistance);
            return;
        }

        transform.position = desiredPosition;
    }

    public virtual void TryEngageCombat(EnemyUnit enemy)
    {
        Health enemyHealth = enemy != null ? enemy.GetComponent<Health>() : null;

        if (enemyHealth != null && enemyHealth.IsAlive)
        {
            combatSystem.StartCombat(enemyHealth);
        }
    }

    public virtual float ModifyIncomingDamage(float damage)
    {
        return damage;
    }

    protected virtual void OnDeath()
    {
        if (isDying)
            return;

        OnRobotDied?.Invoke(RobotType);
        Died?.Invoke(this);
        Die();
    }

    protected void Die()
    {
        if (isDying)
            return;

        StopMovementLoop();
        StartCoroutine(DeathFadeRoutine());
    }

    protected virtual void OnDestroy()
    {
        OnDisable();
    }

    private void UpdateMovementLoop()
    {
        if (!Application.isPlaying)
            return;

        bool shouldPlay = IsAlive && (transform.position - _lastAudioPosition).sqrMagnitude > 0.0004f;
        _lastAudioPosition = transform.position;

        if (!shouldPlay)
        {
            StopMovementLoop();
            return;
        }

        GameAudio.SetLoop(this, "movement", GetMovementLoopCueId(), true, 0.28f, 1.5f, 16f);
    }

    private void StopMovementLoop()
    {
        GameAudio.SetLoop(this, "movement", GetMovementLoopCueId(), false);
    }

    private string GetMovementLoopCueId()
    {
        return RobotType switch
        {
            RobotType.Attacker => AudioCueIds.Level4RobotMoveAttackerLoop,
            RobotType.Healer => AudioCueIds.Level4RobotMoveHealerLoop,
            RobotType.Defender => AudioCueIds.Level4RobotMoveDefenderLoop,
            _ => AudioCueIds.Level4RobotMoveBaseLoop
        };
    }

    private bool TryGetActiveEnemyTarget(out EnemyUnit enemy)
    {
        enemy = null;

        if (enemyDetectionRange <= 0f)
            return false;

        if (cachedTargetEnemy != null && IsEnemyTargetValid(cachedTargetEnemy) && IsEnemyInRange(cachedTargetEnemy, enemyDetectionRange * 1.2f))
        {
            enemy = cachedTargetEnemy;
            return true;
        }

        enemyScanTimer -= Time.deltaTime;
        if (enemyScanTimer > 0f)
            return false;

        enemyScanTimer = Mathf.Max(0.05f, enemyScanInterval);
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        float maxDistanceSqr = enemyDetectionRange * enemyDetectionRange;
        float bestDistanceSqr = maxDistanceSqr;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyUnit candidate = enemies[i];
            if (!IsEnemyTargetValid(candidate))
                continue;

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr <= bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                cachedTargetEnemy = candidate;
            }
        }

        if (cachedTargetEnemy != null && IsEnemyTargetValid(cachedTargetEnemy) && IsEnemyInRange(cachedTargetEnemy, enemyDetectionRange * 1.2f))
        {
            enemy = cachedTargetEnemy;
            return true;
        }

        return false;
    }

    private void MoveTowardsEnemy(EnemyUnit enemy)
    {
        if (enemy == null)
            return;

        Vector3 toEnemy = enemy.transform.position - transform.position;
        toEnemy.y = 0f;

        float sqrDistance = toEnemy.sqrMagnitude;
        if (sqrDistance <= engageDistance * engageDistance)
        {
            TryEngageCombat(enemy);
            return;
        }

        if (toEnemy.sqrMagnitude > 0.0001f)
        {
            Vector3 targetDirection = toEnemy.normalized;
            float turnSpeed = config != null ? Mathf.Max(1f, config.rotationSpeed) : 10f;
            transform.forward = Vector3.RotateTowards(transform.forward, targetDirection, turnSpeed * Time.deltaTime, 0f);
            MoveInDirection(transform.forward, GetEffectiveMoveSpeed());
        }
    }

    private static bool IsEnemyTargetValid(EnemyUnit enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Health == null || !enemy.Health.IsAlive)
            return false;

        return true;
    }

    private bool IsEnemyInRange(EnemyUnit enemy, float range)
    {
        if (enemy == null)
            return false;

        float maxDistanceSqr = range * range;
        return (enemy.transform.position - transform.position).sqrMagnitude <= maxDistanceSqr;
    }

    private IEnumerator DeathFadeRoutine()
    {
        isDying = true;
        isAutonomous = false;
        cachedTargetEnemy = null;
        enemyScanTimer = 0f;

        if (combatSystem != null)
        {
            combatSystem.StopCombat();
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition - Vector3.up * deathSinkDistance;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 0.05f;
        float duration = Mathf.Max(0.1f, deathFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPosition, endPosition, eased);
            transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }

        Destroy(gameObject);
    }

    private bool CanOccupyPosition(Vector3 desiredPosition)
    {
        float radius = Mathf.Max(0.1f, minRobotSpacing * 0.5f);
        Vector3 probe = desiredPosition + Vector3.up * 0.35f;
        Collider[] hits = Physics.OverlapSphere(probe, radius, robotSpacingMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            Robot other = hit.GetComponentInParent<Robot>();
            if (other == null || other == this || !other.IsAlive)
                continue;

            return false;
        }

        return true;
    }
}
