using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private List<Item> _items = new List<Item>();

    public void AddItem(Item item)
    {
        _items.Add(item);
        Debug.Log($"Added item: {item.itemName}");
    }

    public void RemoveItem(Item item)
    {
        _items.Remove(item);
        Debug.Log($"Removed item: {item.itemName}");
    }

    public IEnumerable<Item> GetItems() => _items;
}
