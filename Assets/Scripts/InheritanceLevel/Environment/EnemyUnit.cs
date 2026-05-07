using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyUnit : MonoBehaviour
{
    [Header("Enemy Attack Settings")]
    [SerializeField] private float enemyDamage = 10f;
    [SerializeField] private float attackInterval = 0.75f;
    [SerializeField] private ParticleSystem attackEffect;
    [SerializeField] private bool autoResolveAttackEffect = true;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField, Min(0.1f)] private float deathFadeDuration = 0.65f;
    [SerializeField, Min(0f)] private float deathSinkDistance = 0.75f;

    private Health health;
    private Health currentRobotTarget;
    private Coroutine attackRoutine;
    private bool isDying;

    public static event System.Action<EnemyUnit> OnEnemyDied;
    public event System.Action<EnemyUnit> Died;

    public Health Health => health;
    public float EnemyDamage => enemyDamage;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath += OnDeath;
        TryResolveAttackEffect();
    }

    public void Configure(float maxHealth, float damage)
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (health != null)
        {
            health.Initialize(maxHealth);
        }

        enemyDamage = Mathf.Max(0f, damage);
    }

    public void SetEnemyDamage(float damage)
    {
        enemyDamage = Mathf.Max(0f, damage);
    }

    public void SetDestroyOnDeath(bool shouldDestroy)
    {
        destroyOnDeath = shouldDestroy;
    }

    private void OnCollisionEnter(Collision other)
    {
        Robot incomingRobot = other.gameObject.GetComponentInParent<Robot>();

        if (incomingRobot == null)
            return;

        incomingRobot.TryEngageCombat(this);
        StartAttacking(incomingRobot, other);
    }

    private void OnCollisionStay(Collision other)
    {
        if (attackRoutine != null)
            return;

        Robot incomingRobot = other.gameObject.GetComponentInParent<Robot>();
        if (incomingRobot == null)
            return;

        incomingRobot.TryEngageCombat(this);
        StartAttacking(incomingRobot, other);
    }

    private void StartAttacking(Robot incomingRobot, Collision other)
    {
        if (incomingRobot == null || health == null || !health.IsAlive)
            return;

        Health robotHealth = incomingRobot.GetComponent<Health>();
        if (robotHealth == null || !robotHealth.IsAlive)
            return;

        if (currentRobotTarget != null)
        {
            currentRobotTarget.OnDeath -= HandleCurrentTargetDeath;
        }

        currentRobotTarget = robotHealth;
        currentRobotTarget.OnDeath += HandleCurrentTargetDeath;

        if (attackRoutine != null)
            return;

        Vector3 hitPosition = other.transform.position;
        if (other.contactCount > 0)
        {
            hitPosition = other.GetContact(0).point;
        }

        attackRoutine = StartCoroutine(AttackRobotRoutine(hitPosition));
    }

    private IEnumerator AttackRobotRoutine(Vector3 fallbackHitPosition)
    {
        while (health != null && health.IsAlive && currentRobotTarget != null && currentRobotTarget.IsAlive)
        {
            Robot robot = currentRobotTarget.GetComponent<Robot>();
            float appliedDamage = robot != null ? robot.ModifyIncomingDamage(enemyDamage) : enemyDamage;
            Vector3 hitPosition = fallbackHitPosition;

            Collider targetCollider = currentRobotTarget.GetComponent<Collider>();
            if (targetCollider != null)
            {
                hitPosition = targetCollider.ClosestPoint(transform.position);
            }

            GameAudio.PlayAtPoint(AudioCueIds.Level4EnemyAttack, transform.position, 0.9f, 1f, 20f);
            GameAudio.PlayAtPoint(AudioCueIds.Level4LaserFire, transform.position, 0.65f, 1f, 22f);
            GameAudio.PlayGlobal(AudioCueIds.Level4EnemyAttack, 0.22f);
            GameAudio.PlayGlobal(AudioCueIds.Level4LaserFire, 0.16f);
            OrientAttackEffect(hitPosition);

            currentRobotTarget.TakeDamage(appliedDamage, hitPosition);

            PlayAttackEffect();

            yield return new WaitForSeconds(attackInterval);
        }

        StopAttackRoutine();
        StopAttackEffect();
    }

    private void OnDeath()
    {
        if (isDying)
            return;

        Died?.Invoke(this);
        OnEnemyDied?.Invoke(this);
        StopAttackRoutine();
        StopAttackEffect();

        StartCoroutine(DeathFadeRoutine());
    }

    private void OnCollisionExit(Collision other)
    {
        if (currentRobotTarget == null)
            return;

        Health otherHealth = other.gameObject.GetComponentInParent<Health>();
        if (otherHealth == currentRobotTarget)
        {
            StopAttackRoutine();
            StopAttackEffect();
        }
    }

    private void OnDestroy()
    {
        StopAttackRoutine();

        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }

        StopAttackEffect();
    }

    private void Update()
    {
        if (attackEffect == null || !attackEffect.isPlaying)
            return;

        bool hasValidTarget = currentRobotTarget != null && currentRobotTarget.IsAlive;
        if (!hasValidTarget)
        {
            StopAttackRoutine();
            StopAttackEffect();
        }
    }

    private void StopAttackEffect()
    {
        if (attackEffect != null && attackEffect.isPlaying)
        {
            attackEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void TryResolveAttackEffect()
    {
        if (!autoResolveAttackEffect || attackEffect != null)
            return;

        ParticleSystem[] effects = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem effect in effects)
        {
            if (effect == null)
                continue;

            if (effect.gameObject.name.IndexOf("lightning", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                attackEffect = effect;
                return;
            }
        }
    }

    private void StopAttackRoutine()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (currentRobotTarget != null)
        {
            currentRobotTarget.OnDeath -= HandleCurrentTargetDeath;
        }

        currentRobotTarget = null;
    }

    private void HandleCurrentTargetDeath()
    {
        StopAttackRoutine();
        StopAttackEffect();
    }

    private void OrientAttackEffect(Vector3 hitPosition)
    {
        if (attackEffect == null)
            return;

        Vector3 direction = hitPosition - attackEffect.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        attackEffect.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void PlayAttackEffect()
    {
        if (attackEffect == null)
            return;

        if (!attackEffect.isPlaying)
        {
            attackEffect.Play(true);
        }
    }

    private IEnumerator DeathFadeRoutine()
    {
        isDying = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition - Vector3.up * deathSinkDistance;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 0.06f;
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

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
            transform.localScale = startScale;
            transform.position = startPosition;
            isDying = false;
        }
    }
}
