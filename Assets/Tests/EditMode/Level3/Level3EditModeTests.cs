using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Level3EditModeTests
{
    [Test]
    public void ArtifactManager_MarksFinalDoorOpenedAfterAllArtifactsCollected()
    {
        GameObject managerObject = new GameObject("ArtifactManager");
        GameObject artifactOneObject = new GameObject("ArtifactOne");
        GameObject artifactTwoObject = new GameObject("ArtifactTwo");

        try
        {
            System.Type managerType = AssemblyTypeUtility.ResolveGameplayType("Level3ArtifactManager");
            System.Type artifactType = AssemblyTypeUtility.ResolveGameplayType("Level3Artifact");
            System.Type artifactListType = typeof(List<>).MakeGenericType(artifactType);

            Component manager = managerObject.AddComponent(managerType);
            artifactOneObject.AddComponent<BoxCollider>();
            artifactTwoObject.AddComponent<BoxCollider>();
            Component artifactOne = artifactOneObject.AddComponent(artifactType);
            Component artifactTwo = artifactTwoObject.AddComponent(artifactType);
            object typedArtifactList = System.Activator.CreateInstance(artifactListType);
            artifactListType.GetMethod("Add")!.Invoke(typedArtifactList, new object[] { artifactOne });
            artifactListType.GetMethod("Add")!.Invoke(typedArtifactList, new object[] { artifactTwo });

            TestReflectionUtility.SetPrivateField(
                manager,
                managerType,
                "_artifacts",
                typedArtifactList);

            managerType.GetMethod("NotifyArtifactRestored")?.Invoke(manager, new object[] { artifactOne });
            Assert.That((int)managerType.GetProperty("CollectedArtifacts")!.GetValue(manager), Is.EqualTo(1));
            Assert.That((bool)TestReflectionUtility.GetPrivateField(manager, managerType, "_finalDoorOpened"), Is.False);

            managerType.GetMethod("NotifyArtifactRestored")?.Invoke(manager, new object[] { artifactTwo });

            Assert.That((int)managerType.GetProperty("CollectedArtifacts")!.GetValue(manager), Is.EqualTo(2));
            Assert.That((bool)TestReflectionUtility.GetPrivateField(manager, managerType, "_finalDoorOpened"), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(artifactOneObject);
            Object.DestroyImmediate(artifactTwoObject);
            Object.DestroyImmediate(managerObject);
        }
    }
}
