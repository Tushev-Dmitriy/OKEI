using UnityEngine;

[RequireComponent(typeof(RobotVisualController))]
[RequireComponent(typeof(Rigidbody))]
public class Robot : MonoBehaviour
{
    protected RobotConfigSO config;
    protected bool isAutonomous = false;
    protected RobotVisualController visualController;

    protected virtual void Awake()
    {
        visualController = GetComponent<RobotVisualController>();
    }

    public void Initialize(RobotConfigSO settings)
    {
        config = settings;
        visualController.InitializeVisuals(config);
    }

    public void ActivateAutonomousMode()
    {
        isAutonomous = true;
    }

    protected virtual void Update()
    {
        if (isAutonomous && config != null)
        {
            Move();
        }
    }

    protected virtual void Move()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.MovePosition(rb.position + transform.forward * config.moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.Translate(Vector3.forward * config.moveSpeed * Time.deltaTime);
        }
    }

    public virtual void TryEngageCombat(EnemyUnit enemy)
    {
        Debug.Log($"Робот не умеет сражаться");
        
        Die();
    }

    protected void Die()
    {
        Debug.Log($"{gameObject.name} уничтожен");
        Destroy(gameObject);
    }
}
