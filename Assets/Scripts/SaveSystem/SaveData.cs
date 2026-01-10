using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public PlayerData player;
    public SettingsData settings;
    public SaveInfoData saveInfo;

    public List<SceneObjectStateData> sceneObjects;
}

[Serializable]
public class PlayerData
{
    public string level;
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

[Serializable]
public class SceneObjectStateData
{
    public string id;
    public SceneObjectType type;
    public int state;
}

public enum SceneObjectType
{
    Door,
    Lever,
    Bridge,
    Platform
}
