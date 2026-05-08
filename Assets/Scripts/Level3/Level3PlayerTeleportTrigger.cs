using System;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class Level3PlayerTeleportTrigger : MonoBehaviour
{
    [SerializeField] private Transform targetPoint;
    [SerializeField] private string fallbackTargetName = "BeforeHolePos";

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        EnsureTriggerCollider();
        ResolveTargetPoint();
    }

    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null)
        {
            return;
        }

        if (!ResolveTargetPoint())
        {
            Debug.LogWarning($"[{nameof(Level3PlayerTeleportTrigger)}] Target point is not assigned on '{name}'.");
            return;
        }

        TeleportPlayer(player, targetPoint);
    }

    private bool ResolveTargetPoint()
    {
        if (targetPoint != null)
        {
            return true;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.gameObject.scene != activeScene)
            {
                continue;
            }

            if (string.Equals(candidate.name, fallbackTargetName, StringComparison.Ordinal))
            {
                targetPoint = candidate;
                return true;
            }
        }

        return false;
    }

    private void EnsureTriggerCollider()
    {
        if (TryGetComponent(out Collider triggerCollider))
        {
            triggerCollider.isTrigger = true;
        }
    }

    private static void TeleportPlayer(ThirdPersonController player, Transform destination)
    {
        if (player == null || destination == null)
        {
            return;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = player.GetComponentInChildren<CharacterController>();
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        player.transform.SetPositionAndRotation(destination.position, destination.rotation);

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}
