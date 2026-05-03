using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

public static class PlayerSaveSystem
{
    private static string savePath =>
        Path.Combine(Application.persistentDataPath, "player.json");

    public static void Save(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? Application.persistentDataPath);
        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
        File.WriteAllText(savePath, json);
    }

    public static void Load(out SaveData saveData)
    {
        saveData = null;

        if (!File.Exists(savePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            saveData = JsonConvert.DeserializeObject<SaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[PlayerSaveSystem] Failed to load save file: {exception.Message}");
            saveData = null;
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }
}
