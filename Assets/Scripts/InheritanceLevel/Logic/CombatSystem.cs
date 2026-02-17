using System.Collections;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float damagePerHit = 10f;
    [SerializeField] private float attackInterval = 0.5f;

    [Header("Attack Effect (Optional)")]
    [SerializeField] private ParticleSystem attackEffect;

    private Health targetHealth;
    private bool isInCombat;
    private Coroutine combatCoroutine;

    public float DamagePerHit => damagePerHit;
    public float AttackInterval => attackInterval;
    public bool IsInCombat => isInCombat;

    public void InitializeCombat(float damage, float interval)
    {
        damagePerHit = damage;
        attackInterval = interval;
    }

    public void StartCombat(Health target)
    {
        if (target == null || !target.IsAlive)
        {
            return;
        }

        if (isInCombat)
        {
            return;
        }

        targetHealth = target;
        isInCombat = true;

        combatCoroutine = StartCoroutine(CombatRoutine());
    }

    public void StopCombat()
    {
        if (combatCoroutine != null)
        {
            StopCoroutine(combatCoroutine);
            combatCoroutine = null;
        }

        isInCombat = false;
        targetHealth = null;

        StopAttackEffect();
    }

    private IEnumerator CombatRoutine()
    {
        while (isInCombat && targetHealth != null && targetHealth.IsAlive)
        {
            PerformAttack();

            yield return new WaitForSeconds(attackInterval);

            if (targetHealth == null || !targetHealth.IsAlive)
            {
                OnTargetDefeated();
                break;
            }
        }

        StopCombat();
    }

    protected virtual void PerformAttack()
    {
        if (targetHealth != null && targetHealth.IsAlive)
        {
            if (attackEffect != null)
            {
                attackEffect.Play();
            }

            Vector3 hitPosition = targetHealth.transform.position;
            Collider targetCollider = targetHealth.GetComponent<Collider>();
            if (targetCollider != null)
            {
                hitPosition = targetCollider.ClosestPoint(transform.position);
            }

            targetHealth.TakeDamage(damagePerHit, hitPosition);
        }
    }

    protected virtual void OnTargetDefeated()
    {
        StopAttackEffect();
    }

    private void StopAttackEffect()
    {
        if (attackEffect != null && attackEffect.isPlaying)
        {
            attackEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDisable()
    {
        StopCombat();
    }

    private void OnDestroy()
    {
        StopCombat();
    }
}
