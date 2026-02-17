using DG.Tweening;
using UnityEngine;

public class FloatingTextSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatingTextView floatingTextPrefab;
    [SerializeField] private Transform canvasRoot;

    [Header("Pooling")]
    [SerializeField] private int prewarmCount = 30;

    [Header("Position")]
    [SerializeField] private float offsetY = 1.5f;
    [SerializeField] private float randomSpreadX = 0.3f;
    [SerializeField] private float randomSpreadZ = 0.3f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float distanceUp = 1f;
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Colors")]
    [SerializeField] private Color damageColor = new Color(1f, 0.23f, 0.19f);
    [SerializeField] private Color enemyDamageColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color healColor = new Color(0.2f, 0.78f, 0.35f);

    private SimpleObjectPool<FloatingTextView> pool;

    private void Awake()
    {
        if (floatingTextPrefab == null || canvasRoot == null)
        {
            Debug.LogError("FloatingTextSpawner: Missing prefab or canvasRoot.", this);
            enabled = false;
            return;
        }

        pool = new SimpleObjectPool<FloatingTextView>(floatingTextPrefab, canvasRoot, prewarmCount);
    }

    public void ShowDamage(int value, Vector3 worldPos)
    {
        ShowDamage(value, worldPos, false);
    }

    public void ShowHeal(int value, Vector3 worldPos)
    {
        Show(value, healColor, worldPos);
    }

    public void ShowDamage(int value, Vector3 worldPos, bool isEnemy)
    {
        Color color = isEnemy ? enemyDamageColor : damageColor;
        Show(value, color, worldPos);
    }

    private void Show(int value, Color color, Vector3 worldPos)
    {
        if (!enabled || pool == null || value <= 0)
        {
            return;
        }

        FloatingTextView view = pool.Get();
        view.transform.SetParent(canvasRoot, false);

        Vector3 randomOffset = new Vector3(
            Random.Range(-randomSpreadX, randomSpreadX),
            offsetY,
            Random.Range(-randomSpreadZ, randomSpreadZ));
        Vector3 localPosition = canvasRoot.InverseTransformPoint(worldPos + randomOffset);
        view.transform.localPosition = localPosition;

        view.Initialize(ReturnToPool);
        view.Setup(value, color);
        float localDistanceUp = Mathf.Abs(canvasRoot.InverseTransformVector(Vector3.up * distanceUp).y);
        view.PlayAnimation(duration, localDistanceUp, popScale, moveEase);
    }

    private void ReturnToPool(FloatingTextView view)
    {
        pool.Release(view);
    }
}
