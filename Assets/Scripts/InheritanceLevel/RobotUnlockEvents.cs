using System;
using UnityEngine;

public class RobotUnlockEvents
{
    public event Action<RobotType> OnUnlockRequested;
    public event Action<RobotType> OnUnlocked;

    public void RequestUnlock(RobotType robotType)
    {
        OnUnlockRequested?.Invoke(robotType);
    }

    public void NotifyUnlocked(RobotType robotType)
    {
        OnUnlocked?.Invoke(robotType);
    }
}
