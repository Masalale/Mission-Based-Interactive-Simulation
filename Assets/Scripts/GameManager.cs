using System;
using UnityEngine;

// This keeps the score of the safari: the stamps, the score, the breaches and the jobs.
// It holds the numbers only. There is no UI and no input in here.
// When something changes I raise an event and the other scripts decide what to do.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scoring")]
    [Tooltip("Score a fresh safari starts with, and the top score.")]
    [SerializeField] private int startingScore = 100;
    [Tooltip("Points taken per danger zone breach.")]
    [SerializeField] private int breachPenalty = 10;

    public int Stamps { get; private set; }
    public int Score { get; private set; }
    public int Breaches { get; private set; }
    public int Jobs { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool HasWon { get; private set; }
    public int TotalAnimals => Animal.All.Count;

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
        Jobs++;
        Save();

        OnStateChanged?.Invoke();

        // A full passport wins the safari, so I tell the UI once.
        if (TotalAnimals > 0 && Stamps >= TotalAnimals)
        {
            HasWon = true;
            OnWin?.Invoke();
        }
    }

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
        Jobs = 0;
        Breaches = 0;
        Score = startingScore;
        HasWon = false;
        IsPlaying = true;

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
        Jobs = restored;
        Score = data.score;
        Breaches = data.breaches;
        IsPlaying = true;
        HasWon = TotalAnimals > 0 && Stamps >= TotalAnimals;

        OnStateChanged?.Invoke();

        if (HasWon)
            OnWin?.Invoke();

        return true;
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
