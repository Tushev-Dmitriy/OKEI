using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSaver : MonoBehaviour
{
    private GameObject _player;
    public string CurrentLevelName => SceneManager.GetActiveScene().name;

    private void Start()
    {
        _player = gameObject;
        LoadPlayerData();
        StartCoroutine(SaveDataCor());
    }

    private IEnumerator SaveDataCor()
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

        SaveData data = new SaveData();

        data.player = new PlayerData()
        {
            level = CurrentLevelName,
            position = new Vector3Data()
            {
                x = _player.transform.position.x,
                y = _player.transform.position.y,
                z = _player.transform.position.z
            },
            rotation = new Vector3Data()
            {
                x = _player.transform.eulerAngles.x,
                y = _player.transform.eulerAngles.y,
                z = _player.transform.eulerAngles.z
            }
        };

        data.settings = new SettingsData()
        {
            musicVolume = 0.5f,
            sfxVolume = 0.5f
        };

        data.saveInfo = new SaveInfoData()
        {
            saveVersion = "1.0",
            lastSaveTime = System.DateTime.Now.ToString()
        };

        PlayerSaveSystem.Save(data);

        Debug.Log("PLAYER DATA SAVED");
    }

    public void LoadPlayerData()
    {
        PlayerSaveSystem.Load(out SaveData data);
        if (data == null)
        {
            Debug.LogWarning("No save data!");
            return;
        }

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

        if (data.player.level != CurrentLevelName)
        {
            Debug.Log("Loading saved level: " + data.player.level);
            SceneManager.LoadScene(data.player.level);
            return;
        }

        Debug.Log("PLAYER DATA LOADED");
    }
}
