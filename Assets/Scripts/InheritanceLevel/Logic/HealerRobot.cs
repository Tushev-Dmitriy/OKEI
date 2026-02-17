using UnityEngine;

public class HealerRobot : Robot
{
    private float _healTimer;
    private float _scanTimer;
    private Robot _currentTarget;
    [SerializeField] private float scanInterval = 0.5f;

    protected override void Update()
    {
        base.Update();

        if (!isAutonomous || config == null || !health.IsAlive)
            return;

        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f || _currentTarget == null || !IsValidTarget(_currentTarget))
        {
            _currentTarget = FindHealTarget();
            _scanTimer = scanInterval;
        }

        if (_currentTarget == null)
            return;

        FollowTarget(_currentTarget);
        TryHeal(_currentTarget);
    }

    public override void TryEngageCombat(EnemyUnit enemy)
    {
        // Healer does not engage in combat.
    }

    private Robot FindHealTarget()
    {
        var robots = FindObjectsByType<Robot>(FindObjectsSortMode.None);
        Robot best = null;
        float bestRatio = 1f;

        foreach (var robot in robots)
        {
            if (robot == null || robot == this)
                continue;

            var targetHealth = robot.GetComponent<Health>();
            if (targetHealth == null || !targetHealth.IsAlive)
                continue;

            float ratio = targetHealth.CurrentHealth / Mathf.Max(1f, targetHealth.MaxHealth);
            if (ratio < bestRatio)
            {
                bestRatio = ratio;
                best = robot;
            }
        }

        return best;
    }

    private bool IsValidTarget(Robot target)
    {
        if (target == null || target == this)
            return false;

        var targetHealth = target.GetComponent<Health>();
        return targetHealth != null && targetHealth.IsAlive;
    }

    private void FollowTarget(Robot target)
    {
        var targetTransform = target.transform;
        var distance = Vector3.Distance(transform.position, targetTransform.position);
        if (distance <= config.followDistance)
            return;

        var direction = (targetTransform.position - transform.position).normalized;
        var move = direction * config.moveSpeed * Time.deltaTime;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.MovePosition(rb.position + move);
        }
        else
        {
            transform.position += move;
        }
    }

    private void TryHeal(Robot target)
    {
        _healTimer += Time.deltaTime;
        if (_healTimer < config.healInterval)
            return;

        var targetHealth = target.GetComponent<Health>();
        if (targetHealth == null || !targetHealth.IsAlive)
            return;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > config.healRange)
            return;

        Vector3 healPosition = target.transform.position;
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider != null)
        {
            healPosition = targetCollider.ClosestPoint(transform.position);
        }
        targetHealth.Heal(config.healAmount, healPosition);
        _healTimer = 0f;
    }
}
