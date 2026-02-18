using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class VariableItemSaveSystem
{
    [Serializable]
    private class VariableItemSaveData
    {
        public List<VariableItemStateEntry> entries = new List<VariableItemStateEntry>();
    }

    [Serializable]
    private class VariableItemStateEntry
    {
        public string id;
        public int state;
    }

    private static readonly Dictionary<string, int> _states = new Dictionary<string, int>();
    private static bool _isLoaded;

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "variable_items.json");

    public static bool IsCollected(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _states.TryGetValue(id, out int state) && state == 1;
    }

    public static void SetCollected(string id, bool collected)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        _states[id] = collected ? 1 : 0;
        Save();
    }

    public static void DeleteSave()
    {
        _states.Clear();
        _isLoaded = true;

        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
    }

    private static void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        _states.Clear();

        if (!File.Exists(SavePath))
        {
            return;
        }

        string json = File.ReadAllText(SavePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        var data = JsonUtility.FromJson<VariableItemSaveData>(json);
        if (data?.entries == null)
        {
            return;
        }

        foreach (var entry in data.entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            _states[entry.id] = entry.state;
        }
    }

    private static void Save()
    {
        var data = new VariableItemSaveData();
        foreach (var pair in _states)
        {
            data.entries.Add(new VariableItemStateEntry
            {
                id = pair.Key,
                state = pair.Value
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }
}
