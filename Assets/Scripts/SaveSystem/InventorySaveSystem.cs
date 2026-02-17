using DevionGames.InventorySystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class InventorySaveSystem
{
    private static string savePath => Path.Combine(Application.persistentDataPath, "inventory.json");

    public static void SaveInventory(ItemCollection collection)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        collection.GetObjectData(data);

        string json = JsonConvert.SerializeObject(
            data,
            Formatting.Indented
        );

        File.WriteAllText(savePath, json);
    }

    public static void LoadInventory(ItemCollection collection)
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);

        var raw = JsonConvert.DeserializeObject(json);

        var data = ConvertJToken(raw) as Dictionary<string, object>;

        collection.SetObjectData(data);
    }

    public static bool TryGetSavedItemNames(out HashSet<string> itemNames)
    {
        itemNames = new HashSet<string>();

        if (!File.Exists(savePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            var root = JToken.Parse(json);
            var items = root["Items"] as JArray;
            if (items == null)
            {
                return true;
            }

            foreach (var item in items)
            {
                var nameToken = item?["Name"];
                if (nameToken == null)
                {
                    continue;
                }

                string name = nameToken.Value<string>();
                if (!string.IsNullOrEmpty(name))
                {
                    itemNames.Add(name);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[InventorySaveSystem] Failed to read saved items: {ex.Message}");
            return false;
        }

        return true;
    }

    public static void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }

    private static object ConvertJToken(object token)
    {
        if (token is JObject obj)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in obj.Properties())
            {
                dict[prop.Name] = ConvertJToken(prop.Value);
            }
            return dict;
        }
        if (token is JArray array)
        {
            var list = new List<object>();
            foreach (var item in array)
            {
                list.Add(ConvertJToken(item));
            }
            return list;
        }
        if (token is JValue val)
        {
            return val.Value;
        }

        return token;
    }

}
