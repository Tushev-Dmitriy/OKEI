using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class BootstrapMenuSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private bool buildIfMenuMissing = true;
    [SerializeField] private bool saveSceneAfterBuild = true;

#if UNITY_EDITOR
    private bool _buildQueued;
#endif

    private void OnEnable()
    {
#if UNITY_EDITOR
        QueueBuild();
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Bootstrap Menu")]
    private void RebuildBootstrapMenu()
    {
        if (!IsBootstrapSceneObject())
        {
            return;
        }

        BootstrapMenuSceneBuilder.BuildCurrentScene(gameObject, saveSceneAfterBuild);
    }

    private void QueueBuild()
    {
        if (_buildQueued || !buildIfMenuMissing || !IsBootstrapSceneObject())
        {
            return;
        }

        _buildQueued = true;
        EditorApplication.delayCall += HandleDelayedBuild;
    }

    private void HandleDelayedBuild()
    {
        EditorApplication.delayCall -= HandleDelayedBuild;
        _buildQueued = false;

        if (this == null || !buildIfMenuMissing || !IsBootstrapSceneObject())
        {
            return;
        }

        if (BootstrapMenuSceneBuilder.HasExpectedHierarchy(gameObject.scene))
        {
            return;
        }

        BootstrapMenuSceneBuilder.BuildCurrentScene(gameObject, saveSceneAfterBuild);
    }

    private bool IsBootstrapSceneObject()
    {
        return gameObject.scene.IsValid() && gameObject.scene.path == BootstrapMenuSceneBuilder.ScenePath;
    }
#endif
}
