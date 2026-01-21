using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class RobotProgressSaver : MonoBehaviour
{
    private RobotUnlockManager _unlockManager;

    [Inject]
    public void Construct(RobotUnlockManager unlockManager)
    {
        _unlockManager = unlockManager;
    }

    public void SaveRobotProgress(SaveData saveData)
    {
        if (_unlockManager == null)
        {
            Debug.LogWarning("RobotUnlockManager не найден!");
            return;
        }

        if (saveData.robotProgress == null)
        {
            saveData.robotProgress = new RobotProgressData();
        }

        List<RobotType> unlockedRobots = _unlockManager.GetUnlockedRobotsForSave();
        saveData.robotProgress.unlockedRobotTypes = unlockedRobots.Select(r => (int)r).ToList();

        Debug.Log($"Прогресс роботов сохранен. Открыто роботов: {unlockedRobots.Count}");
    }

    public void LoadRobotProgress(SaveData saveData)
    {
        if (_unlockManager == null)
        {
            Debug.LogWarning("RobotUnlockManager не найден!");
            return;
        }

        if (saveData.robotProgress == null || saveData.robotProgress.unlockedRobotTypes == null)
        {
            Debug.Log("Данные о роботах не найдены. Загружается базовый робот.");
            _unlockManager.LoadUnlockedRobots(new List<RobotType>());
            return;
        }

        List<RobotType> unlockedRobots = saveData.robotProgress.unlockedRobotTypes
            .Select(i => (RobotType)i)
            .ToList();

        _unlockManager.LoadUnlockedRobots(unlockedRobots);

        Debug.Log($"Прогресс роботов загружен. Открыто роботов: {unlockedRobots.Count}");
    }
}
