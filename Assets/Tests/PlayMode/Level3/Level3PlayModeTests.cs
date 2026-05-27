using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class Level3PlayModeTests
{
    [UnityTest]
    public IEnumerator Level3Scene_LoadsArtifactsAndFinalDoor()
    {
        yield return SceneTestUtility.LoadScene("Level3");

        UnityEngine.Object manager = SceneTestUtility.FindFirstGameplayObject("Level3ArtifactManager");
        UnityEngine.Object finalDoor = SceneTestUtility.FindFirstGameplayObject("FinalDoorController");

        Assert.That(manager, Is.Not.Null);
        int totalArtifacts = (int)manager.GetType().GetProperty("TotalArtifacts")!.GetValue(manager);
        Assert.That(totalArtifacts, Is.GreaterThan(0));
        Assert.That(finalDoor, Is.Not.Null);
    }
}
