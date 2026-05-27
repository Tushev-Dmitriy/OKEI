using System.IO;
using UnityEngine;

internal static class TestSaveFileUtility
{
    public static string PlayerSavePath => Path.Combine(Application.persistentDataPath, "player.json");
    public static string BootstrapSavePath => Path.Combine(Application.persistentDataPath, "bootstrap_menu.json");

    public static void DeletePlayerSave()
    {
        DeleteIfExists(PlayerSavePath);
    }

    public static void DeleteBootstrapSave()
    {
        DeleteIfExists(BootstrapSavePath);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
