using UnityEngine;
using UnityEngine.Events;
using Zenject;

public class RobotUnlockTrigger : MonoBehaviour
{
    [SerializeField] private RobotType robotTypeToUnlock = RobotType.Attacker;
    
    public UnityEvent OnTriggerActivated;

    private RobotUnlockEvents _events;
    private RobotUnlockManager _unlockManager;

    [Inject]
    public void Construct(RobotUnlockEvents events)
    {
        _events = events;
    }

    public void Trigger()
    {

        if (_unlockManager == null)
            _unlockManager = FindFirstObjectByType<RobotUnlockManager>();

        if (_unlockManager != null && _unlockManager.IsRobotUnlocked(robotTypeToUnlock))
        {
            return;
        }
        
        _events?.RequestUnlock(robotTypeToUnlock);
        OnTriggerActivated?.Invoke();
    }

    [ContextMenu("Test Trigger")]
    private void TestTrigger()
    {
        Trigger();
    }
}

