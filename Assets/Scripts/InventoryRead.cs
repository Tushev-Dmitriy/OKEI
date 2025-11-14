using System.Collections;
using UnityEngine;

public class InventoryRead : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(TestCor());
    }

    IEnumerator TestCor()
    {
        yield return new WaitForSeconds(12);
        var inventory = PlayerPrefs.GetString("Level1");
        Debug.Log(inventory);
    }
}
