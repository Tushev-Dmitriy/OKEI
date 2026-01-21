using System.Collections;
using UnityEngine;
using Zenject;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float damagePerHit = 10f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private float combatRange = 2f;

    [Header("Attack Effect (Optional)")]
    [SerializeField] private ParticleSystem attackEffect;

    private Health targetHealth;
    private bool isInCombat = false;
    private Coroutine combatCoroutine;
    private RobotUnlockEvents _robotUnlockEvents;

    public float DamagePerHit => damagePerHit;
    public float AttackInterval => attackInterval;
    public bool IsInCombat => isInCombat;

    [Inject]
    public void Construct(RobotUnlockEvents robotUnlockEvents)
    {
        _robotUnlockEvents = robotUnlockEvents;
    }

    public void InitializeCombat(float damage, float interval)
    {
        damagePerHit = damage;
        attackInterval = interval;
    }

    public void StartCombat(Health target)
    {
        if (target == null || !target.IsAlive)
        {
            Debug.LogWarning($"{gameObject.name}: Невозможно начать бой - цель недоступна");
            return;
        }

        if (isInCombat)
        {
            Debug.Log($"{gameObject.name} уже в бою");
            return;
        }

        targetHealth = target;
        isInCombat = true;

        Debug.Log($"{gameObject.name} начинает бой с {target.gameObject.name}");

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

        Debug.Log($"{gameObject.name} прекратил бой");

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
            Debug.Log($"{gameObject.name} атакует {targetHealth.gameObject.name} на {damagePerHit} урона");

            if (attackEffect != null)
            {
                attackEffect.Play();
            }

            targetHealth.TakeDamage(damagePerHit);
        }
    }

    protected virtual void OnTargetDefeated()
    {
        Debug.Log($"{gameObject.name} победил в бою!");
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
