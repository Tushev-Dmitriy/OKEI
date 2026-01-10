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
