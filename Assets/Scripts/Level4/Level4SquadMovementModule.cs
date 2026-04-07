using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4SquadMovementModule : MonoBehaviour
{
    private readonly List<Robot> _livingBuffer = new();

    public void UpdateFinalSquadSpacing(Level4FlowController flow, List<Robot> finalSquad, Transform spawnPoint)
    {
        if (flow == null || finalSquad == null || finalSquad.Count == 0)
            return;

        Vector3 forward = spawnPoint != null ? spawnPoint.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        _livingBuffer.Clear();
        for (int i = 0; i < finalSquad.Count; i++)
        {
            Robot robot = finalSquad[i];
            if (robot != null && robot.IsAlive)
                _livingBuffer.Add(robot);
        }

        if (_livingBuffer.Count == 0)
            return;

        _livingBuffer.Sort((a, b) =>
        {
            float aProj = Vector3.Dot(a.transform.position, forward);
            float bProj = Vector3.Dot(b.transform.position, forward);
            return bProj.CompareTo(aProj);
        });

        float baseMultiplier = Mathf.Max(0.1f, flow.PlayerRobotMoveSpeedMultiplierValue);
        float minSpacing = Mathf.Max(0.2f, flow.SquadMinMoveSpacing);
        float minFactor = Mathf.Clamp01(flow.SquadMinSpeedFactorWhenBlocked);

        _livingBuffer[0].SetMoveSpeedMultiplier(baseMultiplier);

        for (int i = 1; i < _livingBuffer.Count; i++)
        {
            Robot front = _livingBuffer[i - 1];
            Robot back = _livingBuffer[i];

            float gap = Vector3.Dot(front.transform.position - back.transform.position, forward);
            float normalized = gap <= 0f ? 0f : Mathf.Clamp01(gap / minSpacing);
            float factor = Mathf.Lerp(minFactor, 1f, normalized);
            back.SetMoveSpeedMultiplier(baseMultiplier * factor);
        }
    }

    public void StabilizeFinalSpawnedRobot(Level4FlowController flow, Robot robot)
    {
        if (flow == null || robot == null)
            return;

        Transform spawnPoint = flow.Spawner != null ? flow.Spawner.SpawnPoint : null;
        if (spawnPoint != null)
        {
            robot.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            if (robot.TryGetComponent(out Rigidbody spawnRb))
            {
                spawnRb.position = spawnPoint.position;
                spawnRb.rotation = spawnPoint.rotation;
                spawnRb.linearVelocity = Vector3.zero;
                spawnRb.angularVelocity = Vector3.zero;
            }

            return;
        }

        Transform tr = robot.transform;
        Vector3 origin = tr.position + Vector3.up * flow.SquadGroundProbeHeight;
        float maxDistance = flow.SquadGroundProbeHeight * 2f + 3f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, flow.SquadGroundMask, QueryTriggerInteraction.Ignore))
            return;

        float bottomOffset = ComputeBottomOffset(robot);
        Vector3 groundedPosition = tr.position;
        groundedPosition.y = hit.point.y + bottomOffset + flow.SquadGroundOffset;
        tr.position = groundedPosition;

        if (robot.TryGetComponent(out Rigidbody rb))
        {
            rb.position = groundedPosition;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private static float ComputeBottomOffset(Robot robot)
    {
        if (robot == null)
            return 0f;

        Collider[] colliders = robot.GetComponentsInChildren<Collider>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (!hasBounds)
            return 0f;

        return Mathf.Max(0f, robot.transform.position.y - bounds.min.y);
    }
}
