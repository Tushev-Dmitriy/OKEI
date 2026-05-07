using System.Collections;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float damagePerHit = 10f;
    [SerializeField] private float attackInterval = 0.5f;

    [Header("Attack Effect (Optional)")]
    [SerializeField] private ParticleSystem attackEffect;
    [SerializeField] private bool autoResolveAttackEffect = true;

    private Health targetHealth;
    private bool isInCombat;
    private Coroutine combatCoroutine;

    public float DamagePerHit => damagePerHit;
    public float AttackInterval => attackInterval;
    public bool IsInCombat => isInCombat;

    private void Awake()
    {
        TryResolveAttackEffect();
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
            return;
        }

        if (isInCombat)
        {
            return;
        }

        if (targetHealth != null)
        {
            targetHealth.OnDeath -= HandleTargetDeath;
        }

        targetHealth = target;
        targetHealth.OnDeath += HandleTargetDeath;
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
        if (targetHealth != null)
        {
            targetHealth.OnDeath -= HandleTargetDeath;
            targetHealth = null;
        }

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
            PlayAttackSound();
            PlayAttackEffect();

            Vector3 hitPosition = targetHealth.transform.position;
            Collider targetCollider = targetHealth.GetComponent<Collider>();
            if (targetCollider != null)
            {
                hitPosition = targetCollider.ClosestPoint(transform.position);
            }

            OrientAttackEffect(hitPosition);

            float finalDamage = Mathf.Max(0f, damagePerHit);
            Robot targetRobot = targetHealth.GetComponent<Robot>();
            if (targetRobot != null)
            {
                finalDamage = targetRobot.ModifyIncomingDamage(finalDamage);
            }

            targetHealth.TakeDamage(finalDamage, hitPosition);
        }
    }

    private void PlayAttackSound()
    {
        Robot ownerRobot = GetComponent<Robot>();
        string cueId = ownerRobot != null
            ? ownerRobot.RobotType switch
            {
                RobotType.Attacker => AudioCueIds.Level4RobotAttackAttacker,
                RobotType.Healer => AudioCueIds.Level4RobotAttackHealer,
                RobotType.Defender => AudioCueIds.Level4RobotAttackDefender,
                _ => AudioCueIds.Level4RobotAttackBase
            }
            : AudioCueIds.Level4LaserFire;

        GameAudio.PlayAtPoint(cueId, transform.position, 0.9f, 1f, 20f);
        GameAudio.PlayAtPoint(AudioCueIds.Level4LaserFire, transform.position, 0.7f, 1f, 22f);
        GameAudio.PlayGlobal(cueId, 0.22f);
        GameAudio.PlayGlobal(AudioCueIds.Level4LaserFire, 0.18f);
    }

    protected virtual void OnTargetDefeated()
    {
        StopAttackEffect();
    }

    private void HandleTargetDeath()
    {
        StopCombat();
    }

    private void StopAttackEffect()
    {
        if (attackEffect != null && attackEffect.isPlaying)
        {
            attackEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
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

    private void OnDisable()
    {
        StopCombat();
    }

    private void OnDestroy()
    {
        StopCombat();
    }
}
