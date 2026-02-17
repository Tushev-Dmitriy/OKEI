using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SaveResetter))]
public class SaveResetterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var resetter = (SaveResetter)target;

        GUILayout.Space(8f);

        if (GUILayout.Button("Collect Saveables From Scene"))
        {
            resetter.CollectSaveablesInScene();
        }

        if (GUILayout.Button("Reset Saves"))
        {
            resetter.ResetSaves();
        }
    }
}
