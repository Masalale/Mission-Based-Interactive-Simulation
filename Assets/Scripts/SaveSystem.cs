using System;
using System.Collections.Generic;
using UnityEngine;

// JsonUtility cannot read dictionaries, so the save uses a plain List.
[Serializable]
public class SaveData
{
    public List<string> scannedIds = new List<string>();
    public int score = 100;
    public int breaches;
}

// PlayerPrefs, because WebGL has no writable folder.
public static class SaveSystem
{
    private const string Key = "SafariSave";

    public static bool HasSave => PlayerPrefs.HasKey(Key);

    public static void Save(SaveData data)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));

        // WebGL keeps the save in memory until Save, so a closed tab would lose the run.
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;

        string json = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        // An old save that does not parse must not crash the game.
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception error)
        {
            Debug.LogWarning($"{nameof(SaveSystem)} could not read the save: {error.Message}");
        }

        return data != null;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
