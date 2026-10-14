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

    [Header("Crosshair")]
    [Tooltip("The ring in the middle of the screen.")]
    [SerializeField] private Image crosshair;
    [Tooltip("Small label under the crosshair that names the animal I am aiming at.")]
    [SerializeField] private Text crosshairLabel;

    [Header("Photo and danger feedback")]
    [Tooltip("White full screen image that flashes when a photo is taken.")]
    [SerializeField] private Image photoFlash;
    [Tooltip("Red full screen image that flashes when an animal is disturbed.")]
    [SerializeField] private Image dangerFlash;
    [Tooltip("How fast the photo flash fades. 0.7 alpha is gone in about a quarter of a second.")]
    [SerializeField] private float photoFlashSpeed = 2.8f;
    [Tooltip("How fast the danger flash fades. It is gone in about 0.6 s.")]
    [SerializeField] private float dangerFlashSpeed = 1.6f;

    [Header("Controls tip")]
    [Tooltip("Shown for the first seconds of a safari. There is one line for desktop and one for phones.")]
    [SerializeField] private Text controlsTipDesktop;
    [SerializeField] private Text controlsTipPhone;
    [SerializeField] private float controlsTipSeconds = 8f;

    [Header("Fact card")]
    [SerializeField] private Text factTitleText;
    [SerializeField] private Text factHabitatText;
    [SerializeField] private Text factStatusText;
    [SerializeField] private Text factBodyText;
    [SerializeField] private Text factTipText;
    [SerializeField] private Text factStampText;

    [Header("End screen")]
    [SerializeField] private Text endSummaryText;

    [Header("References")]
    [SerializeField] private PlayerController player;

    private float toastHideTime;
    private float tipHideTime;
    private bool tipShowing;

    private static readonly Color CrosshairIdleColor = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color CrosshairGoodColor = new Color(0.35f, 0.95f, 0.35f, 1f);
    private static readonly Color CrosshairNearColor = new Color(1f, 0.72f, 0.18f, 1f);

    // True while a fact card is on screen. The player cannot move or look then.
    public bool IsFactCardOpen => factCard != null && factCard.activeSelf;

    // True once the safari is over. The pause menu must never open on top of the end screen.
    public bool IsEndScreenOpen => endPanel != null && endPanel.activeSelf;

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
        {
            player.Bumped += HandleBump;
            player.Moved += HideControlsTip;
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);

        if (newSafariButton != null)
            newSafariButton.onClick.AddListener(OnNewSafari);

        if (replayButton != null)
            replayButton.onClick.AddListener(Replay);

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
        {
            player.Bumped -= HandleBump;
            player.Moved -= HideControlsTip;
        }

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinue);

        if (newSafariButton != null)
            newSafariButton.onClick.RemoveListener(OnNewSafari);

        if (replayButton != null)
            replayButton.onClick.RemoveListener(Replay);

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
        HideControlsTip();

        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave;

        // The sticks are for phones and tablets only.
        // I cannot test for a touchscreen, because desktop browsers and touch laptops report one too.
        Show(mobileControls, Application.isMobilePlatform);

        // On a desktop the player closes the card with E or a click, so the button says so.
        SetCloseButtonLabel(Application.isMobilePlatform ? "Close" : "Close (E)");

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

        // The controls tip goes away on its own after a few seconds.
        if (tipShowing && Time.time >= tipHideTime)
            HideControlsTip();

        // Both flashes fade out here, which is one line each instead of two coroutines.
        FadeOut(photoFlash, photoFlashSpeed * Time.unscaledDeltaTime);
        FadeOut(dangerFlash, dangerFlashSpeed * Time.unscaledDeltaTime);
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

    // The crosshair tells the player what aiming at the animal will do right now.
    // A green ring means a good photo is possible, an amber ring means step closer.
    public void SetCrosshair(Color ringColour, string label)
    {
        // The crosshair starts white and is tinted by AnimalScanner when an animal is in the ray.
        if (crosshair != null)
            crosshair.color = ringColour;

        SetText(crosshairLabel, label);
    }

    // The three states of the crosshair, so AnimalScanner only chooses a state.
    public static Color CrosshairNormal => CrosshairIdleColor;
    public static Color CrosshairReady => CrosshairGoodColor;
    public static Color CrosshairTooFar => CrosshairNearColor;

    // A white flash over the whole screen, so a photo feels like a camera going off.
    public void FlashPhoto()
    {
        SetFlash(photoFlash, new Color(1f, 1f, 1f, 0.7f));
    }

    // A red flash over the whole screen, so a disturbance is felt before it is read.
    public void FlashDanger()
    {
        SetFlash(dangerFlash, new Color(0.85f, 0.12f, 0.1f, 0.9f));
    }

    // firstPhoto tells the card to say the passport gained a stamp, or that it is already there.
    public void ShowFactCard(AnimalData data, bool firstPhoto)
    {
        if (data == null)
            return;

        SetText(factTitleText, data.DisplayName);
        SetText(factHabitatText, "Habitat: " + data.Habitat);
        SetText(factStatusText, "Status: " + data.Status);
        SetText(factBodyText, data.Fact);
        SetText(factTipText, "Ranger's tip: " + data.ViewingTip);

        // The stamp line is the only part of the card that changes between a first photo and a repeat photo.
        string stampLine = "Already in your passport.";

        if (firstPhoto)
        {
            int done = 0;
            int total = 5;

            if (GameManager.Instance != null)
            {
                done = GameManager.Instance.Stamps;
                total = GameManager.Instance.TotalAnimals > 0 ? GameManager.Instance.TotalAnimals : 5;
            }

            stampLine = "Passport stamped! " + done + " of " + total;
        }

        SetText(factStampText, stampLine);

        Show(factCard, true);
        HideHint();

        // The last photo wins the safari before this card opens, so I push the end screen back
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

    // Main Menu from the pause menu keeps the save and brings this screen back.
    public void ShowStartPanel()
    {
        Show(startPanel, true);
        Show(hudPanel, false);
        Show(endPanel, false);
        Show(factCard, false);
        HideMessages();

        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave;
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

    // Replay is also the Restart Safari button on the pause menu.
    public void Replay()
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
        ShowControlsTip();

        Refresh();

        // A save restored at 5 of 5 is a finished safari, so I go straight to the end screen.
        if (RunIsWon())
            ShowEnd();
    }

    private void Refresh()
    {
        GameManager game = GameManager.Instance;
        int score = game != null ? game.Score : 0;
        int stamps = game != null ? game.Stamps : 0;
        int total = game != null ? game.TotalAnimals : 0;

        // The score is the visitor's Respect, and the stamps are the Safari Passport.
        SetText(scoreText, "Respect: " + score);
        SetText(stampsText, "Passport: " + stamps + " / " + total);

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

            bool stamped = IsStamped(slot);

            // I check each array against the slot, because I may wire fewer icons or labels than slots.
            if (stampIcons != null && i < stampIcons.Length && stampIcons[i] != null)
                stampIcons[i].color = stamped ? stampedColor : emptyColor;

            if (stampLabels != null && i < stampLabels.Length)
                SetText(stampLabels[i], slot.DisplayName);
        }
    }

    // True when an animal of this species has been photographed in the current run.
    private static bool IsStamped(AnimalData data)
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

        // The reserve holds five animals, and I only use the 5 when they have not spawned yet.
        int total = game != null && game.TotalAnimals > 0 ? game.TotalAnimals : 5;

        // 100 is startingScore, so it is also the best Respect score the safari can end on.
        string summary = "Photos: " + stamps + " / " + total + "\n";
        summary += "Respect: " + score + " / 100   (disturbances: " + breaches + ")\n";

        string rating;
        string ratingLine;

        // The number of disturbances decides how the reserve would rate this visitor.
        if (breaches == 0)
        {
            rating = "Responsible Visitor";
            ratingLine = "You saw all five without disturbing one. That is how tourism keeps the Big Five wild.";
        }
        else if (breaches <= 2)
        {
            rating = "Careful Visitor";
            ratingLine = "A few close calls. Next time give them more space.";
        }
        else
        {
            rating = "Reckless Visitor";
            ratingLine = "Too many disturbances. Stressed animals leave the places tourists come to see.";
        }

        SetText(endSummaryText, summary + rating + "\n" + ratingLine);
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

    // The controls tip shows at the bottom of the HUD for the first few seconds of a safari.
    private void ShowControlsTip()
    {
        bool phone = Application.isMobilePlatform;

        SetText(controlsTipDesktop, phone ? string.Empty : "Move WASD · Look mouse · Photo click or E · Pause Esc");
        SetText(controlsTipPhone, phone ? "Move left stick · Look right stick · Photo button · Pause II" : string.Empty);

        tipShowing = true;
        tipHideTime = Time.time + controlsTipSeconds;
    }

    // Hides the controls tip, either because the timer ran out or because the player moved.
    private void HideControlsTip()
    {
        tipShowing = false;
        SetText(controlsTipDesktop, string.Empty);
        SetText(controlsTipPhone, string.Empty);
    }

    // Hides the hint, warning and toast together. The pause menu calls this too, so an old
// "too close" banner cannot sit behind the Paused title.
    public void HideMessages()
    {
        Show(hintRoot, false);
        Show(warningRoot, false);
        Show(toastRoot, false);
    }

    // A full screen flash image only fades by losing its alpha.
    private static void FadeOut(Image image, float step)
    {
        if (image == null || image.color.a <= 0f)
            return;

        Color colour = image.color;
        colour.a = Mathf.Max(0f, colour.a - step);
        image.color = colour;
    }

    private static void SetFlash(Image image, Color colour)
    {
        if (image != null)
            image.color = colour;
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

    // The close button only mentions E on a desktop, because a phone has no E key.
    private void SetCloseButtonLabel(string value)
    {
        if (factCardCloseButton == null)
            return;

        Text label = factCardCloseButton.GetComponentInChildren<Text>();
        if (label != null)
            label.text = value;
    }
}