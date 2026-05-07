using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private ParticleSystem damageEffect;
    [SerializeField] private bool autoResolveDamageEffect = true;
    [SerializeField, Min(0.02f)] private float damageEffectHoldAfterHit = 0.28f;

    [Header("Floating Text (Optional)")]
    [SerializeField] private FloatingTextSpawner floatingTextSpawner;

    private float currentHealth;
    private bool isEnemyUnit;
    private float damageEffectStopAtTime;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        TryResolveDamageEffect();

        if (floatingTextSpawner == null)
        {
            floatingTextSpawner = FindFirstObjectByType<FloatingTextSpawner>();
        }

        isEnemyUnit = GetComponent<EnemyUnit>() != null;
    }

    public void Initialize(float health)
    {
        maxHealth = health;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector3 hitPosition)
    {
        if (!IsAlive)
            return;

        float incomingDamage = Mathf.Max(0f, damage);
        if (incomingDamage <= 0f)
            return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - incomingDamage);
        OnHealthChanged?.Invoke(currentHealth);

        float appliedDamage = oldHealth - currentHealth;
        int shownDamage = Mathf.RoundToInt(appliedDamage);
        if (shownDamage > 0 && floatingTextSpawner != null)
        {
            Vector3 displayPosition = ResolveFloatingTextPosition(hitPosition);
            floatingTextSpawner.ShowDamage(shownDamage, displayPosition, isEnemyUnit);
        }

        if (appliedDamage > 0f)
        {
            PlayDamageEffect();
            GameAudio.PlayAtPoint(isEnemyUnit ? AudioCueIds.Level4EnemyHit : AudioCueIds.Level4RobotHit, hitPosition, 0.9f, 1f, 16f);
            GameAudio.PlayAtPoint(AudioCueIds.Level4LaserHitMetal, hitPosition, 0.7f, 1f, 18f);
            GameAudio.PlayAtPoint(AudioCueIds.Level4SparkImpact, hitPosition, 0.65f, 1f, 18f);
        }

        if (currentHealth <= 0)
        {
            GameAudio.PlayAtPoint(isEnemyUnit ? AudioCueIds.Level4EnemyDeathClean : AudioCueIds.Level4RobotDeathClean, transform.position, 1f, 1f, 20f);
            GameAudio.PlayAtPoint(isEnemyUnit ? AudioCueIds.Level4ExplosionSmallEnemy : AudioCueIds.Level4ExplosionSmallRobot, transform.position, 0.9f, 1f, 20f);
            GameAudio.PlayGlobal(isEnemyUnit ? AudioCueIds.Level4EnemyDeathClean : AudioCueIds.Level4RobotDeathClean, 0.9f);
            GameAudio.PlayGlobal(isEnemyUnit ? AudioCueIds.Level4ExplosionSmallEnemy : AudioCueIds.Level4ExplosionSmallRobot, 0.75f);
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        Heal(amount, transform.position);
    }

    public void Heal(float amount, Vector3 healPosition)
    {
        if (!IsAlive)
            return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);

        float appliedHeal = currentHealth - oldHealth;
        int shownHeal = Mathf.RoundToInt(appliedHeal);
        if (shownHeal > 0 && floatingTextSpawner != null)
        {
            Vector3 displayPosition = ResolveFloatingTextPosition(healPosition);
            floatingTextSpawner.ShowHeal(shownHeal, displayPosition);
        }
    }

    private void TryResolveDamageEffect()
    {
        if (!autoResolveDamageEffect || damageEffect != null)
            return;

        ParticleSystem[] effects = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem effect in effects)
        {
            if (effect == null)
                continue;

            string effectName = effect.gameObject.name;
            if (effectName.IndexOf("spark", StringComparison.OrdinalIgnoreCase) >= 0 ||
                effectName.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                effectName.IndexOf("hit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                effectName.IndexOf("impact", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                damageEffect = effect;
                return;
            }
        }

        // Fallback so damage VFX still works even when prefab naming is inconsistent.
        if (effects.Length > 0)
        {
            damageEffect = effects[0];
        }
    }

    private Vector3 ResolveFloatingTextPosition(Vector3 hitPosition)
    {
        Vector3 basePosition = transform.position;

        if (TryGetComponent(out Collider col))
        {
            basePosition = col.bounds.center + Vector3.up * (col.bounds.extents.y * 1.15f);
        }

        if (!IsFinite(hitPosition))
            return basePosition;

        if ((hitPosition - basePosition).sqrMagnitude > 16f)
            return basePosition;

        return Vector3.Lerp(basePosition, hitPosition, 0.35f) + Vector3.up * 0.45f;
    }

    private void PlayDamageEffect()
    {
        if (damageEffect == null)
            return;

        if (!damageEffect.isPlaying)
        {
            damageEffect.Play(true);
        }

        damageEffectStopAtTime = Time.time + Mathf.Max(0.02f, damageEffectHoldAfterHit);
    }

    private void StopDamageEffectNow()
    {
        damageEffectStopAtTime = 0f;

        if (damageEffect != null)
        {
            damageEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void OnDisable()
    {
        StopDamageEffectNow();
    }

    private void Update()
    {
        if (damageEffect == null || !damageEffect.isPlaying || damageEffectStopAtTime <= 0f)
            return;

        if (Time.time >= damageEffectStopAtTime)
        {
            damageEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            damageEffectStopAtTime = 0f;
        }
    }

    private void OnDestroy()
    {
        StopDamageEffectNow();
    }
}
