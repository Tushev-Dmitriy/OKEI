using DG.Tweening;
using TMPro;
using UnityEngine;

public class TooltipUI : MonoBehaviour
{
    [SerializeField] private TextMeshPro _textMesh;
    [SerializeField] private float _fadeDuration = 0.4f;
    [SerializeField] private float _scaleDuration = 2f;

    private void Awake()
    {
        _textMesh.alpha = 0;    
        transform.localScale = Vector3.zero;
    }

    public void Show()
    {
        _textMesh.alpha = 0;
        transform.localScale = Vector3.zero;

        _textMesh.DOFade(1, _fadeDuration).SetId(this);
        transform.DOScale(Vector3.one, _scaleDuration).SetEase(Ease.OutBack).SetId(this);
    }

    public void Hide()
    {
        if (gameObject.GetComponent<RectTransform>() == null) return;

        _textMesh.DOFade(0, _fadeDuration);
        transform.DOScale(Vector3.zero, _scaleDuration).SetEase(Ease.InBack);
    }


    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}
