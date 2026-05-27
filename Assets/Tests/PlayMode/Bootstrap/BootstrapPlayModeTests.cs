using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class BootstrapPlayModeTests
{
    [UnityTest]
    public IEnumerator BootstrapScene_LoadsWithConfiguredLevelProgressManager()
    {
        yield return SceneTestUtility.LoadScene("Bootstrap");

        UnityEngine.Object progressManager = SceneTestUtility.FindFirstGameplayObject("LevelProgressManager");

        Assert.That(progressManager, Is.Not.Null);
        int levelCount = (int)progressManager.GetType().GetMethod("GetConfiguredLevelCount")!.Invoke(progressManager, null);
        Assert.That(levelCount, Is.GreaterThanOrEqualTo(4));
    }
}
