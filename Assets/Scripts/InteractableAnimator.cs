using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class InteractableAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Transform animatedPart;
    [SerializeField] private Vector3 activeRotation;
    [SerializeField] private Vector3 activePosition;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private bool toggleMode = true;

    [Header("Bridge")]
    [SerializeField] private Bridge bridge;
    [SerializeField] private AllBridgeController bridgeController;

    [Header("Progress")]
    [SerializeField] private float percentStep = 10f;

    [Header("Text")]
    [SerializeField] private List<InteractableTextConroller> textControllers = new();

    private Vector3 startRotation;
    private Vector3 startPosition;
    private float currentPercent;
    private bool isActive;

    private void Awake()
    {
        if (!animatedPart)
            animatedPart = transform;

        startRotation = animatedPart.localEulerAngles;
        startPosition = animatedPart.localPosition;
    }

    private void OnEnable()
    {
        if (bridgeController != null)
            bridgeController.OnBridgePercentChanged += OnBridgePercentChanged;
    }

    private void OnDisable()
    {
        if (bridgeController != null)
            bridgeController.OnBridgePercentChanged -= OnBridgePercentChanged;
    }

    public void Interact()
    {
        UpdateTexts();

        if (toggleMode)
        {
            Toggle();
        }
        else
        {
            StepProgress();
        }
    }

    private void Toggle()
    {
        isActive = !isActive;
        currentPercent = isActive ? 100f : 0f;

        bridge.ChangePercent((int)currentPercent);
        bridge.ApplyInstant();

        AnimatePart(isActive);
    }

    private void StepProgress()
    {
        currentPercent = Mathf.Clamp(currentPercent + percentStep, 0f, 100f);

        bridge.ChangePercent((int)currentPercent);
        bridge.ApplyInstant();

        DOTween.Sequence()
            .Append(AnimatePart(true))
            .AppendInterval(duration + 0.05f)
            .Append(AnimatePart(false));
    }

    private Tween AnimatePart(bool activate)
    {
        Vector3 targetRot = activate ? activeRotation : startRotation;
        Vector3 targetPos = activate ? activePosition : startPosition;

        return DOTween.Sequence()
            .Join(animatedPart.DOLocalRotate(targetRot, duration).SetEase(ease))
            .Join(animatedPart.DOLocalMove(targetPos, duration).SetEase(ease));
    }

    private void UpdateTexts()
    {
        foreach (var tc in textControllers)
        {
            if (tc.useCounter)
                tc.IncrementCounter();
            else
                tc.SwapText();
        }
    }

    private void OnBridgePercentChanged(float percent)
    {
        if (!toggleMode && percent >= 100f)
        {
            currentPercent = 0f;

            foreach (var tc in textControllers)
            {
                if (tc.useCounter)
                    tc.ResetCounter();
            }
        }
    }
}
