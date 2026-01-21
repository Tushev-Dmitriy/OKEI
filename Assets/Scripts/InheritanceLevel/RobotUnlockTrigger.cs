using UnityEngine;
using Zenject;

public class RobotUnlockTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private RobotType requiredRobotType = RobotType.Attacker;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float triggerDelay = 0.5f;

    private bool _hasTriggered = false;
    private RobotUnlockEvents _events;
    private RobotUnlockManager _unlockManager;

    [Inject]
    public void Construct(RobotUnlockEvents events, RobotUnlockManager unlockManager)
    {
        _events = events;
        _unlockManager = unlockManager;
    }

    public void TriggerHint()
    {
        if (triggerOnce && _hasTriggered)
            return;

        if (_unlockManager != null && _unlockManager.IsRobotUnlocked(requiredRobotType))
        {
            Debug.Log($"Робот {requiredRobotType} уже открыт. Подсказка не показывается.");
            return;
        }

        _hasTriggered = true;

        Invoke(nameof(SendHintRequest), triggerDelay);
    }

    private void SendHintRequest()
    {
        if (_events != null)
        {
            _events.RequestRobotHint(requiredRobotType);
        }
        else
        {
            Debug.LogWarning("RobotUnlockEvents не найдена!");
        }
    }

    public void ResetTrigger()
    {
        _hasTriggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerHint();
        }
    }

    [ContextMenu("Test Trigger")]
    private void TestTrigger()
    {
        TriggerHint();
    }
}
