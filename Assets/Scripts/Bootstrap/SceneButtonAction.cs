using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonAction : MonoBehaviour
{
    public enum ActionType
    {
        LoadScenes,
        UnloadScenes,
        UnloadAllAndLoadSingle
    }

    [Header("Main")]
    public ActionType action = ActionType.LoadScenes;
    public string[] scenes;

    [Header("Load options")]
    public bool loadAdditive = true;
    public string setActiveSceneAfterLoad;

    [Header("Unload options")]
    public bool dontUnloadThisScene = true;

    [Header("Bootstrap")]
    public string bootstrapSceneName = "Bootstrap";
    public bool unloadBootstrapAfterLoad = true;

    [Header("Advanced")]
    public bool waitForOperations = true;

    private bool _isBusy;

    public void Execute()
    {
        if (_isBusy) return;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        _isBusy = true;

        switch (action)
        {
            case ActionType.LoadScenes:
                yield return DoLoadScenes();
                break;

            case ActionType.UnloadScenes:
                yield return DoUnloadScenes();
                break;

            case ActionType.UnloadAllAndLoadSingle:
                yield return DoUnloadAllAndLoadSingle();
                break;
        }

        _isBusy = false;
    }

    private IEnumerator DoLoadScenes()
    {
        if (scenes == null || scenes.Length == 0) yield break;

        if (!loadAdditive)
        {
            yield return LoadScene(scenes[0], LoadSceneMode.Single);

            for (int i = 1; i < scenes.Length; i++)
                yield return LoadScene(scenes[i], LoadSceneMode.Additive);
        }
        else
        {
            for (int i = 0; i < scenes.Length; i++)
                yield return LoadScene(scenes[i], LoadSceneMode.Additive);
        }

        TrySetActiveScene(setActiveSceneAfterLoad);
        EnsureActiveSceneIsNotBootstrap();

        if (unloadBootstrapAfterLoad)
            yield return UnloadBootstrapIfLoaded();
    }

    private IEnumerator DoUnloadScenes()
    {
        if (scenes == null || scenes.Length == 0) yield break;

        string thisSceneName = gameObject.scene.name;

        for (int i = 0; i < scenes.Length; i++)
        {
            string sceneName = scenes[i];
            if (string.IsNullOrWhiteSpace(sceneName)) continue;

            if (dontUnloadThisScene && sceneName == thisSceneName)
                continue;

            if (!IsSceneLoaded(sceneName))
                continue;

            yield return UnloadScene(sceneName);
        }
    }

    private IEnumerator DoUnloadAllAndLoadSingle()
    {
        if (scenes == null || scenes.Length == 0 || string.IsNullOrWhiteSpace(scenes[0]))
            yield break;

        yield return LoadScene(scenes[0], LoadSceneMode.Single);

        for (int i = 1; i < scenes.Length; i++)
            yield return LoadScene(scenes[i], LoadSceneMode.Additive);

        TrySetActiveScene(setActiveSceneAfterLoad);
        EnsureActiveSceneIsNotBootstrap();

        if (unloadBootstrapAfterLoad)
            yield return UnloadBootstrapIfLoaded();
    }

    private IEnumerator LoadScene(string sceneName, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) yield break;

        if (mode == LoadSceneMode.Additive && IsSceneLoaded(sceneName))
            yield break;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null) yield break;

        if (waitForOperations)
        {
            while (!op.isDone)
                yield return null;
        }
    }

    private IEnumerator UnloadScene(string sceneName)
    {
        AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null) yield break;

        if (waitForOperations)
        {
            while (!op.isDone)
                yield return null;
        }
    }

    private IEnumerator UnloadBootstrapIfLoaded()
    {
        if (string.IsNullOrWhiteSpace(bootstrapSceneName))
            yield break;

        Scene bootstrap = SceneManager.GetSceneByName(bootstrapSceneName);

        if (!bootstrap.IsValid() || !bootstrap.isLoaded)
            yield break;

        if (SceneManager.GetActiveScene().name == bootstrapSceneName)
            yield break;

        AsyncOperation op = SceneManager.UnloadSceneAsync(bootstrapSceneName);
        if (op == null) yield break;

        while (!op.isDone)
            yield return null;
    }

    private bool IsSceneLoaded(string sceneName)
    {
        Scene s = SceneManager.GetSceneByName(sceneName);
        return s.IsValid() && s.isLoaded;
    }

    private void TrySetActiveScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        Scene s = SceneManager.GetSceneByName(sceneName);
        if (s.IsValid() && s.isLoaded)
            SceneManager.SetActiveScene(s);
    }

    private void EnsureActiveSceneIsNotBootstrap()
    {
        if (SceneManager.GetActiveScene().name != bootstrapSceneName)
            return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.IsValid() && s.isLoaded && s.name != bootstrapSceneName)
            {
                SceneManager.SetActiveScene(s);
                return;
            }
        }
    }
}