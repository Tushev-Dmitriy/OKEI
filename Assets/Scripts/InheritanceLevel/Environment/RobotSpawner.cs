using UnityEngine;

public class RobotSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject robotPrefab;
    [SerializeField] private bool spawnAssaultRobot;

    [Header("Data")]
    [SerializeField] private RobotConfigSO basicConfig;
    [SerializeField] private RobotConfigSO assaultConfig;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        SpawnRobot();
    }

    public void SpawnRobot()
    {
        GameObject instance = Instantiate(robotPrefab, spawnPoint.position, spawnPoint.rotation);

        Robot robotLogic = null;
        RobotConfigSO configToUse = null;

        if (spawnAssaultRobot)
        {
            robotLogic = instance.GetComponent<AssaultRobot>();
            if (robotLogic == null)
                robotLogic = instance.AddComponent<AssaultRobot>();
            configToUse = assaultConfig;
        }
        else
        {
            robotLogic = instance.GetComponent<Robot>();
            if (robotLogic == null)
                robotLogic = instance.AddComponent<Robot>();
            configToUse = basicConfig;
        }

        if (robotLogic != null && configToUse != null)
        {
            robotLogic.Initialize(configToUse);
        }
    }
}
