using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class RobotUnlockManager : MonoBehaviour
{
    [SerializeField] private List<RobotConfigSO> allRobotConfigs = new List<RobotConfigSO>();
    [SerializeField] private List<RobotType> unlockOrder = new List<RobotType>
    {
        RobotType.Base,
        RobotType.Attacker,
        RobotType.Healer,
        RobotType.Defender
    };

    private HashSet<RobotType> _unlockedRobots = new HashSet<RobotType>();
    private RobotUnlockEvents _events;

    public event Action OnProgressApplied;
    public event Action<RobotType> OnRobotUnlocked;

    [Header("Debug/Reset")]
    [SerializeField] private bool enableResetHotkey = true;
    [SerializeField] private KeyCode resetHotkey = KeyCode.F9;
    [SerializeField] private bool saveAfterReset = true;

    [Inject]
    public void Construct(RobotUnlockEvents events)
    {
        _events = events;
    }

    private void Awake()
    {
        _unlockedRobots.Clear();
        _unlockedRobots.Add(RobotType.Base);
    }

    private void Start()
    {
        if (_events != null)
        {
            _events.OnUnlockRequested += HandleUnlockRequest;
        }
    }

    private void Update()
    {
        if (enableResetHotkey && Input.GetKeyDown(resetHotkey))
        {
            ResetUnlocks(saveAfterReset);
        }
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.OnUnlockRequested -= HandleUnlockRequest;
        }
    }

    private void HandleUnlockRequest(RobotType robotType)
    {
        
        if (_unlockedRobots.Contains(robotType))
        {
            return;
        }

        _unlockedRobots.Add(robotType);
        
        OnRobotUnlocked?.Invoke(robotType);
        _events?.NotifyUnlocked(robotType);
    }

    public void UnlockNextRobot()
    {
        RobotType next = GetNextRobotToUnlock();
        if (next != RobotType.None)
        {
            _events?.RequestUnlock(next);
        }
    }

    public bool IsRobotUnlocked(RobotType robotType)
    {
        return _unlockedRobots.Contains(robotType);
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

    public RobotConfigSO GetRobotConfig(RobotType robotType)
    {
        return allRobotConfigs.FirstOrDefault(config => config.robotType == robotType);
    }

    public bool UnlockRobot(RobotType robotType)
    {
        if (_unlockedRobots.Contains(robotType))
            return false;

        HandleUnlockRequest(robotType);
        return true;
    }

    public IReadOnlyList<RobotConfigSO> GetAllRobotConfigs()
    {
        return allRobotConfigs;
    }

    public RobotProgressData CaptureProgress()
    {
        return new RobotProgressData
        {
            unlockedRobotTypes = _unlockedRobots.Select(t => (int)t).ToList()
        };
    }

    public void ApplyProgress(RobotProgressData data)
    {
        _unlockedRobots.Clear();

        if (data != null && data.unlockedRobotTypes != null && data.unlockedRobotTypes.Count > 0)
        {
            foreach (var rawType in data.unlockedRobotTypes)
            {
                var robotType = (RobotType)rawType;
                if (robotType == RobotType.None)
                    continue;

                _unlockedRobots.Add(robotType);
            }
        }

        _unlockedRobots.Add(RobotType.Base);
        OnProgressApplied?.Invoke();
    }

    public List<RobotType> GetUnlockedRobots()
    {
        return _unlockedRobots.ToList();
    }

    public void ResetUnlocks(bool save = true)
    {
        _unlockedRobots.Clear();
        _unlockedRobots.Add(RobotType.Base);

        OnProgressApplied?.Invoke();

        if (save)
        {
            TrySaveThroughPlayerSaver();
        }
    }

    [ContextMenu("Debug: Unlock Next")]
    private void DebugUnlockNext()
    {
        UnlockNextRobot();
    }

    [ContextMenu("Debug: Reset All (and Save)")]
    private void DebugResetAll()
    {
        ResetUnlocks(true);
    }

    private void TrySaveThroughPlayerSaver()
    {
        var saver = FindFirstObjectByType<PlayerSaver>();
        if (saver != null)
        {
            saver.SavePlayerData();
            return;
        }

    }
}

