using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The saved safari: which animals are stamped, the score and the breach count.
/// Plain fields and List&lt;string&gt; because JsonUtility cannot serialise
/// dictionaries or anything else clever.
/// </summary>
[System.Serializable]
public class SaveData
{
    public List<string> scannedIds = new List<string>();
    public int score = 100;
    public int breaches;
}

/// <summary>
/// Save and load through PlayerPrefs and JsonUtility. PlayerPrefs works on
/// every target including WebGL, which has no writable filesystem. Static
/// because only one save exists and no scene object needs to own it.
/// </summary>
public static class SaveSystem
{
    private const string Key = "SafariSave";

    /// <summary>True when a previous safari was saved and Continue can be pressed.</summary>
    public static bool HasSave => PlayerPrefs.HasKey(Key);

    /// <summary>Writes the given state to PlayerPrefs and flushes it to disk.</summary>
    public static void Save(SaveData data)
    {
        if (data == null)
            return;

        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        // WebGL keeps PlayerPrefs in memory until this flush, so a tab close
        // without it would lose the run.
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Reads the save. Returns false when there is no key, the string is empty,
    /// or the JSON cannot be parsed, so callers can fall back to a new run.
    /// </summary>
    public static bool TryLoad(out SaveData data)
    {
        data = null;

        if (!PlayerPrefs.HasKey(Key))
            return false;

        string json = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        // A corrupted PlayerPrefs entry can come from an older build, so parsing
        // is guarded instead of throwing out of a load.
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception error)
        {
            Debug.LogWarning($"{nameof(SaveSystem)} could not read the save: {error.Message}");
            data = null;
        }

        return data != null;
    }

    /// <summary>Deletes the save. Used by ResetRun so a new safari starts clean.</summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}