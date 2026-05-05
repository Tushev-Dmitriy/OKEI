using DevionGames.InventorySystem;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventorySaver : MonoBehaviour
{
    [SerializeField] private ItemCollection playerInventory;

    private void Awake()
    {
        if (playerInventory == null)
            return;

        playerInventory.onItemAdded.AddListener(HandleInventoryChanged);
        playerInventory.onItemRemoved.AddListener(HandleInventoryChanged);
    }

    private void Start()
    {
        InventorySaveSystem.LoadInventory(playerInventory);
    }

    private void OnDestroy()
    {
        if (playerInventory == null)
            return;

        playerInventory.onItemAdded.RemoveListener(HandleInventoryChanged);
        playerInventory.onItemRemoved.RemoveListener(HandleInventoryChanged);
    }

    public void SaveInventory()
    {
        InventorySaveSystem.SaveInventory(playerInventory);
    }

    private void HandleInventoryChanged()
    {
        SaveInventory();
        GameplaySaveManager.SaveCurrentGame();
    }
}
