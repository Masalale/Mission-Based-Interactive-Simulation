using System;
using UnityEngine;

// The one source of truth for a safari: stamps, score, breaches and jobs.
// It holds state only, no UI and no input. It tells the other scripts about changes through events.
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

    public event Action OnStateChanged;
    public event Action<Animal> OnStampAdded;
    public event Action<Animal> OnBreach;
    public event Action OnWin;

    private void Awake()
    {
        // A second GameManager would fight over the singleton, so the copy destroys itself.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Runs before UIManager.Start, so the first refresh shows the starting score.
        Score = startingScore;
        OnStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Collects a stamp. AnimalScanner is the only caller, after a raycast hit inside a viewing zone.
    public void AddStamp(Animal animal)
    {
        if (animal == null || animal.Scanned || !IsPlaying)
            return;

        animal.MarkScanned();
        Stamps++;
        Jobs++;
        Save();

        OnStampAdded?.Invoke(animal);
        OnStateChanged?.Invoke();

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
        if (animal != null)
            animal.RegisterBreach();

        Score = Mathf.Max(0, Score - breachPenalty);
        Save();

        OnBreach?.Invoke(animal);
        OnStateChanged?.Invoke();
    }

    // Starts a brand new safari: clears the animals, the save and the score.
    public void ResetRun()
    {
        SaveSystem.Clear();

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

    // Rebuilds the run from the save. The state is set directly, not through AddStamp,
    // so loading never awards a stamp twice and never raises OnStampAdded.
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

    // Builds the save from the animals themselves, then writes it to PlayerPrefs.
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
