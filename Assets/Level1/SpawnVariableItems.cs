using System.Collections.Generic;
using UnityEngine;

public class SpawnVariableItems : MonoBehaviour
{
    [SerializeField] private List<VariableItem> _variableItems;
    private List<Vector3> _itemsPos = new List<Vector3>();

    private void Awake()
    {
        SetRandomPosToSpawn();
    }

    private void Start()
    {
        SpawnItems();
    }

    private void SetRandomPosToSpawn()
    {
        int x, z;

        for (int i = 0; i < _variableItems.Count; i++)
        {
            x = Random.Range(-10, 10);
            z = Random.Range(-10, 10);
            _itemsPos.Add(new Vector3(x, 0.5f, z));
        }
    }

    private void SpawnItems()
    {
        for (int i = 0; i < _variableItems.Count; i++)
        {
            GameObject tempItem = Instantiate(_variableItems[i].objectPrefab, _itemsPos[i], Quaternion.identity);
            tempItem.GetComponent<VariableItemSpawn>().Spawn(_variableItems[i]);
        }
    }
}
