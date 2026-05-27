using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Level4EditModeTests
{
    [Test]
    public void ProgressModule_ReportsSquadModeUnlockedWhenAllSpecialRobotsAreUnlocked()
    {
        GameObject flowObject = new GameObject("Level4Flow");
        GameObject unlockManagerObject = new GameObject("UnlockManager");
        GameObject progressModuleObject = new GameObject("ProgressModule");

        try
        {
            System.Type flowType = AssemblyTypeUtility.ResolveGameplayType("Level4FlowController");
            System.Type unlockManagerType = AssemblyTypeUtility.ResolveGameplayType("RobotUnlockManager");
            System.Type progressModuleType = AssemblyTypeUtility.ResolveGameplayType("Level4ProgressModule");
            System.Type robotProgressDataType = AssemblyTypeUtility.ResolveGameplayType("RobotProgressData");
            System.Type robotType = AssemblyTypeUtility.ResolveGameplayType("RobotType");

            Component flow = flowObject.AddComponent(flowType);
            Component unlockManager = unlockManagerObject.AddComponent(unlockManagerType);
            Component progressModule = progressModuleObject.AddComponent(progressModuleType);

            object progressData = System.Activator.CreateInstance(robotProgressDataType);
            List<int> unlockedRobotTypes = new List<int>
            {
                (int)System.Enum.Parse(robotType, "Base"),
                (int)System.Enum.Parse(robotType, "Attacker"),
                (int)System.Enum.Parse(robotType, "Healer"),
                (int)System.Enum.Parse(robotType, "Defender")
            };

            robotProgressDataType.GetField("unlockedRobotTypes")!.SetValue(progressData, unlockedRobotTypes);
            unlockManagerType.GetMethod("ApplyProgress")!.Invoke(unlockManager, new[] { progressData });

            TestReflectionUtility.SetPrivateField(flow, flowType, "unlockManager", unlockManager);
            TestReflectionUtility.SetPrivateField(flow, flowType, "_progressStage", 0);
            TestReflectionUtility.SetPrivateField(flow, flowType, "_finalSectionUnlocked", false);

            bool squadUnlocked = (bool)progressModuleType.GetMethod("IsSquadModeUnlocked")!.Invoke(progressModule, new object[] { flow });

            Assert.That(squadUnlocked, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(progressModuleObject);
            Object.DestroyImmediate(unlockManagerObject);
            Object.DestroyImmediate(flowObject);
        }
    }
}
