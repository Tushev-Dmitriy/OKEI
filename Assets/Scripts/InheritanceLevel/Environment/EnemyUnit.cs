using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        Robot incomingRobot = other.gameObject.GetComponent<Robot>();

        if (incomingRobot != null)
        {
            incomingRobot.TryEngageCombat(this);
        }
    }

    public void TakeDamage()
    {
        Debug.Log("Враг уничтожен");
        Destroy(gameObject);
    }
}
