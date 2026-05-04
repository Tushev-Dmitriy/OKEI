using UnityEngine;

public class LockInputs : MonoBehaviour
{
    [SerializeField] private LockControlSystem lockControlSystem;
    [SerializeField] private bool powerEnabled;
    [SerializeField] private bool coolingEnabled;
    [SerializeField] private bool safeModeEnabled;
    [SerializeField] private bool inputEnabled = true;

    public bool PowerEnabled => powerEnabled;
    public bool CoolingEnabled => coolingEnabled;
    public bool SafeModeEnabled => safeModeEnabled;
    public bool InputEnabled => inputEnabled;

    public void SetSystem(LockControlSystem system)
    {
        lockControlSystem = system;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void RestoreState(bool power, bool cooling, bool safeMode, bool enabled)
    {
        powerEnabled = power;
        coolingEnabled = cooling;
        safeModeEnabled = safeMode;
        inputEnabled = enabled;
    }

    public void SetPowerState(bool enabled)
    {
        if (!CanChangeSwitchState())
            return;

        if (powerEnabled == enabled)
            return;

        powerEnabled = enabled;
        GameplaySaveManager.SaveCurrentGame();
    }

    public void SetCoolingState(bool enabled)
    {
        if (!CanChangeSwitchState())
            return;

        if (coolingEnabled == enabled)
            return;

        coolingEnabled = enabled;
        GameplaySaveManager.SaveCurrentGame();
    }

    public void SetSafeModeState(bool enabled)
    {
        if (!CanChangeSwitchState())
            return;

        if (safeModeEnabled == enabled)
            return;

        safeModeEnabled = enabled;
        GameplaySaveManager.SaveCurrentGame();
    }

    public void TogglePower()
    {
        SetPowerState(!powerEnabled);
    }

    public void ToggleCooling()
    {
        SetCoolingState(!coolingEnabled);
    }

    public void ToggleSafeMode()
    {
        SetSafeModeState(!safeModeEnabled);
    }

    public void PumpForFive()
    {
        if (!CanUseForActions())
            return;

        if (lockControlSystem.TryStartPumpFor5())
            GameplaySaveManager.SaveCurrentGame();
    }

    public void WaterForTen()
    {
        if (!CanUseForActions())
            return;

        if (lockControlSystem.TryStartWaterPrimaryFor())
            GameplaySaveManager.SaveCurrentGame();
    }

    public void WaterForFive()
    {
        if (!CanUseForActions())
            return;

        if (lockControlSystem.TryStartWaterSecondaryFor())
            GameplaySaveManager.SaveCurrentGame();
    }

    public void LiftForTen()
    {
        if (!CanUseForActions())
            return;

        if (lockControlSystem.TryStartLiftSecondaryFor())
            GameplaySaveManager.SaveCurrentGame();
    }

    public void LiftForTwentyFive()
    {
        if (!CanUseForActions())
            return;

        if (lockControlSystem.TryStartLiftPrimaryFor())
            GameplaySaveManager.SaveCurrentGame();
    }

    private bool CanChangeSwitchState()
    {
        if (!inputEnabled)
            return false;

        if (lockControlSystem == null)
            return true;

        return lockControlSystem.CanReceiveInput;
    }

    private bool CanUseForActions()
    {
        if (!inputEnabled || lockControlSystem == null)
            return false;

        return lockControlSystem.CanReceiveInput;
    }
}
