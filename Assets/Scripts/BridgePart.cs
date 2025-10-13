using UnityEngine;
using DG.Tweening;

public class BridgePart : MonoBehaviour
{
    [Header("Transform References")]
    [SerializeField] private Transform bridgePart;

    [Header("Positions and Rotations")]
    [SerializeField] private Vector3 posStart = new Vector3(0.2647f, 0.103f, 0f);
    [SerializeField] private Vector3 rotStart = new Vector3(0f, 180f, 0f);

    [SerializeField] private Vector3 posEnd = new Vector3(0.275f, 0.134f, 0f);
    [SerializeField] private Vector3 rotEnd = new Vector3(0f, 180f, 40f);

    [Header("Animation Settings")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private Ease easeType = Ease.InOutSine;

    private bool isRaised = false;
    private Tweener moveTween;
    private Tweener rotateTween;

    public void RaisePart()
    {
        if (isRaised) return;
        isRaised = true;

        moveTween?.Kill();
        rotateTween?.Kill();

        moveTween = bridgePart.DOLocalMove(posEnd, duration).SetEase(easeType);
        rotateTween = bridgePart.DOLocalRotate(rotEnd, duration).SetEase(easeType);
    }

    public void LowerPart()
    {
        if (!isRaised) return;
        isRaised = false;

        moveTween?.Kill();
        rotateTween?.Kill();

        moveTween = bridgePart.DOLocalMove(posStart, duration).SetEase(easeType);
        rotateTween = bridgePart.DOLocalRotate(rotStart, duration).SetEase(easeType);
    }

    public void TogglePart()
    {
        if (isRaised)
        {
            LowerPart();
        }
        else 
        {
            RaisePart();
        }
    }
}
