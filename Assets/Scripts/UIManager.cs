using UnityEngine;
using UnityEngine.UI;

// This script runs all the screens of the safari.
// Start screen, HUD, passport, hint line, warning line, toast, fact card and end screen.
// I put it all in one script so the gameplay scripts never need a reference to the UI.
// The UI only listens to GameManager, and nothing else in the game changes a screen.
public class UIManager : MonoBehaviour
{
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
    [Tooltip("Animals in stamp slot order. Index 0 is the first slot.")]
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
        // There should only be one of me in the scene. If there are two, I remove the extra one.
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

        // The player tells me when it hits something, and I turn that into a toast.
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

    // I take every subscription back here.
    // If I did not, Replay would leave the old listeners behind and Refresh would run twice for every change.
    private void OnDisable()
    {
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
        HideMessages();

        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave;

        // The sticks are for phones and tablets only.
        // I cannot test for a touchscreen, because desktop browsers and touch laptops report one too.
        Show(mobileControls, Application.isMobilePlatform);

        // I switch the player off until a run starts, so I cannot walk around on the start screen.
        if (player != null)
            player.enabled = false;

        Refresh();
    }

    private void Update()
    {
        // I use a timestamp instead of a coroutine, so nothing keeps running if the toast is hidden early.
        if (toastRoot != null && toastRoot.activeSelf && Time.time >= toastHideTime)
            Show(toastRoot, false);
    }

    public void ShowHint(string message)
    {
        SetText(hintText, message);
        Show(hintRoot, true);
    }

    public void HideHint()
    {
        Show(hintRoot, false);
    }

    public void ShowWarning(string message)
    {
        SetText(warningText, message);
        Show(warningRoot, true);
    }

    public void HideWarning()
    {
        Show(warningRoot, false);
    }

    public void ShowToast(string message)
    {
        SetText(toastText, message);
        Show(toastRoot, true);
        toastHideTime = Time.time + toastSeconds;
    }

    // firstScan tells the card to say New stamp collected, or to say the animal is already in the passport.
    public void ShowFactCard(AnimalData data, bool firstScan)
    {
        if (data == null)
            return;

        SetText(factTitleText, data.DisplayName);
        SetText(factHabitatText, "Habitat: " + data.Habitat);
        SetText(factStatusText, "Status: " + data.Status);
        SetText(factBodyText, data.Fact);
        SetText(factJobText, "Job supported: " + data.JobRole + "\n" + data.JobDescription);

        // The stamp line is the only part of the card that changes between a first scan and a repeat scan.
        string stampLine = "Already in your passport";
        if (firstScan)
            stampLine = "New stamp collected! Jobs Supported +1";
        SetText(factStampText, stampLine);

        Show(factCard, true);
        HideHint();

        // The last scan wins the safari before this card opens, so I push the end screen back
        // until the player has read the last fact.
        if (RunIsWon())
            Show(endPanel, false);
    }

    public void HideFactCard()
    {
        Show(factCard, false);

        // Closing the last fact card of a finished safari brings up the end screen.
        if (RunIsWon())
            ShowEnd();
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
        // I put the player back at the gate before I clear the state, so the guide line
        // starts from the spawn point again.
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
        HideMessages();

        Refresh();

        // A save restored at 5 of 5 is a finished safari, so I go straight to the end screen.
        if (RunIsWon())
            ShowEnd();
    }

    private void Refresh()
    {
        GameManager game = GameManager.Instance;
        int jobs = game != null ? game.Jobs : 0;
        int score = game != null ? game.Score : 0;
        int stamps = game != null ? game.Stamps : 0;
        int total = game != null ? game.TotalAnimals : 0;

        SetText(jobsText, "Jobs Supported: " + jobs);
        SetText(scoreText, "Score: " + score);
        SetText(stampsText, "Stamps: " + stamps + " / " + total);

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

            // I check each array against the slot, because I may wire fewer icons or labels than slots.
            if (stampIcons != null && i < stampIcons.Length && stampIcons[i] != null)
                stampIcons[i].color = stamped ? stampedColor : emptyColor;

            if (stampLabels != null && i < stampLabels.Length)
                SetText(stampLabels[i], slot.DisplayName);
        }
    }

    // True when an animal of this species has been stamped in the current run.
    private static bool IsScanned(AnimalData data)
    {
        foreach (Animal animal in Animal.All)
        {
            if (animal.Data == data && animal.Scanned)
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

        // The reserve holds five animals, and I only use the 5 when they have not spawned yet.
        int total = game != null && game.TotalAnimals > 0 ? game.TotalAnimals : 5;

        // 100 is startingScore, so it is also the best score the safari can end on.
        string summary = "Safari complete: " + stamps + " of " + total + " stamps\n";
        summary += "Score: " + score + " / 100  (breaches: " + breaches + ")\n";
        summary += "Jobs supported: " + jobs;

        // A clean safari says so on the end screen.
        if (breaches == 0)
            summary += "\nPerfect run. Zero breaches.";

        SetText(endSummaryText, summary);
    }

    private void HandleBump(Collider hit)
    {
        // The collider is on the Body child, so I look up for the animal on the parent.
        Animal animal = hit.GetComponentInParent<Animal>();

        if (animal != null && animal.Data != null)
            ShowToast("Blocked by the " + animal.Data.DisplayName);
        else
            ShowToast("Blocked by a rock or tree");
    }

    // Hides the hint, warning and toast together.
    private void HideMessages()
    {
        Show(hintRoot, false);
        Show(warningRoot, false);
        Show(toastRoot, false);
    }

    // True once the run is won. This is when the end screen takes over.
    private static bool RunIsWon()
    {
        return GameManager.Instance != null && GameManager.Instance.HasWon;
    }

    // A panel I did not wire is skipped, so this never throws.
    private static void Show(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    // A label I did not wire is skipped, so this never throws.
    private static void SetText(Text label, string value)
    {
        if (label != null)
            label.text = value;
    }
}
