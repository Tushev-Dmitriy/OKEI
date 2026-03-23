using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class FinalPortal : MonoBehaviour
{
    [SerializeField] private int targetSceneBuildIndex = 0;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float delayBeforeLoad = 0.1f;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private Color fadeColor = Color.black;

    private bool _isTransitionRunning;

    private void Awake()
    {
        if (TryGetComponent(out Collider portalCollider))
        {
            portalCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTransitionRunning)
        {
            return;
        }

        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null)
        {
            return;
        }

        StartCoroutine(TransitionToScene());
    }

    private IEnumerator TransitionToScene()
    {
        _isTransitionRunning = true;
        DontDestroyOnLoad(gameObject);

        if (triggerOnce && TryGetComponent(out Collider portalCollider))
        {
            portalCollider.enabled = false;
        }

        ScreenFadeOverlay overlay = ScreenFadeOverlay.GetOrCreate(fadeColor);
        yield return overlay.Fade(0f, 1f, fadeDuration);

        if (delayBeforeLoad > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLoad);
        }

        yield return overlay.LoadSceneWithFadeOut(targetSceneBuildIndex, fadeDuration);

        if (triggerOnce)
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    private sealed class ScreenFadeOverlay : MonoBehaviour
    {
        private const string OverlayName = "RuntimeScreenFadeOverlay";

        private Canvas _canvas;
        private Image _image;

        public static ScreenFadeOverlay GetOrCreate(Color color)
        {
            ScreenFadeOverlay existing = FindFirstObjectByType<ScreenFadeOverlay>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.SetColor(color);
                existing.SetAlpha(0f);
                return existing;
            }

            GameObject root = new GameObject(OverlayName);
            DontDestroyOnLoad(root);

            ScreenFadeOverlay overlay = root.AddComponent<ScreenFadeOverlay>();
            overlay.Initialize(color);
            return overlay;
        }

        public IEnumerator Fade(float from, float to, float duration)
        {
            if (_image == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            SetAlpha(from);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetAlpha(to);
        }

        public IEnumerator LoadSceneWithFadeOut(int buildIndex, float fadeOutDuration)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            if (loadOperation != null)
            {
                while (!loadOperation.isDone)
                {
                    yield return null;
                }
            }

            // Wait one frame so the new scene can initialize its UI/cameras before we fade back out.
            yield return null;
            yield return Fade(1f, 0f, fadeOutDuration);
            DestroySelf();
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        private void Initialize(Color color)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;

            gameObject.AddComponent<GraphicRaycaster>();

            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(transform, false);

            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _image = imageObject.AddComponent<Image>();
            _image.raycastTarget = false;

            SetColor(color);
            SetAlpha(0f);
        }

        private void SetColor(Color color)
        {
            if (_image == null)
            {
                return;
            }

            Color nextColor = color;
            nextColor.a = _image.color.a;
            _image.color = nextColor;
        }

        private void SetAlpha(float alpha)
        {
            if (_image == null)
            {
                return;
            }

            Color color = _image.color;
            color.a = Mathf.Clamp01(alpha);
            _image.color = color;
        }
    }
}
