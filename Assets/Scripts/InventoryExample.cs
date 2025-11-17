using DevionGames.InventorySystem;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventoryExample : MonoBehaviour
{
    public ItemCollection playerInventory;

    private void Start()
    {
        // Загружаем инвентарь при старте
        InventorySaveSystem.LoadInventory(playerInventory);
        StartCoroutine(TestCor());
    }

    IEnumerator TestCor()
    {
        yield return new WaitForSeconds(10f);
        SaveButtonPressed();
    }

    public void SaveButtonPressed()
    {
        // Сохраняем инвентарь по кнопке
        InventorySaveSystem.SaveInventory(playerInventory);
    }
}
