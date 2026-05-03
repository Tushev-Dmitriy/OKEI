using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayCursorPolicy
{
    private const string BootstrapSceneName = "Bootstrap";
    private const string Level2SceneName = "Level2";
    private const string Level4SceneName = "Level4";

    public static bool ActiveSceneNeedsFreeCursor()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == BootstrapSceneName || sceneName == Level2SceneName || sceneName == Level4SceneName;
    }

    public static void ApplyForActiveScene(bool gameplayInputEnabled)
    {
        if (ActiveSceneNeedsFreeCursor())
        {
            ApplyFreeCursor();
            SetStarterAssetsCursorInput(false);
            return;
        }

        if (gameplayInputEnabled)
        {
            ApplyLockedCursor();
            SetStarterAssetsCursorInput(true);
        }
        else
        {
            ApplyFreeCursor();
            SetStarterAssetsCursorInput(false);
        }
    }

    public static void ApplyFreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void ApplyLockedCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void SetStarterAssetsCursorInput(bool enabled)
    {
        StarterAssetsInputs inputs = Object.FindFirstObjectByType<StarterAssetsInputs>();
        if (inputs == null)
        {
            return;
        }

        inputs.cursorLocked = enabled;
        inputs.cursorInputForLook = enabled;
        inputs.LookInput(Vector2.zero);
    }
}
