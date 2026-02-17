using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private ParticleSystem damageEffect;
    [Header("Floating Text (Optional)")]
    [SerializeField] private FloatingTextSpawner floatingTextSpawner;
    private float currentHealth;
    private bool isEnemyUnit;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (floatingTextSpawner == null)
        {
            floatingTextSpawner = FindObjectOfType<FloatingTextSpawner>();
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
        if (!IsAlive) return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);

        float appliedDamage = oldHealth - currentHealth;
        int shownDamage = Mathf.RoundToInt(appliedDamage);
        if (shownDamage > 0 && floatingTextSpawner != null)
        {
            floatingTextSpawner.ShowDamage(shownDamage, hitPosition, isEnemyUnit);
        }

        if (damageEffect != null)
        {
            damageEffect.Play();
        }

        if (currentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        Heal(amount, transform.position);
    }

    public void Heal(float amount, Vector3 healPosition)
    {
        if (!IsAlive) return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);

        float appliedHeal = currentHealth - oldHealth;
        int shownHeal = Mathf.RoundToInt(appliedHeal);
        if (shownHeal > 0 && floatingTextSpawner != null)
        {
            floatingTextSpawner.ShowHeal(shownHeal, healPosition);
        }
    }
}

