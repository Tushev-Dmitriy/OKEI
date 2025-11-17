using DevionGames.InventorySystem;
using System.Collections.Generic;
using UnityEngine;

public static class InventoryWriter
{
    public static List<InventoryData> CollectInventory()
    {
        List<InventoryData> result = new List<InventoryData>();

        ItemContainer inv = InventoryManager.current.PlayerInfo.gameObject.GetComponentInChildren<ItemContainer>();

        foreach (var slot in inv.Slots)
        {
            if (slot.ObservedItem != null)
            {
                result.Add(new InventoryData
                {
                    itemID = slot.ObservedItem.Id,
                    stack = slot.ObservedItem.Stack
                });
            }
        }

        return result;
    }
}
