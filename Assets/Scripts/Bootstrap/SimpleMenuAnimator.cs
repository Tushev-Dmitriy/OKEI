using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SimpleMenuAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Graphic backgroundGraphic;
    [SerializeField] private TMP_Text label;
    [SerializeField] private bool accentStyle;
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float pressedScale = 0.98f;
    [SerializeField] private float duration = 0.12f;

    private Vector3 _baseScale = Vector3.one;
    private bool _isPointerInside;
    private Color _normalBackgroundColor;
    private Color _hoverBackgroundColor;
    private Color _pressedBackgroundColor;
    private Color _normalLabelColor;
    private Color _hoverLabelColor;
    private Color _pressedLabelColor;

    public void Configure(RectTransform targetRect, Graphic graphic, TMP_Text labelText, bool isAccent)
    {
        target = targetRect;
        backgroundGraphic = graphic;
        label = labelText;
        accentStyle = isAccent;

        ResolveReferences();
        CacheBaseScale();
        BuildPalette();
        ApplyState(_baseScale, _normalBackgroundColor, _normalLabelColor, true);
    }

    private void Awake()
    {
        ResolveReferences();
        CacheBaseScale();
        BuildPalette();
    }

    private void OnEnable()
    {
        ApplyState(_baseScale, _normalBackgroundColor, _normalLabelColor, true);
    }

    private void OnDisable()
    {
        KillTweens();
        ApplyState(_baseScale, _normalBackgroundColor, _normalLabelColor, true);
        _isPointerInside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        AnimateTo(_baseScale * hoverScale, _hoverBackgroundColor, _hoverLabelColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        AnimateTo(_baseScale, _normalBackgroundColor, _normalLabelColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateTo(_baseScale * pressedScale, _pressedBackgroundColor, _pressedLabelColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(
            _isPointerInside ? _baseScale * hoverScale : _baseScale,
            _isPointerInside ? _hoverBackgroundColor : _normalBackgroundColor,
            _isPointerInside ? _hoverLabelColor : _normalLabelColor);
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        if (backgroundGraphic == null)
        {
            backgroundGraphic = GetComponent<Graphic>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void CacheBaseScale()
    {
        if (target != null)
        {
            _baseScale = target.localScale == Vector3.zero ? Vector3.one : target.localScale;
        }
    }

    private void BuildPalette()
    {
        if (accentStyle)
        {
            _normalBackgroundColor = Color.white;
            _hoverBackgroundColor = new Color(0.99f, 0.97f, 0.94f, 1f);
            _pressedBackgroundColor = new Color(0.88f, 0.83f, 0.76f, 1f);
            _normalLabelColor = new Color(0.21f, 0.19f, 0.16f, 1f);
            _hoverLabelColor = new Color(0.14f, 0.13f, 0.11f, 1f);
            _pressedLabelColor = new Color(0.26f, 0.23f, 0.19f, 1f);
            return;
        }

        _normalBackgroundColor = Color.white;
        _hoverBackgroundColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        _pressedBackgroundColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        _normalLabelColor = new Color(0.96f, 0.94f, 0.90f, 1f);
        _hoverLabelColor = Color.white;
        _pressedLabelColor = new Color(0.84f, 0.81f, 0.76f, 1f);
    }

    private void AnimateTo(Vector3 scale, Color backgroundColor, Color labelColor)
    {
        ApplyState(scale, backgroundColor, labelColor, false);
    }

    private void ApplyState(Vector3 scale, Color backgroundColor, Color labelColor, bool immediate)
    {
        if (target == null)
        {
            return;
        }

        KillTweens();

        if (immediate)
        {
            target.localScale = scale;

            if (backgroundGraphic != null)
            {
                backgroundGraphic.color = backgroundColor;
            }

            if (label != null)
            {
                label.color = labelColor;
            }

            return;
        }

        target.DOScale(scale, duration).SetEase(Ease.OutQuad).SetUpdate(true);

        if (backgroundGraphic != null)
        {
            backgroundGraphic.DOColor(backgroundColor, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (label != null)
        {
            label.DOColor(labelColor, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    private void KillTweens()
    {
        if (target != null)
        {
            target.DOKill();
        }

        if (backgroundGraphic != null)
        {
            backgroundGraphic.DOKill();
        }

        if (label != null)
        {
            label.DOKill();
        }
    }
}
