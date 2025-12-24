using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSaver : MonoBehaviour
{
    private GameObject _player;
    private SaveData _cachedSaveData;

    public string CurrentLevelName => SceneManager.GetActiveScene().name;

    private void Awake()
    {
        _player = gameObject;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        LoadPlayerData();
        StartCoroutine(AutoSaveCoroutine());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);
            SavePlayerData();
        }
    }

    public void SavePlayerData()
    {
        if (_player == null)
        {
            Debug.LogError("Player reference is missing");
            return;
        }

        SaveData data = new SaveData
        {
            player = new PlayerData
            {
                level = CurrentLevelName,
                position = new Vector3Data
                {
                    x = _player.transform.position.x,
                    y = _player.transform.position.y,
                    z = _player.transform.position.z
                },
                rotation = new Vector3Data
                {
                    x = _player.transform.eulerAngles.x,
                    y = _player.transform.eulerAngles.y,
                    z = _player.transform.eulerAngles.z
                }
            },

            settings = new SettingsData
            {
                musicVolume = 0.5f,
                sfxVolume = 0.5f
            },

            saveInfo = new SaveInfoData
            {
                saveVersion = "1.0",
                lastSaveTime = System.DateTime.Now.ToString()
            },

            sceneObjects = CaptureSceneObjects()
        };

        PlayerSaveSystem.Save(data);
        Debug.Log("PLAYER DATA SAVED");
    }

    private List<SceneObjectStateData> CaptureSceneObjects()
    {
        var saveables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<ISceneSaveable>();

        List<SceneObjectStateData> result = new();

        foreach (var saveable in saveables)
        {
            result.Add(saveable.CaptureState());
        }

        return result;
    }

    public void LoadPlayerData()
    {
        PlayerSaveSystem.Load(out SaveData data);

        if (data == null)
        {
            Debug.LogWarning("No save data found");
            return;
        }

        _cachedSaveData = data;

        if (data.player.level != CurrentLevelName)
        {
            Debug.Log("Loading saved level: " + data.player.level);
            SceneManager.LoadScene(data.player.level);
            return;
        }

        RestorePlayerTransform(data);
        RestoreSceneObjects(data);

        Debug.Log("PLAYER DATA LOADED");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_cachedSaveData == null)
            return;

        RestorePlayerTransform(_cachedSaveData);
        RestoreSceneObjects(_cachedSaveData);

        Debug.Log("SCENE OBJECT STATES RESTORED");
    }

    private void RestorePlayerTransform(SaveData data)
    {
        _player.transform.position = new Vector3(
            data.player.position.x,
            data.player.position.y,
            data.player.position.z
        );

        _player.transform.eulerAngles = new Vector3(
            data.player.rotation.x,
            data.player.rotation.y,
            data.player.rotation.z
        );
    }

    private void RestoreSceneObjects(SaveData data)
    {
        if (data.sceneObjects == null || data.sceneObjects.Count == 0)
            return;

        var saveables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<ISceneSaveable>();

        foreach (var saveable in saveables)
        {
            var state = data.sceneObjects
                .FirstOrDefault(x => x.id == saveable.SaveId);

            if (state != null)
                saveable.RestoreState(state);
        }
    }
}
