using System;
using StarterAssets;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class FinalPortal : MonoBehaviour
{
    [SerializeField] private int targetSceneBuildIndex = 0;
    [SerializeField] private int completedLevelIndex = 1;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private bool triggerOnce = true;

    private bool _isTransitionRunning;
    private ThirdPersonController _currentPlayer;

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

        _currentPlayer = player;
        _isTransitionRunning = true;

        bool started = SceneTransitionService.StartPortalTransition(
            targetSceneBuildIndex,
            completedLevelIndex,
            fadeInDuration,
            fadeOutDuration,
            "Завершение уровня",
            "Возвращаемся в меню",
            "Сохраняем прогресс и открываем следующий уровень",
            false,
            HandleTransitionCommitted,
            HandleTransitionCancelled);

        if (!started)
        {
            _isTransitionRunning = false;
            _currentPlayer = null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isTransitionRunning)
        {
            return;
        }

        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null || player != _currentPlayer)
        {
            return;
        }

        SceneTransitionService.CancelPortalTransition();
    }

    private void HandleTransitionCommitted()
    {
        SaveResetter.ResetGameplayProgress();

        if (triggerOnce)
        {
            if (TryGetComponent(out Collider portalCollider))
            {
                portalCollider.enabled = false;
            }
        }
    }

    private void HandleTransitionCancelled()
    {
        _isTransitionRunning = false;
        _currentPlayer = null;
    }
}
