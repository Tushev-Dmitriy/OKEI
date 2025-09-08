using DevionGames;
using DevionGames.InventorySystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VariableItemSpawn : MonoBehaviour
{
    [SerializeField] private VariableItem _variableItem;

    private void OnEnable()
    {
        Spawn();
    }

    public void Spawn()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = _variableItem.displayColor;
        }

        TextMeshPro objTextData = gameObject.transform.GetChild(1).GetComponent<TextMeshPro>();
        objTextData.color = _variableItem.displayColor;
        objTextData.text = $"{(_variableItem.type).ToString().ToLower()} name = {_variableItem.value};";

        for (int i = 2; i < gameObject.transform.childCount; i++)
        {
            TextMeshPro textMesh = gameObject.transform.GetChild(i).GetComponent<TextMeshPro>();
            textMesh.text = _variableItem.value;
        }
    }
}
