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
    [SerializeField] private bool replaceExisting = true;
    [SerializeField] private RobotType defaultRobotType = RobotType.Base;
    [SerializeField] private bool unlockAttackerOnBaseDeath = true;

    private RobotType _selectedRobotType;
    private GameObject _currentInstance;
    private Health _currentHealth;
    private RobotUnlockManager _unlockManager;
    private RobotType _currentRobotType;

    private void Start()
    {
        _unlockManager = FindFirstObjectByType<RobotUnlockManager>();
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

    private void SpawnRobot(RobotType robotType)
    {
        if (robotPrefab == null || spawnPoint == null)
            return;

        if (_currentHealth != null)
        {
            _currentHealth.OnDeath -= OnCurrentRobotDeath;
            _currentHealth = null;
        }

        if (replaceExisting && _currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }

        GameObject instance = Instantiate(robotPrefab, spawnPoint.position, spawnPoint.rotation);
        _currentInstance = instance;
        _currentRobotType = robotType;

        Robot robotLogic = null;
        RobotConfigSO configToUse = ResolveConfig(robotType);

        if (robotType == RobotType.Attacker)
        {
            robotLogic = instance.GetComponent<AssaultRobot>();
            if (robotLogic == null)
                robotLogic = instance.AddComponent<AssaultRobot>();
        }
        else
        {
            robotLogic = instance.GetComponent<Robot>();
            if (robotLogic == null)
                robotLogic = instance.AddComponent<Robot>();
        }

        if (robotLogic != null && configToUse != null)
        {
            robotLogic.Initialize(configToUse);
        }

        _currentHealth = instance.GetComponent<Health>();
        if (_currentHealth != null)
        {
            _currentHealth.OnDeath += OnCurrentRobotDeath;
        }
    }

    private RobotConfigSO ResolveConfig(RobotType robotType)
    {
        if (robotConfigs != null && robotConfigs.Count > 0)
        {
            var config = robotConfigs.Find(x => x != null && x.robotType == robotType);
            if (config != null)
                return config;
        }

        var manager = FindFirstObjectByType<RobotUnlockManager>();
        if (manager != null)
        {
            var config = manager.GetRobotConfig(robotType);
            if (config != null)
                return config;
        }

        if (robotType == RobotType.Attacker && assaultConfig != null)
            return assaultConfig;

        return basicConfig;
    }

    private void OnCurrentRobotDeath()
    {
        if (!unlockAttackerOnBaseDeath)
            return;

        if (_currentRobotType != RobotType.Base)
            return;

        if (_unlockManager == null)
            _unlockManager = FindFirstObjectByType<RobotUnlockManager>();

        if (_unlockManager != null)
        {
            _unlockManager.UnlockRobot(RobotType.Attacker);
        }
    }

    private void OnDestroy()
    {
        if (_currentHealth != null)
        {
            _currentHealth.OnDeath -= OnCurrentRobotDeath;
        }
    }
}
