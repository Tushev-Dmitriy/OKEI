using DevionGames.InventorySystem;
using System.Collections.Generic;
using UnityEngine;
using DevionItem = DevionGames.InventorySystem.Item;

public class SpawnVariableItems : MonoBehaviour
{
    [SerializeField] private List<GameObject> _variableItems;
    [SerializeField] private List<int> _randomRange = new List<int>();
    private List<Vector3> _itemsPos = new List<Vector3>();

    private void Awake()
    {
        FilterItemsBySave();
        SetRandomPosToSpawn();
    }

    private void Start()
    {
        SpawnItems();
    }

    private void SetRandomPosToSpawn()
    {
        for (int i = 0; i < _variableItems.Count; i++)
        {
            Vector3 spawnPos;
            bool validPos = false;

            do
            {
                float x = Random.Range(_randomRange[0], _randomRange[1]);
                float z = Random.Range(_randomRange[2], _randomRange[3]);
                spawnPos = new Vector3(x, 0.1f, z);

                validPos = true;

                foreach (Vector3 pos in _itemsPos)
                {
                    if (Vector3.Distance(pos, spawnPos) < 0.5f)
                    {
                        validPos = false;
                        break;
                    }
                }

            } while (!validPos);

            _itemsPos.Add(spawnPos);
        }

    }

    private void FilterItemsBySave()
    {
        if (!InventorySaveSystem.TryGetSavedItemNames(out var savedNames))
        {
            return;
        }

        if (savedNames.Count == 0)
        {
            return;
        }

        for (int i = _variableItems.Count - 1; i >= 0; i--)
        {
            var prefab = _variableItems[i];
            if (prefab == null)
            {
                _variableItems.RemoveAt(i);
                continue;
            }

            if (IsItemAlreadySaved(prefab, savedNames))
            {
                _variableItems.RemoveAt(i);
            }
        }
    }

    private bool IsItemAlreadySaved(GameObject prefab, HashSet<string> savedNames)
    {
        if (!prefab.TryGetComponent(out ItemCollection collection))
        {
            return false;
        }

        List<DevionItem> items = collection.GetItemsInCollection();
        if (items == null || items.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item != null && savedNames.Contains(item.Name))
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnItems()
    {
        for (int i = 0; i < _variableItems.Count; i++)
        {
            Instantiate(_variableItems[i], _itemsPos[i], Quaternion.identity);
        }
    }
}
