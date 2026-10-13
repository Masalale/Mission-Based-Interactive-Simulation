using System;
using UnityEngine;

/// <summary>
/// The single source of truth for one safari: stamps, score, breaches and jobs.
/// It holds state only, no UI and no input, and broadcasts every change through
/// events so UIManager and SafariGuideLine can react without polling.
/// Runs before everything else so the UI can rely on Instance in OnEnable.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    /// <summary>Singleton access for UIManager, AnimalScanner and the zones.</summary>
    public static GameManager Instance { get; private set; }

    [Header("Scoring")]
    [Tooltip("Score a fresh safari starts with, and the top score.")]
    [SerializeField] private int startingScore = 100;
    [Tooltip("Points taken per danger zone breach.")]
    [SerializeField] private int breachPenalty = 10;

    /// <summary>Animals scanned so far this run.</summary>
    public int Stamps { get; private set; }

    /// <summary>Current score, never below zero.</summary>
    public int Score { get; private set; }

    /// <summary>Total danger zone breaches this run.</summary>
    public int Breaches { get; private set; }

    /// <summary>Jobs supported, one per first scan.</summary>
    public int Jobs { get; private set; }

    /// <summary>False while the start screen is showing.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>True once every animal has been stamped.</summary>
    public bool HasWon { get; private set; }

    /// <summary>How many animals the win condition needs.</summary>
    public int TotalAnimals => Animal.All.Count;

    /// <summary>Raised after any change to the numbers above.</summary>
    public event Action OnStateChanged;

    /// <summary>Raised with the animal that was just stamped.</summary>
    public event Action<Animal> OnStampAdded;

    /// <summary>Raised with the animal whose danger zone was broken.</summary>
    public event Action<Animal> OnBreach;

    /// <summary>Raised once when the last stamp is collected.</summary>
    public event Action OnWin;

    private void Awake()
    {
        // A second GameManager would fight over the singleton and double every
        // score change, so the copy takes its whole GameObject with it.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Start sets the numbers, UIManager starts the run. The start screen is
        // showing at this point, so play has not begun.
        Score = startingScore;
        IsPlaying = false;
        OnStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Collects a passport stamp: marks the animal, adds the job, saves and
    /// checks for the win. The only call site in the whole project is
    /// AnimalScanner, after a raycast hit that is inside a viewing zone, so no
    /// other code can award a stamp.
    /// </summary>
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

    /// <summary>Punishes one danger zone breach and tells everyone about it.</summary>
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

    /// <summary>Starts a brand new safari: clears animals, save file and score.</summary>
    public void ResetRun()
    {
        SaveSystem.Clear();

        for (int i = 0; i < Animal.All.Count; i++)
        {
            Animal animal = Animal.All[i];
            if (animal != null)
                animal.ResetState();
        }

        Stamps = 0;
        Jobs = 0;
        Breaches = 0;
        Score = startingScore;
        HasWon = false;
        IsPlaying = true;

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Rebuilds the run from the save. State is written directly instead of
    /// through AddStamp, so loading never awards a stamp a second time and
    /// never raises OnStampAdded. Returns false when there was nothing to load.
    /// </summary>
    public bool Restore()
    {
        if (!SaveSystem.TryLoad(out SaveData data))
        {
            ResetRun();
            return false;
        }

        int restored = 0;

        for (int i = 0; i < Animal.All.Count; i++)
        {
            Animal animal = Animal.All[i];
            if (animal == null)
                continue;

            animal.ResetState();

            if (animal.Data == null)
                continue;

            if (data.scannedIds.Contains(animal.Data.Id))
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

    /// <summary>Snapshot of the run built from the animals themselves, then written to PlayerPrefs.</summary>
    private void Save()
    {
        SaveData data = new SaveData();

        for (int i = 0; i < Animal.All.Count; i++)
        {
            Animal animal = Animal.All[i];
            if (animal == null || !animal.Scanned || animal.Data == null)
                continue;

            data.scannedIds.Add(animal.Data.Id);
        }

        data.score = Score;
        data.breaches = Breaches;
        SaveSystem.Save(data);
    }
}