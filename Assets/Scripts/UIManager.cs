using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Every screen in the safari: start screen, HUD counters, the five passport
/// stamps, hint, warning, toast and the fact card. It only listens to
/// GameManager, so the gameplay scripts never touch a UI reference directly.
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>Singleton access for the gameplay scripts.</summary>
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private GameObject factCard;
    [SerializeField] private GameObject mobileControls;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newSafariButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button factCardCloseButton;

    [Header("Passport")]
    [Tooltip("Animals in stamp slot order, index 0 is the first slot.")]
    [SerializeField] private AnimalData[] passportOrder;
    [SerializeField] private Image[] stampIcons;
    [SerializeField] private Text[] stampLabels;
    [SerializeField] private Color stampedColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("HUD text")]
    [SerializeField] private Text jobsText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text stampsText;

    [Header("Messages")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private Text hintText;
    [SerializeField] private GameObject warningRoot;
    [SerializeField] private Text warningText;
    [SerializeField] private GameObject toastRoot;
    [SerializeField] private Text toastText;
    [Tooltip("How long a toast stays on screen.")]
    [SerializeField] private float toastSeconds = 2f;

    [Header("Fact card")]
    [SerializeField] private Text factTitleText;
    [SerializeField] private Text factHabitatText;
    [SerializeField] private Text factStatusText;
    [SerializeField] private Text factBodyText;
    [SerializeField] private Text factJobText;
    [SerializeField] private Text factStampText;

    [Header("End screen")]
    [SerializeField] private Text endSummaryText;

    [Header("References")]
    [SerializeField] private PlayerController player;

    private float toastHideTime;

    private void Awake()
    {
        // Two managers would both answer ShowToast and fight over the panels.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += Refresh;
            GameManager.Instance.OnWin += ShowEnd;
        }

        if (player != null)
            player.Bumped += HandleBump;

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);

        if (newSafariButton != null)
            newSafariButton.onClick.AddListener(OnNewSafari);

        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplay);

        if (factCardCloseButton != null)
            factCardCloseButton.onClick.AddListener(HideFactCard);
    }

    private void OnDisable()
    {
        // Every subscription is undone here, otherwise pressing Replay would
        // leave the old listeners behind and Refresh would run twice per change.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= Refresh;
            GameManager.Instance.OnWin -= ShowEnd;
        }

        if (player != null)
            player.Bumped -= HandleBump;

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinue);

        if (newSafariButton != null)
            newSafariButton.onClick.RemoveListener(OnNewSafari);

        if (replayButton != null)
            replayButton.onClick.RemoveListener(OnReplay);

        if (factCardCloseButton != null)
            factCardCloseButton.onClick.RemoveListener(HideFactCard);
    }

    private void Start()
    {
        Show(startPanel, true);
        Show(hudPanel, false);
        Show(endPanel, false);
        Show(factCard, false);
        Show(hintRoot, false);
        Show(warningRoot, false);
        HideToast();

        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave;

        // New Input System check: the on-screen sticks only make sense where a
        // touchscreen exists. On desktop the mouse and keyboard stay in charge.
        Show(mobileControls, Touchscreen.current != null);

        // Freeze the player until a run starts, so the start screen cannot be
        // walked around before Continue or New Safari is pressed.
        if (player != null)
            player.enabled = false;

        Refresh();
    }

    private void Update()
    {
        // A timestamp instead of a coroutine: no allocation, and it cannot be
        // left running if the panel is hidden early.
        if (toastRoot != null && toastRoot.activeSelf && Time.time >= toastHideTime)
            HideToast();
    }

    /// <summary>Shows the hint line, e.g. the viewing zone prompt.</summary>
    public void ShowHint(string message)
    {
        if (hintText != null)
            hintText.text = message;

        Show(hintRoot, true);
    }

    /// <summary>Hides the hint line.</summary>
    public void HideHint()
    {
        Show(hintRoot, false);
    }

    /// <summary>Shows the danger zone warning.</summary>
    public void ShowWarning(string message)
    {
        if (warningText != null)
            warningText.text = message;

        Show(warningRoot, true);
    }

    /// <summary>Hides the danger zone warning.</summary>
    public void HideWarning()
    {
        Show(warningRoot, false);
    }

    /// <summary>Shows a short message that hides itself after toastSeconds.</summary>
    public void ShowToast(string message)
    {
        if (toastText != null)
            toastText.text = message;

        Show(toastRoot, true);
        toastHideTime = Time.time + toastSeconds;
    }

    /// <summary>
    /// Opens the fact card for one animal. firstScan decides whether the card
    /// celebrates a new stamp or says the animal is already in the passport.
    /// </summary>
    public void ShowFactCard(AnimalData data, bool firstScan)
    {
        if (data == null)
            return;

        if (factTitleText != null)
            factTitleText.text = data.DisplayName;

        if (factHabitatText != null)
            factHabitatText.text = "Habitat: " + data.Habitat;

        if (factStatusText != null)
            factStatusText.text = "Status: " + data.Status;

        if (factBodyText != null)
            factBodyText.text = data.Fact;

        if (factJobText != null)
            factJobText.text = "Job supported: " + data.JobRole + "\n" + data.JobDescription;

        if (factStampText != null)
            factStampText.text = firstScan
                ? "New stamp collected! Jobs Supported +1"
                : "Already in your passport";

        Show(factCard, true);
        HideHint();
    }

    /// <summary>Closes the fact card, for the close button.</summary>
    public void HideFactCard()
    {
        Show(factCard, false);
    }

    private void OnContinue()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.Restore();
        BeginPlay();
    }

    private void OnNewSafari()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.ResetRun();
        BeginPlay();
    }

    private void OnReplay()
    {
        // Replay starts the player back at the gate before the state is reset,
        // so the guide line draws from the spawn point.
        if (player != null)
            player.ResetToSpawn();

        if (GameManager.Instance != null)
            GameManager.Instance.ResetRun();

        BeginPlay();
    }

    private void BeginPlay()
    {
        Show(startPanel, false);
        Show(endPanel, false);
        Show(factCard, false);
        Show(hudPanel, true);

        if (player != null)
            player.enabled = true;

        HideHint();
        HideWarning();
        HideToast();

        Refresh();

        // A save restored at 5 of 5 is a finished safari, so go straight to the end screen.
        if (GameManager.Instance != null && GameManager.Instance.HasWon)
            ShowEnd();
    }

    private void Refresh()
    {
        GameManager game = GameManager.Instance;
        int jobs = game != null ? game.Jobs : 0;
        int score = game != null ? game.Score : 0;
        int stamps = game != null ? game.Stamps : 0;
        int total = game != null ? game.TotalAnimals : 0;

        if (jobsText != null)
            jobsText.text = "Jobs Supported: " + jobs;

        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (stampsText != null)
            stampsText.text = "Stamps: " + stamps + " / " + total;

        RefreshPassport();
    }

    private void RefreshPassport()
    {
        if (passportOrder == null)
            return;

        for (int i = 0; i < passportOrder.Length; i++)
        {
            AnimalData slot = passportOrder[i];

            if (slot == null)
                continue;

            bool stamped = IsScanned(slot);

            // Every array is bounds checked: the editor may wire fewer icons
            // than passport slots and that must not break the HUD.
            if (stampIcons != null && i < stampIcons.Length && stampIcons[i] != null)
                stampIcons[i].color = stamped ? stampedColor : emptyColor;

            if (stampLabels != null && i < stampLabels.Length && stampLabels[i] != null)
                stampLabels[i].text = slot.DisplayName;
        }
    }

    private static bool IsScanned(AnimalData data)
    {
        for (int i = 0; i < Animal.All.Count; i++)
        {
            Animal animal = Animal.All[i];

            if (animal != null && animal.Data == data && animal.Scanned)
                return true;
        }

        return false;
    }

    private void ShowEnd()
    {
        Show(endPanel, true);
        Show(factCard, false);
        HideHint();
        HideWarning();

        if (player != null)
            player.enabled = false;

        GameManager game = GameManager.Instance;
        int stamps = game != null ? game.Stamps : 0;
        int score = game != null ? game.Score : 0;
        int breaches = game != null ? game.Breaches : 0;
        int jobs = game != null ? game.Jobs : 0;

        // The reserve holds five animals; the fallback keeps the line readable
        // if the end screen somehow opens before they spawned.
        int total = game != null && game.TotalAnimals > 0 ? game.TotalAnimals : 5;

        if (endSummaryText == null)
            return;

        // 100 is startingScore, so it is also the top score a run can reach.
        string summary = "Safari complete: " + stamps + " of " + total + " stamps\n";
        summary += "Score: " + score + " / 100  (breaches: " + breaches + ")\n";
        summary += "Jobs supported: " + jobs;

        if (breaches == 0)
            summary += "\nPerfect run. Zero breaches.";

        endSummaryText.text = summary;
    }

    private void HandleBump(Collider hit)
    {
        if (hit == null)
            return;

        Animal animal = hit.GetComponentInParent<Animal>();

        if (animal != null && animal.Data != null)
            ShowToast("Blocked by the " + animal.Data.DisplayName);
        else
            ShowToast("Blocked by a rock or tree");
    }

    private void HideToast()
    {
        Show(toastRoot, false);
    }

    // One null-guarded SetActive helper: an unwired panel must never throw.
    private static void Show(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }
}