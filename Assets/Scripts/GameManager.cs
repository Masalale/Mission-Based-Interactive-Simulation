using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scoring")]
    [Tooltip("Respect a fresh safari starts with, and the top Respect score.")]
    [SerializeField] private int startingScore = 100;
    [Tooltip("Respect lost per disturbance.")]
    [SerializeField] private int breachPenalty = 10;

    public int Stamps { get; private set; }
    public int Score { get; private set; }
    public int Breaches { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool HasWon { get; private set; }
    public int TotalAnimals => Animal.All.Count;

    public bool IsPaused { get; private set; }

    // Wired up in the Inspector. Fires whenever a number changes.
    public UnityEvent OnStateChanged;

    public UnityEvent OnWin;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Score = startingScore;
        OnStateChanged.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Only AnimalScanner calls this, so stamps are only earned by photos.
    public void AddStamp(Animal animal)
    {
        if (animal == null || animal.Scanned || !IsPlaying)
            return;

        animal.MarkScanned();
        Stamps++;
        Save();

        OnStateChanged.Invoke();

        if (TotalAnimals > 0 && Stamps >= TotalAnimals)
        {
            HasWon = true;
            OnWin.Invoke();
        }
    }

    public void RegisterBreach(Animal animal)
    {
        if (!IsPlaying)
            return;

        Breaches++;

        Score = Mathf.Max(0, Score - breachPenalty);
        Save();

        OnStateChanged.Invoke();
    }

    public void ResetRun()
    {
        SaveSystem.Clear();

        foreach (Animal animal in Animal.All)
            animal.ResetState();

        Stamps = 0;
        Breaches = 0;
        Score = startingScore;
        HasWon = false;
        IsPlaying = true;

        SetPaused(false);

        OnStateChanged.Invoke();
    }

    // Set the fields directly, because AddStamp would hand out the stamp again.
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
            // Clear first, so an animal missing from the save is not left stamped.
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

        OnStateChanged.Invoke();

        if (HasWon)
            OnWin.Invoke();

        return true;
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }

    public void LeaveRun()
    {
        IsPlaying = false;
        SetPaused(false);
        OnStateChanged.Invoke();
    }

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
