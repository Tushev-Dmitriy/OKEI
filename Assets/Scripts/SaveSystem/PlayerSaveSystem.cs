using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class PlayerSaveSystem
{
    private static string savePath => 
        Path.Combine(Application.persistentDataPath, "player.json");

    public static void Save(SaveData saveData)
    {
        string json = JsonConvert.SerializeObject(saveData, 
            Formatting.Indented);
        File.WriteAllText(savePath, json);

    }

    public static void Load(out SaveData saveData)
    {
        saveData = null;

        if (!File.Exists(savePath))
        {
            return;
        }

        string json = File.ReadAllText(savePath);
        saveData = JsonConvert.DeserializeObject<SaveData>(json);

    }
}

