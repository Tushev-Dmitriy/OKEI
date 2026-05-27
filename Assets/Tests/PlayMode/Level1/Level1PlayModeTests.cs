using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class Level1PlayModeTests
{
    [UnityTest]
    public IEnumerator Level1Scene_LoadsDoorGameplayComponents()
    {
        yield return SceneTestUtility.LoadScene("Level1");

        UnityEngine.Object condition = SceneTestUtility.FindFirstGameplayObject("DoorCondition");
        UnityEngine.Object finalPortal = SceneTestUtility.FindFirstGameplayObject("FinalPortal");

        Assert.That(condition, Is.Not.Null);
        Assert.That(finalPortal, Is.Not.Null);
    }
}
