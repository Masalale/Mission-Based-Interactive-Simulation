using System;
using System.Collections.Generic;
using UnityEngine;

// This is the saved run.
// JsonUtility only reads plain fields and a List, so I keep it simple and use no dictionary.
[Serializable]
public class SaveData
{
    public List<string> scannedIds = new List<string>();
    public int score = 100;
    public int breaches;
}

// I save the run as JSON in PlayerPrefs.
// I use PlayerPrefs because WebGL has no writable folder, and this works on every platform.
public static class SaveSystem
{
    // One key holds the whole run. If I add a second key I have to keep the two in step.
    private const string Key = "SafariSave";

    public static bool HasSave => PlayerPrefs.HasKey(Key);

    public static void Save(SaveData data)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));

        // WebGL only keeps the save in memory until I call Save, so without this a closed tab loses the run.
        PlayerPrefs.Save();
    }

    // I return false when there is no save or when I cannot read it, so the caller can start a new safari.
    public static bool TryLoad(out SaveData data)
    {
        data = null;

        // A missing key gives back an empty string, so one check covers both cases.
        string json = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        // A save from an older version may not read, and a throw here would kill the game,
        // so I catch it and let the caller start a new safari.
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
