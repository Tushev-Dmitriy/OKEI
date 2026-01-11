using UnityEngine;

public class RobotMoverPlatform : MonoBehaviour
{
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private float moveSpeed = 2f;

    private void OnCollisionStay(Collision collision)
    {
        Robot robot = collision.gameObject.GetComponent<Robot>();
        if (robot != null)
        {
            robot.transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
        }
    }
}
