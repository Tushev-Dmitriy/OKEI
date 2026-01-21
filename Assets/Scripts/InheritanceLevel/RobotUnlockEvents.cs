using System;
using UnityEngine;

public class RobotUnlockEvents
{
    public event Action<RobotType> OnRobotHintRequested;

    public event Action<RobotType> OnRobotUnlockRequested;
    public event Action<RobotType> OnRobotUnlocked;
    public event Action<RobotType> OnRobotUnlockFailed;

    public event Action OnRobotProgressLoaded;

    public void RequestRobotHint(RobotType robotType)
    {
        Debug.Log($"[Event] Запрос подсказки для робота: {robotType}");
        OnRobotHintRequested?.Invoke(robotType);
    }

    public void RequestRobotUnlock(RobotType robotType)
    {
        Debug.Log($"[Event] Запрос на открытие робота: {robotType}");
        OnRobotUnlockRequested?.Invoke(robotType);
    }

    public void NotifyRobotUnlocked(RobotType robotType)
    {
        Debug.Log($"[Event] Робот открыт: {robotType}");
        OnRobotUnlocked?.Invoke(robotType);
    }

    public void NotifyRobotUnlockFailed(RobotType robotType)
    {
        Debug.Log($"[Event] Не удалось открыть робота: {robotType}");
        OnRobotUnlockFailed?.Invoke(robotType);
    }

    public void NotifyRobotProgressLoaded()
    {
        Debug.Log($"[Event] Прогресс роботов загружен");
        OnRobotProgressLoaded?.Invoke();
    }
}