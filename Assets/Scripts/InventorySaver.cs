using DevionGames.InventorySystem;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventorySaver : MonoBehaviour
{
    [SerializeField] private ItemCollection playerInventory;

    private void Start()
    {
        InventorySaveSystem.LoadInventory(playerInventory);
    }

    public void SaveInventory()
    {
        InventorySaveSystem.SaveInventory(playerInventory);
    }
}
