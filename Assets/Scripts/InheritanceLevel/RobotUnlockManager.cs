using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class RobotUnlockManager : MonoBehaviour
{
    [SerializeField] private List<RobotConfigSO> allRobotConfigs = new List<RobotConfigSO>();
    private HashSet<RobotType> _unlockedRobots = new HashSet<RobotType>();
    private RobotUnlockEvents _events;
    private readonly Dictionary<RobotType, int> _deathCounts = new Dictionary<RobotType, int>();

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
        _deathCounts.Clear();
        _unlockedRobots.Add(RobotType.Base);
    }

    private void Start()
    {
        if (_events != null)
        {
            _events.OnUnlockRequested += HandleUnlockRequest;
        }
    }

    private void OnEnable()
    {
        Robot.OnRobotDied += HandleRobotDied;
    }

    private void OnDisable()
    {
        Robot.OnRobotDied -= HandleRobotDied;
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
        _deathCounts.Clear();

        OnRobotUnlocked?.Invoke(robotType);
        _events?.NotifyUnlocked(robotType);
    }

    public bool IsRobotUnlocked(RobotType robotType)
    {
        return _unlockedRobots.Contains(robotType);
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
        _deathCounts.Clear();

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
        _deathCounts.Clear();
        _unlockedRobots.Add(RobotType.Base);

        OnProgressApplied?.Invoke();

        if (save)
        {
            TrySaveThroughPlayerSaver();
        }
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

    private void HandleRobotDied(RobotType robotType)
    {
        if (robotType == RobotType.None)
            return;

        if (_deathCounts.ContainsKey(robotType))
            _deathCounts[robotType]++;
        else
            _deathCounts[robotType] = 1;

        TryUnlockByConditions();
    }

    private void TryUnlockByConditions()
    {
        foreach (var config in allRobotConfigs)
        {
            if (config == null)
                continue;

            if (config.robotType == RobotType.Base)
                continue;

            if (_unlockedRobots.Contains(config.robotType))
                continue;

            if (AreConditionsMet(config))
            {
                UnlockRobot(config.robotType);
            }
        }
    }

    private bool AreConditionsMet(RobotConfigSO config)
    {
        if (config.unlockConditions == null || config.unlockConditions.Count == 0)
            return false;

        foreach (var condition in config.unlockConditions)
        {
            if (condition == null)
                continue;

            int count = GetDeathCount(condition.robotType);
            if (count < condition.requiredDeaths)
                return false;
        }

        return true;
    }

    private int GetDeathCount(RobotType robotType)
    {
        return _deathCounts.TryGetValue(robotType, out var count) ? count : 0;
    }
}
