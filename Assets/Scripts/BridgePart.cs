using UnityEngine;
using DG.Tweening;

public class BridgePart : MonoBehaviour
{
    [Header("Transform References")]
    [SerializeField] private Transform bridgePart;

    [Header("Positions and Rotations")]
    [SerializeField] private Vector3 posStart;
    [SerializeField] private Vector3 rotStart;
    [SerializeField] private Vector3 posEnd;
    [SerializeField] private Vector3 rotEnd;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private Ease easeType = Ease.InOutSine;

    private Tweener moveTween;
    private Tweener rotateTween;
    private float currentProgress = 0f;

    public void RaisePart() => MoveToProgress(1f);
    public void LowerPart() => MoveToProgress(0f);

    public void MoveToProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        currentProgress = progress;

        moveTween?.Kill();
        rotateTween?.Kill();

        Vector3 targetPos = Vector3.Lerp(posStart, posEnd, progress);
        Vector3 targetRot = Vector3.Lerp(rotStart, rotEnd, progress);

        moveTween = bridgePart.DOLocalMove(targetPos, duration).SetEase(easeType);
        rotateTween = bridgePart.DOLocalRotate(targetRot, duration).SetEase(easeType);
    }

    public void TogglePart()
    {
        if (currentProgress > 0.5f)
        {
            MoveToProgress(0f);
        }
        else
        {
            MoveToProgress(1f);
        }
    }
}
