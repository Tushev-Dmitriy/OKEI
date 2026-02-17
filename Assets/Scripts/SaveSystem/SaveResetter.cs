using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveResetter : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> sceneSaveables = new();
    [SerializeField] private List<InventorySaver> inventorySavers = new();

    public void ResetSaves()
    {
        PlayerSaveSystem.DeleteSave();
        InventorySaveSystem.DeleteSave();
    }

    public void CollectSaveablesInScene()
    {
#if UNITY_EDITOR
        Undo.RecordObject(this, "Collect Saveables");
#endif

        sceneSaveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<ISceneSaveable>()
            .Select(x => x as MonoBehaviour)
            .ToList();

        inventorySavers = FindObjectsByType<InventorySaver>(FindObjectsSortMode.None)
            .ToList();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}
