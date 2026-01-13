using System.Collections.Generic;
using UnityEngine;

public enum VisualModuleType
{
    None = 0,
    Blaster = 1,
    Shield = 2,
    Antenna = 3
}

[CreateAssetMenu(fileName = "NewRobotConfig", menuName = "Configs/Robot Config")]
public class RobotConfigSO : ScriptableObject
{
    [Header("Stats")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Combat")]
    public float damagePerHit = 10f;
    public float attackInterval = 0.5f;

    [Header("Visuals")]
    public List<VisualModuleType> activeModules;
}
