using System.Collections.Generic;
using UnityEngine;

public class SpawnVariableItems : MonoBehaviour
{
    [SerializeField] private List<VariableItem> _variableItems;
    [SerializeField] private List<int> _randomRange = new List<int>();
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

    private void SpawnItems()
    {
        for (int i = 0; i < _variableItems.Count; i++)
        {
            GameObject tempItem = Instantiate(_variableItems[i].objectPrefab, _itemsPos[i], Quaternion.identity);
            tempItem.GetComponent<VariableItemSpawn>().Spawn(_variableItems[i]);
        }
    }
}
