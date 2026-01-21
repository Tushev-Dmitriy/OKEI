using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class RobotUnlockManager : MonoBehaviour
{
    private RobotUnlockEvents _events;

    [Header("Robot Configs")]
    [SerializeField] private List<RobotConfigSO> allRobotConfigs = new List<RobotConfigSO>();

    [Header("Unlock Order")]
    [SerializeField] private List<RobotType> unlockOrder = new List<RobotType>
    {
        RobotType.Base,
        RobotType.Attacker,
        RobotType.Healer,
        RobotType.Defender
    };

    private HashSet<RobotType> _unlockedRobots = new HashSet<RobotType>();

    [Inject]
    public void Construct(RobotUnlockEvents events)
    {
        _events = events;
    }

    private void Awake()
    {
        InitializeDefaultRobot();
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (_events != null)
        {
            _events.OnRobotUnlockRequested += HandleUnlockRequest;
        }
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.OnRobotUnlockRequested -= HandleUnlockRequest;
        }
    }

    private void InitializeDefaultRobot()
    {
        if (!_unlockedRobots.Contains(RobotType.Base))
        {
            _unlockedRobots.Add(RobotType.Base);
        }
    }

    public bool IsRobotUnlocked(RobotType robotType)
    {
        return _unlockedRobots.Contains(robotType);
    }

    public bool UnlockNextRobot()
    {
        RobotType nextRobot = GetNextRobotToUnlock();
        
        if (nextRobot != RobotType.None)
        {
            return UnlockRobot(nextRobot);
        }
        
        Debug.LogWarning("Все роботы уже открыты!");
        return false;
    }

    private void HandleUnlockRequest(RobotType robotType)
    {
        bool success = UnlockRobot(robotType);
        
        if (success)
        {
            _events.NotifyRobotUnlocked(robotType);
        }
        else
        {
            _events.NotifyRobotUnlockFailed(robotType);
        }
    }

    private bool UnlockRobot(RobotType robotType)
    {
        if (robotType == RobotType.None)
        {
            Debug.LogWarning("Попытка открыть None робота!");
            return false;
        }

        if (_unlockedRobots.Contains(robotType))
        {
            Debug.LogWarning($"Робот {robotType} уже открыт!");
            return false;
        }

        _unlockedRobots.Add(robotType);
        
        Debug.Log($"Робот {robotType} открыт!");
        return true;
    }

    public RobotType GetNextRobotToUnlock()
    {
        foreach (var robotType in unlockOrder)
        {
            if (!_unlockedRobots.Contains(robotType))
            {
                return robotType;
            }
        }
        
        return RobotType.None;
    }

    public List<RobotType> GetUnlockedRobots()
    {
        return _unlockedRobots.ToList();
    }

    public List<RobotConfigSO> GetUnlockedRobotConfigs()
    {
        return allRobotConfigs.Where(config => IsRobotUnlocked(config.robotType)).ToList();
    }

    public RobotConfigSO GetRobotConfig(RobotType robotType)
    {
        return allRobotConfigs.FirstOrDefault(config => config.robotType == robotType);
    }

    public bool CanUnlockNextRobot()
    {
        return GetNextRobotToUnlock() != RobotType.None;
    }

    public string GetNextRobotName()
    {
        RobotType nextRobot = GetNextRobotToUnlock();
        if (nextRobot == RobotType.None)
            return "Все роботы открыты";

        RobotConfigSO config = GetRobotConfig(nextRobot);
        return config != null ? config.robotName : nextRobot.ToString();
    }

    public void LoadUnlockedRobots(List<RobotType> unlockedRobots)
    {
        _unlockedRobots.Clear();
        
        if (unlockedRobots != null && unlockedRobots.Count > 0)
        {
            foreach (var robotType in unlockedRobots)
            {
                _unlockedRobots.Add(robotType);
            }
        }
        else
        {
            InitializeDefaultRobot();
        }

        if (_events != null)
        {
            _events.NotifyRobotProgressLoaded();
        }
    }

    public List<RobotType> GetUnlockedRobotsForSave()
    {
        return _unlockedRobots.ToList();
    }


    [ContextMenu("Unlock Next Robot")]
    private void DebugUnlockNext()
    {
        UnlockNextRobot();
    }

    [ContextMenu("Reset All Robots")]
    private void DebugResetAll()
    {
        _unlockedRobots.Clear();
        InitializeDefaultRobot();
        Debug.Log("Все роботы сброшены!");
    }

    [ContextMenu("Unlock All Robots")]
    private void DebugUnlockAll()
    {
        foreach (var robotType in unlockOrder)
        {
            if (!_unlockedRobots.Contains(robotType))
            {
                UnlockRobot(robotType);
            }
        }
    }
}
