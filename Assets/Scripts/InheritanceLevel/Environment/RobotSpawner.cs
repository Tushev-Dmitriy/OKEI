using System;
using System.Collections.Generic;
using UnityEngine;

public class RobotSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject robotPrefab;
    [SerializeField] private bool spawnAssaultRobot;

    [Header("Data")]
    [SerializeField] private RobotConfigSO basicConfig;
    [SerializeField] private RobotConfigSO assaultConfig;
    [SerializeField] private List<RobotConfigSO> robotConfigs = new List<RobotConfigSO>();

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool replaceExisting = false;
    [SerializeField] private RobotType defaultRobotType = RobotType.Base;
    [SerializeField, Min(0f)] private float spawnCooldownSeconds = 1.8f;

    private RobotType _selectedRobotType;
    private readonly List<GameObject> _instances = new List<GameObject>();
    private GameObject _currentInstance;
    private Robot _currentRobot;
    private float _nextAllowedSpawnTime;

    public event Func<RobotType, SpawnPermissionResult> OnSpawnRequested;
    public event Action<Robot> OnRobotSpawned;
    public event Action<RobotType, string> OnSpawnDenied;

    public Robot CurrentRobot => _currentRobot;
    public Transform SpawnPoint => spawnPoint;

    private void Start()
    {
        _selectedRobotType = spawnAssaultRobot ? RobotType.Attacker : defaultRobotType;

        if (spawnOnStart)
        {
            SpawnSelectedRobot();
        }
    }

    public void SpawnRobot()
    {
        SpawnRobot(_selectedRobotType);
    }

    public void SpawnSelectedRobot()
    {
        SpawnRobot(_selectedRobotType);
    }

    public Robot SpawnRobotOfType(
        RobotType robotType,
        Vector3 position,
        Quaternion rotation,
        bool registerAsCurrent = true,
        bool bypassValidation = false)
    {
        return SpawnRobot(robotType, position, rotation, registerAsCurrent, bypassValidation);
    }

    public void SetSelectedRobotType(RobotType robotType, bool spawnNow = true)
    {
        if (robotType == RobotType.None)
            return;

        _selectedRobotType = robotType;

        if (spawnNow)
        {
            SpawnSelectedRobot();
        }
    }

    public RobotType GetSelectedRobotType()
    {
        return _selectedRobotType;
    }

    public RobotConfigSO GetConfigForType(RobotType robotType)
    {
        return ResolveConfig(robotType);
    }

    public bool CanSpawnType(RobotType robotType, out string denyReason)
    {
        return CanSpawn(robotType, out denyReason);
    }

    public void NotifySpawnDenied(RobotType robotType, string denyReason)
    {
        OnSpawnDenied?.Invoke(robotType, denyReason);
    }

    public void ClearCurrentRobot(Robot robot = null)
    {
        if (robot != null && _currentRobot != robot)
            return;

        if (_currentRobot != null)
        {
            _currentRobot.Died -= HandleCurrentRobotDied;
        }

        _currentRobot = null;
        _currentInstance = null;
    }

    private Robot SpawnRobot(RobotType robotType)
    {
        if (spawnPoint == null)
            return null;

        return SpawnRobot(robotType, spawnPoint.position, spawnPoint.rotation, true, false);
    }

    private Robot SpawnRobot(
        RobotType robotType,
        Vector3 position,
        Quaternion rotation,
        bool registerAsCurrent,
        bool bypassValidation)
    {
        if (robotPrefab == null)
            return null;

        if (!bypassValidation && !IsSpawnCooldownReady(out string cooldownReason))
        {
            NotifySpawnDenied(robotType, cooldownReason);
            return null;
        }

        if (!bypassValidation && !CanSpawn(robotType, out string denyReason))
        {
            NotifySpawnDenied(robotType, denyReason);
            return null;
        }

        if (registerAsCurrent && replaceExisting && _currentInstance != null)
        {
            ClearCurrentRobot();
            Destroy(_currentInstance);
            _instances.Clear();
        }

        GameObject instance = Instantiate(robotPrefab, position, rotation);
        _instances.Add(instance);

        Robot robotLogic = AttachRobotLogic(instance, robotType);
        RobotConfigSO configToUse = ResolveConfig(robotType);

        if (robotLogic != null && configToUse != null)
        {
            robotLogic.Initialize(configToUse);
        }

        if (registerAsCurrent)
        {
            SetCurrentRobot(robotLogic, instance);
        }

        OnRobotSpawned?.Invoke(robotLogic);
        _nextAllowedSpawnTime = Time.unscaledTime + Mathf.Max(0f, spawnCooldownSeconds);
        return robotLogic;
    }

    private Robot AttachRobotLogic(GameObject instance, RobotType robotType)
    {
        if (instance == null)
            return null;

        Robot robotLogic = GetOrCreateRobotLogic(instance, robotType);

        foreach (Robot robotComponent in instance.GetComponents<Robot>())
        {
            if (robotComponent == null)
                continue;

            robotComponent.enabled = robotComponent == robotLogic;
        }

        return robotLogic;
    }

    private Robot GetOrCreateRobotLogic(GameObject instance, RobotType robotType)
    {
        return robotType switch
        {
            RobotType.Attacker => GetOrAdd<AssaultRobot>(instance),
            RobotType.Healer => GetOrAdd<HealerRobot>(instance),
            RobotType.Defender => GetOrAdd<DefenderRobot>(instance),
            _ => GetOrAdd<Robot>(instance)
        };
    }

    private static T GetOrAdd<T>(GameObject instance) where T : Robot
    {
        T component = instance.GetComponent<T>();
        if (component == null)
        {
            component = instance.AddComponent<T>();
        }

        return component;
    }

    private bool CanSpawn(RobotType robotType, out string denyReason)
    {
        if (!IsSpawnCooldownReady(out denyReason))
        {
            return false;
        }

        if (OnSpawnRequested != null)
        {
            foreach (Func<RobotType, SpawnPermissionResult> handler in OnSpawnRequested.GetInvocationList())
            {
                SpawnPermissionResult result = handler.Invoke(robotType);
                if (!result.Allowed)
                {
                    denyReason = result.Reason;
                    return false;
                }
            }
        }

        denyReason = string.Empty;
        return true;
    }

    private bool IsSpawnCooldownReady(out string denyReason)
    {
        float remaining = _nextAllowedSpawnTime - Time.unscaledTime;
        if (remaining > 0f)
        {
            denyReason = $"Spawn cooldown: wait {remaining:F1}s before launching the next robot.";
            return false;
        }

        denyReason = string.Empty;
        return true;
    }

    private void SetCurrentRobot(Robot robot, GameObject instance)
    {
        if (_currentRobot != null)
        {
            _currentRobot.Died -= HandleCurrentRobotDied;
        }

        _currentRobot = robot;
        _currentInstance = instance;

        if (_currentRobot != null)
        {
            _currentRobot.Died += HandleCurrentRobotDied;
        }
    }

    private void HandleCurrentRobotDied(Robot robot)
    {
        if (_currentRobot != robot)
            return;

        ClearCurrentRobot(robot);
    }

    private RobotConfigSO ResolveConfig(RobotType robotType)
    {
        if (robotConfigs != null && robotConfigs.Count > 0)
        {
            RobotConfigSO config = robotConfigs.Find(x => x != null && x.robotType == robotType);
            if (config != null)
                return config;
        }

        RobotUnlockManager manager = FindFirstObjectByType<RobotUnlockManager>();
        if (manager != null)
        {
            RobotConfigSO config = manager.GetRobotConfig(robotType);
            if (config != null)
                return config;
        }

        if (robotType == RobotType.Attacker && assaultConfig != null)
            return assaultConfig;

        return basicConfig;
    }

    private void OnDestroy()
    {
        ClearCurrentRobot();
    }
}

public readonly struct SpawnPermissionResult
{
    public SpawnPermissionResult(bool allowed, string reason)
    {
        Allowed = allowed;
        Reason = reason ?? string.Empty;
    }

    public bool Allowed { get; }
    public string Reason { get; }

    public static SpawnPermissionResult Allow()
    {
        return new SpawnPermissionResult(true, string.Empty);
    }

    public static SpawnPermissionResult Deny(string reason)
    {
        return new SpawnPermissionResult(false, reason);
    }
}
