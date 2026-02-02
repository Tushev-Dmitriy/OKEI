using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RobotUnlockManager))]
public class RobotUnlockManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (RobotUnlockManager)target;

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset Robot Unlocks (Save)"))
            {
                manager.ResetUnlocks(true);
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Reset Robot Unlocks (No Save)"))
            {
                manager.ResetUnlocks(false);
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
