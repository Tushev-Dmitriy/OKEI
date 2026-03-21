using StarterAssets;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class PlayerDefaultsPortal : MonoBehaviour
{
    private struct PortalPassState
    {
        public float EntrySide;
        public bool HasApplied;
    }

    private readonly Dictionary<ThirdPersonController, PortalPassState> _trackedPlayers = new();

    private void Awake()
    {
        Collider portalCollider = GetComponent<Collider>();
        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
        }
    }

    private void Reset()
    {
        Collider portalCollider = GetComponent<Collider>();
        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null || _trackedPlayers.ContainsKey(player))
        {
            return;
        }

        float side = Mathf.Sign(GetPortalSide(player.transform.position));
        if (Mathf.Approximately(side, 0f))
        {
            side = 1f;
        }

        _trackedPlayers[player] = new PortalPassState
        {
            EntrySide = side,
            HasApplied = false
        };
    }

    private void OnTriggerStay(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null || !_trackedPlayers.TryGetValue(player, out PortalPassState state) || state.HasApplied)
        {
            return;
        }

        float currentSide = Mathf.Sign(GetPortalSide(player.transform.position));
        if (Mathf.Approximately(currentSide, 0f) || Mathf.Approximately(currentSide, state.EntrySide))
        {
            return;
        }

        player.RestoreDefaultParameters();
        state.HasApplied = true;
        _trackedPlayers[player] = state;
    }

    private void OnTriggerExit(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null)
        {
            return;
        }

        _trackedPlayers.Remove(player);
    }

    private float GetPortalSide(Vector3 worldPosition)
    {
        return Vector3.Dot(worldPosition - transform.position, transform.forward);
    }
}
