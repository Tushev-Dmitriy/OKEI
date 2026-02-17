using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingTextView : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private Sequence sequence;
    private Action<FloatingTextView> onReturnToPool;

    public void Initialize(Action<FloatingTextView> returnToPool)
    {
        onReturnToPool = returnToPool;
    }

    public void Setup(int value, Color color)
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
            if (text == null)
            {
                Debug.LogError("FloatingTextView: Missing TMP_Text reference.", this);
                ReturnToPool();
                return;
            }
        }

        text.text = value.ToString();
        text.color = color;
        text.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    public void PlayAnimation(float duration, float distanceUp, float popScale, Ease moveEase)
    {
        if (text == null)
        {
            ReturnToPool();
            return;
        }

        KillTweens();

        float startY = transform.localPosition.y;

        sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveY(startY + distanceUp, duration).SetEase(moveEase));
        sequence.Insert(duration * 0.5f, text.DOFade(0f, duration * 0.5f));

        if (popScale > 1f)
        {
            sequence.Insert(0f, transform.DOScale(popScale, duration * 0.15f).SetEase(Ease.OutBack));
            sequence.Insert(duration * 0.15f, transform.DOScale(1f, duration * 0.1f).SetEase(Ease.OutQuad));
        }

        sequence.OnComplete(ReturnToPool);
    }

    private void ReturnToPool()
    {
        onReturnToPool?.Invoke(this);
    }

    private void KillTweens()
    {
        if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
