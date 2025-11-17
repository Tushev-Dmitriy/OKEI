using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public PlayerData player;
    public List<InventoryData> inventory;
    public SettingsData settings;
    public SaveInfoData saveInfo;
}

[Serializable]
public class PlayerData
{
    public int level;
    public Vector3Data position;
    public Vector3Data rotation;
}

[Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class InventoryData
{
    public string itemID;
    public int stack;
}

[Serializable]
public class SettingsData
{
    public float musicVolume;
    public float sfxVolume;
}

[Serializable]
public class SaveInfoData
{
    public string saveVersion;
    public string lastSaveTime;
}
