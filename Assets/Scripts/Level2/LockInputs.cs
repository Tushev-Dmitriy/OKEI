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

    public void SetPowerState(bool enabled)
    {
        if (!CanChangeSwitchState())
            return;

        powerEnabled = enabled;
    }

    public void SetCoolingState(bool enabled)
    {
        if (!CanChangeSwitchState())
            return;

        coolingEnabled = enabled;
    }

    public void SetSafeModeState(bool enabled)
    {
        if (!CanChangeSwitchState())
            return;

        safeModeEnabled = enabled;
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

        lockControlSystem.TryStartPumpFor5();
    }

    public void WaterForTen()
    {
        if (!CanUseForActions())
            return;

        lockControlSystem.TryStartWaterPrimaryFor();
    }

    public void WaterForFive()
    {
        if (!CanUseForActions())
            return;

        lockControlSystem.TryStartWaterSecondaryFor();
    }

    public void LiftForTen()
    {
        if (!CanUseForActions())
            return;

        lockControlSystem.TryStartLiftSecondaryFor();
    }

    public void LiftForTwentyFive()
    {
        if (!CanUseForActions())
            return;

        lockControlSystem.TryStartLiftPrimaryFor();
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
