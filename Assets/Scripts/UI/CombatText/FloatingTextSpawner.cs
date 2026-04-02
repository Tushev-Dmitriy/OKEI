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
    [SerializeField] private float offsetY = 2.35f;
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
    private Canvas targetCanvas;
    private RectTransform canvasRect;
    private Camera worldCamera;

    private void Awake()
    {
        if (floatingTextPrefab == null || canvasRoot == null)
        {
            Debug.LogError("FloatingTextSpawner: Missing prefab or canvasRoot.", this);
            enabled = false;
            return;
        }

        targetCanvas = canvasRoot.GetComponentInParent<Canvas>();
        canvasRect = canvasRoot as RectTransform;
        worldCamera = ResolveWorldCamera();

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
        EnsureBillboard(view);

        Vector3 randomOffset = new Vector3(
            Random.Range(-randomSpreadX, randomSpreadX),
            offsetY,
            Random.Range(-randomSpreadZ, randomSpreadZ));

        Vector3 spawnWorldPosition = worldPos + randomOffset;
        SetViewPosition(view.transform, spawnWorldPosition);

        view.Initialize(ReturnToPool);
        view.Setup(value, color);
        float localDistanceUp = GetVerticalAnimationDistance();
        view.PlayAnimation(duration, localDistanceUp, popScale, moveEase);
    }

    private static void EnsureBillboard(FloatingTextView view)
    {
        if (view == null)
            return;

        FloatingTextBillboard legacyBillboard = view.GetComponent<FloatingTextBillboard>();
        if (legacyBillboard != null)
        {
            legacyBillboard.enabled = false;
            Destroy(legacyBillboard);
        }

        if (view.GetComponent<Billboard>() == null)
        {
            view.gameObject.AddComponent<Billboard>();
        }
    }

    private void SetViewPosition(Transform viewTransform, Vector3 spawnWorldPosition)
    {
        if (viewTransform == null)
            return;

        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.WorldSpace)
        {
            viewTransform.position = spawnWorldPosition;
            return;
        }

        Camera cameraForProjection = worldCamera != null ? worldCamera : Camera.main;
        if (cameraForProjection == null || canvasRect == null)
        {
            viewTransform.position = spawnWorldPosition;
            return;
        }

        Vector3 screenPoint = cameraForProjection.WorldToScreenPoint(spawnWorldPosition);
        if (screenPoint.z < 0f)
        {
            viewTransform.position = spawnWorldPosition;
            return;
        }

        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        RectTransform rectTransform = viewTransform as RectTransform;
        if (rectTransform == null)
        {
            viewTransform.position = spawnWorldPosition;
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 anchoredPos))
        {
            rectTransform.anchoredPosition = anchoredPos;
        }
    }

    private float GetVerticalAnimationDistance()
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.WorldSpace)
        {
            return distanceUp;
        }

        return distanceUp * 60f;
    }

    private static Camera ResolveWorldCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in cameras)
        {
            if (cam != null && cam.enabled)
                return cam;
        }

        return null;
    }

    private void ReturnToPool(FloatingTextView view)
    {
        pool.Release(view);
    }
}
