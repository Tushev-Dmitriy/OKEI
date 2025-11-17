using DevionGames.InventorySystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class InventoryReader
{
    public static void RestoreInventory(List<InventoryData> saved)
    {
        ItemContainer inv = InventoryManager.current.PlayerInfo.gameObject.GetComponentInChildren<ItemContainer>();

        // очищаем инвентарь
        inv.RemoveItems();

        foreach (var data in saved)
        {
            DevionGames.InventorySystem.Item original = InventoryManager.Database.items
                .First(i => i.Id == data.itemID);

            DevionGames.InventorySystem.Item instance = InventoryManager.CreateInstance(original, data.stack, new ItemModifierList());

            inv.AddItem(instance);
        }
    }
}
