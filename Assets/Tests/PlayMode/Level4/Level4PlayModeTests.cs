using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class Level4PlayModeTests
{
    [UnityTest]
    public IEnumerator Level4Scene_LoadsRobotFlowRuntime()
    {
        yield return SceneTestUtility.LoadScene("Level4");

        UnityEngine.Object flow = SceneTestUtility.FindFirstGameplayObject("Level4FlowController");
        UnityEngine.Object unlockManager = SceneTestUtility.FindFirstGameplayObject("RobotUnlockManager");
        UnityEngine.Object selectionUi = SceneTestUtility.FindFirstGameplayObject("RobotSelectionUI");

        Assert.That(flow, Is.Not.Null);
        Assert.That(unlockManager, Is.Not.Null);
        Assert.That(selectionUi, Is.Not.Null);
    }
}
