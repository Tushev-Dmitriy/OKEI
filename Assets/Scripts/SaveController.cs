using UnityEngine;
using System.Collections;

public class SaveController : MonoBehaviour
{
    public float saveInterval = 10f;

    private void Start()
    {
        StartCoroutine(AutoSave());
    }

    private IEnumerator AutoSave()
    {
        while (true)
        {
            SaveGame();
            yield return new WaitForSeconds(saveInterval);
        }
    }

    public void SaveGame()
    {
        InventoryWriter.CollectInventory();
        Debug.Log("[Custom Save] Inventory saved.");
    }
}
