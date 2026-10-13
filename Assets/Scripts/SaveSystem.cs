using System;
using System.Collections.Generic;
using UnityEngine;

// The saved run. Plain fields and a List, because JsonUtility cannot read dictionaries.
[Serializable]
public class SaveData
{
    public List<string> scannedIds = new List<string>();
    public int score = 100;
    public int breaches;
}

// Saves the run as JSON in PlayerPrefs. PlayerPrefs works on WebGL, which has no file system.
public static class SaveSystem
{
    private const string Key = "SafariSave";

    public static bool HasSave => PlayerPrefs.HasKey(Key);

    public static void Save(SaveData data)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));

        // WebGL keeps PlayerPrefs in memory until Save() flushes it, so a closed tab would lose the run.
        PlayerPrefs.Save();
    }

    // Returns false when there is no save, or when it cannot be read, so the caller starts a new run.
    public static bool TryLoad(out SaveData data)
    {
        data = null;

        // A missing key gives an empty string, so this one check covers both cases.
        string json = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        // An old or damaged save must not throw, so parsing is guarded.
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
