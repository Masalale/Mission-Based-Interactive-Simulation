using System;
using UnityEngine;

// This keeps the score of the safari: the stamps, the Respect score and the disturbances.
// It holds the numbers only. There is no UI and no input in here.
// When something changes I raise an event and the other scripts decide what to do.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scoring")]
    [Tooltip("Respect a fresh safari starts with, and the top Respect score.")]
    [SerializeField] private int startingScore = 100;
    [Tooltip("Respect taken per disturbance, so the player has to be taught the lesson.")]
    [SerializeField] private int breachPenalty = 10;

    public int Stamps { get; private set; }
    public int Score { get; private set; }
    public int Breaches { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool HasWon { get; private set; }
    public int TotalAnimals => Animal.All.Count;

    // The pause menu sets this, so PlayerController and AnimalScanner can stop listening to the player.
    public bool IsPaused { get; private set; }

    // OnStateChanged goes off whenever a number changes, so the UI can refresh.
    public event Action OnStateChanged;

    // OnWin goes off once, on the last stamp of the safari.
    public event Action OnWin;

    private void Awake()
    {
        // There should only be one of me in the scene. If there are two, I remove the extra one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // I run before UIManager, so the score is set before the first refresh of the UI.
        Score = startingScore;
        OnStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Give an animal a stamp. Only AnimalScanner calls this, and only after it hits an animal
    // with the scan ray while the player is inside the viewing zone.
    public void AddStamp(Animal animal)
    {
        if (animal == null || animal.Scanned || !IsPlaying)
            return;

        animal.MarkScanned();
        Stamps++;
        Save();

        OnStateChanged?.Invoke();

        // A full passport wins the safari, so I tell the UI once.
        if (TotalAnimals > 0 && Stamps >= TotalAnimals)
        {
            HasWon = true;
            OnWin?.Invoke();
        }
    }

    // A disturbance is a breach of the red danger zone. It costs the visitor Respect.
    public void RegisterBreach(Animal animal)
    {
        if (!IsPlaying)
            return;

        Breaches++;

        // The score stops at 0 and never turns negative.
        Score = Mathf.Max(0, Score - breachPenalty);
        Save();

        OnStateChanged?.Invoke();
    }

    // Throw the old safari away and start a fresh one.
    public void ResetRun()
    {
        SaveSystem.Clear();

        // Every animal goes back to unscanned, then I zero the counters by hand.
        foreach (Animal animal in Animal.All)
            animal.ResetState();

        Stamps = 0;
        Breaches = 0;
        Score = startingScore;
        HasWon = false;
        IsPlaying = true;

        // A fresh safari is never paused.
        SetPaused(false);

        OnStateChanged?.Invoke();
    }

    // Put the safari back the way the save says it was.
    // I set the numbers straight into the fields instead of going through AddStamp,
    // because that would hand out the same stamp a second time.
    public bool Restore()
    {
        if (!SaveSystem.TryLoad(out SaveData data))
        {
            ResetRun();
            return false;
        }

        int restored = 0;

        foreach (Animal animal in Animal.All)
        {
            // Clear this animal first, so an id that is not in the save is not left stamped.
            animal.ResetState();

            if (animal.Data != null && data.scannedIds.Contains(animal.Data.Id))
            {
                animal.MarkScanned();
                restored++;
            }
        }

        Stamps = restored;
        Score = data.score;
        Breaches = data.breaches;
        IsPlaying = true;
        HasWon = TotalAnimals > 0 && Stamps >= TotalAnimals;

        SetPaused(false);

        OnStateChanged?.Invoke();

        if (HasWon)
            OnWin?.Invoke();

        return true;
    }

    // Pause the whole safari without losing it. The pause menu shows the panel,
    // I stop the clock and the sound, so nothing moves while the player reads.
    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }

    // Main Menu keeps the save, so Continue Safari is still on the start panel afterwards.
    public void LeaveRun()
    {
        IsPlaying = false;
        SetPaused(false);
        OnStateChanged?.Invoke();
    }

    // Build the save from the animals that are stamped at the moment, then write it to PlayerPrefs.
    private void Save()
    {
        SaveData data = new SaveData();

        foreach (Animal animal in Animal.All)
        {
            if (animal.Scanned && animal.Data != null)
                data.scannedIds.Add(animal.Data.Id);
        }

        data.score = Score;
        data.breaches = Breaches;
        SaveSystem.Save(data);
    }
}
