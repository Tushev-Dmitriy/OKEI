using System.Collections.Generic;
using UnityEngine;

public enum VisualModuleType
{
    None = 0,
    Blaster = 1,
    Shield = 2,
    Heal = 3
}

[System.Serializable]
public class UnlockCondition
{
    public RobotType robotType = RobotType.Base;
    public int requiredDeaths = 1;
}

[CreateAssetMenu(fileName = "NewRobotConfig", menuName = "Configs/Robot Config")]
public class RobotConfigSO : ScriptableObject
{
    [Header("Robot Type")]
    public RobotType robotType = RobotType.Base;
    
    [Header("UI Info")]
    public string robotName = "Базовый робот";
    public Sprite robotIcon;
    
    [Header("UI Stats (0-5)")]
    [Range(0, 5)] public int health = 2;
    [Range(0, 5)] public int damage = 1;
    [Range(0, 5)] public int speed = 2;
    
    [Header("Methods")]
    public List<string> methodNames = new List<string>();

    [Header("Unlock Conditions (Deaths)")]
    public List<UnlockCondition> unlockConditions = new List<UnlockCondition>();
    
    [Header("Stats")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Combat")]
    public float damagePerHit = 10f;
    public float attackInterval = 0.5f;

    [Header("Support (Healer)")]
    public float healAmount = 15f;
    public float healInterval = 1.5f;
    public float healRange = 3f;
    public float followDistance = 2f;

    [Header("Visuals")]
    public List<VisualModuleType> activeModules;
}
