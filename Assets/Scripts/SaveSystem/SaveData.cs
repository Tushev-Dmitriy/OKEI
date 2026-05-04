using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public PlayerData player;
    public List<PlayerLevelData> playerLevels = new List<PlayerLevelData>();
    public SettingsData settings;
    public SaveInfoData saveInfo;
    public RobotProgressData robotProgress;
    public GameplayProgressData gameplayProgress;

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
public class PlayerLevelData
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
    public string sceneName;
    public string id;
    public SceneObjectType type;
    public int state;
    public string json;
}

[Serializable]
public class RobotProgressData
{
    public List<int> unlockedRobotTypes = new List<int>();
}

[Serializable]
public class GameplayProgressData
{
    public int level4ProgressStage;
}

public enum SceneObjectType
{
    Door,
    Lever,
    Bridge,
    Platform,
    VariableItem,
    Artifact,
    Ship
}
