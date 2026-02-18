using DevionGames.InventorySystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnVariableItems : MonoBehaviour
{
    [SerializeField] private List<GameObject> _variableItems;
    [SerializeField] private List<int> _randomRange = new List<int>();

    private readonly List<SpawnEntry> _spawnEntries = new List<SpawnEntry>();
    private List<Vector3> _itemsPos = new List<Vector3>();

    private struct SpawnEntry
    {
        public GameObject prefab;
        public string saveId;
    }

    private void Awake()
    {
        BuildSpawnEntries();
        SetRandomPosToSpawn();
    }

    private void Start()
    {
        SpawnItems();
    }

    private void SetRandomPosToSpawn()
    {
        for (int i = 0; i < _spawnEntries.Count; i++)
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

    private void BuildSpawnEntries()
    {
        _spawnEntries.Clear();

        for (int i = 0; i < _variableItems.Count; i++)
        {
            var prefab = _variableItems[i];
            if (prefab == null)
            {
                continue;
            }

            string saveId = BuildSpawnSaveId(i, prefab);
            if (VariableItemSaveSystem.IsCollected(saveId))
            {
                continue;
            }

            _spawnEntries.Add(new SpawnEntry
            {
                prefab = prefab,
                saveId = saveId
            });
        }
    }

    private void SpawnItems()
    {
        for (int i = 0; i < _spawnEntries.Count; i++)
        {
            var entry = _spawnEntries[i];
            var instance = Instantiate(entry.prefab, _itemsPos[i], Quaternion.identity);

            var variableSpawn = instance.GetComponent<VariableItemSpawn>();
            if (variableSpawn == null)
            {
                continue;
            }

            variableSpawn.ConfigureSaveId(entry.saveId);
        }
    }

    private string BuildSpawnSaveId(int index, GameObject prefab)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string spawnerName = gameObject.name;
        string prefabName = prefab != null ? prefab.name : "null";
        return $"{sceneName}::SpawnVariableItems::{spawnerName}::{index}::{prefabName}";
    }
}
