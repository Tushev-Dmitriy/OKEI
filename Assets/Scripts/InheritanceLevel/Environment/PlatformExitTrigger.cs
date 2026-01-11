using UnityEngine;

public class PlatformExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Robot robot = other.GetComponent<Robot>();
        if (robot != null)
        {
            robot.ActivateAutonomousMode();
        }
    }
}
