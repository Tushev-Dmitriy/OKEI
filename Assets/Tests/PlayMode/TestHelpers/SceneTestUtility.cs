using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SceneTestUtility
{
    public static IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!operation.isDone)
        {
            yield return null;
        }

        yield return null;
        yield return null;
    }

    public static Object FindFirstGameplayObject(string typeName)
    {
        System.Type gameplayType = AssemblyTypeUtility.ResolveGameplayType(typeName);
        Object directMatch = Object.FindFirstObjectByType(gameplayType);
        if (directMatch != null)
        {
            return directMatch;
        }

        foreach (Object candidate in Resources.FindObjectsOfTypeAll(gameplayType))
        {
            if (candidate is Component component)
            {
                if (component.gameObject.scene.IsValid())
                {
                    return component;
                }

                continue;
            }

            if (candidate is GameObject gameObject && gameObject.scene.IsValid())
            {
                return gameObject;
            }
        }

        return null;
    }
}
