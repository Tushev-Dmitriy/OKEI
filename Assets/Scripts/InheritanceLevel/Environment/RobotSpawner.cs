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

    private RobotType _selectedRobotType;
    private readonly List<GameObject> _instances = new List<GameObject>();
    private GameObject _currentInstance;

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

        if (replaceExisting && _currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
            _instances.Clear();
        }

        GameObject instance = Instantiate(robotPrefab, spawnPoint.position, spawnPoint.rotation);
        _currentInstance = instance;
        _instances.Add(instance);

        Robot robotLogic = null;
        RobotConfigSO configToUse = ResolveConfig(robotType);

        if (robotType == RobotType.Attacker)
        {
            robotLogic = instance.GetComponent<AssaultRobot>();
            if (robotLogic == null)
                robotLogic = instance.AddComponent<AssaultRobot>();
        }
        else if (robotType == RobotType.Healer)
        {
            robotLogic = instance.GetComponent<HealerRobot>();
            if (robotLogic == null)
                robotLogic = instance.AddComponent<HealerRobot>();
        }
        else if (robotType == RobotType.Defender)
        {
            robotLogic = instance.GetComponent<DefenderRobot>();
            if (robotLogic == null)
                robotLogic = instance.AddComponent<DefenderRobot>();
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

    private void OnDestroy()
    {
    }
}
