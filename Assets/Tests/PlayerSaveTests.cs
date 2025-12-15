using NUnit.Framework;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class PlayerSaveTests
{
    [Test]
    public void SaveFile_IsCreated()
    {
        SaveData data = new SaveData();
        data.player = new PlayerData()
        {
            level = "TestLevel",
            position = new Vector3Data() { x = 1, y = 2, z = 3 },
            rotation = new Vector3Data() { x = 0, y = 90, z = 0 }
        };

        PlayerSaveSystem.Save(data);

        string path = Path.Combine(Application.persistentDataPath, "player.json");

        Assert.IsTrue(File.Exists(path),
            "ОШИБКА: файл player.json не был создан системой сохранения");
    }

    [Test]
    public void LoadFile_DataIsCorrect()
    {
        SaveData data = new SaveData();
        data.player = new PlayerData()
        {
            level = "TestLevel",
            position = new Vector3Data() { x = 5, y = 5, z = 5 },
            rotation = new Vector3Data() { x = 10, y = 20, z = 30 }
        };

        PlayerSaveSystem.Save(data);

        PlayerSaveSystem.Load(out SaveData loaded);

        Assert.NotNull(loaded, "ОШИБКА: сохранённые данные не были загружены");
        Assert.AreEqual(5, loaded.player.position.x,
            "ОШИБКА: позиция игрока (X) восстановлена неверно");
        Assert.AreEqual("TestLevel", loaded.player.level,
            "ОШИБКА: название уровня восстановлено неверно");
    }

    [Test]
    public void ConvertJToken_WorksCorrectly()
    {
        JObject obj = JObject.Parse("{\"value\": 10}");

        var result = InventorySaveSystem_TestAccess.ConvertJToken_Public(obj)
                     as Dictionary<string, object>;

        Assert.NotNull(result,
            "ОШИБКА: преобразование JToken - Dictionary вернуло null");
        Assert.AreEqual(10L, result["value"],
            "ОШИБКА: JValue было преобразовано неверно");
    }
}

public static class InventorySaveSystem_TestAccess
{
    public static object ConvertJToken_Public(object token)
    {
        var method = typeof(InventorySaveSystem)
            .GetMethod(
                "ConvertJToken",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);

        Assert.NotNull(method,
            "ОШИБКА: приватный метод ConvertJToken не найден через Reflection");

        return method.Invoke(null, new[] { token });
    }
}
