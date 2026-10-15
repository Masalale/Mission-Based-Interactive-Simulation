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

        OnStateChanged.Invoke();
    }

    public void ResetRun()
    {
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
}
