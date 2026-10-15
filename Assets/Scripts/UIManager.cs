using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// This script runs every screen of the safari: start, HUD, passport, messages, fact card and end screen.
// I put it all in one script so the gameplay scripts never need a reference to the UI.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // The controls are written down once here and shown on the start panel, the pause panel and the HUD tip.
    // A phone has no keyboard and no mouse, so it gets the other line of the two.
    private const string ControlsDesktop = "Move WASD · Look mouse · Photo click or E · Pause Esc";
    private const string ControlsPhone = "Move left stick · Look right stick · Photo button · Pause II";
    private static string ControlsLine => Application.isMobilePlatform ? ControlsPhone : ControlsDesktop;

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
    [SerializeField] private AnimalData[] passportOrder;
    [SerializeField] private Image[] stampIcons;
    [SerializeField] private Text[] stampLabels;
    [SerializeField] private Color stampedColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("HUD text")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text stampsText;

    [Header("Controls text")]
    [Tooltip("One line of controls on the start panel and on the pause panel.")]
    [SerializeField] private Text startControlsText;
    [SerializeField] private Text pauseControlsText;

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
    [SerializeField] private Image crosshair;
    [SerializeField] private Text crosshairLabel;

    [Header("Photo and danger feedback")]
    [Tooltip("The photo flash is a white screen, the danger flash a red vignette at the edges only.")]
    [SerializeField] private Image photoFlash;
    [SerializeField] private Image dangerFlash;
    [SerializeField] private float photoFlashAlpha = 0.7f;
    [SerializeField] private float photoFlashSeconds = 0.25f;
    [SerializeField] private float dangerFlashAlpha = 0.6f;
    [SerializeField] private float dangerFlashSeconds = 0.6f;

    [Header("Controls tip")]
    [Tooltip("Shown for the first seconds of a safari, with the line for this platform only.")]
    [SerializeField] private Text controlsTip;
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
    [SerializeField] private Text endRatingText;
    [SerializeField] private Text endRangerText;
    [SerializeField] private Color responsibleColor = new Color(1f, 0.8f, 0.1f, 1f);
    [SerializeField] private Color carefulColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color recklessColor = new Color(1f, 0.35f, 0.15f, 1f);

    [Header("References")]
    [SerializeField] private PlayerController player;

    private float toastHideTime;
    private float tipHideTime;
    private bool tipShowing;
    private Coroutine photoRoutine;
    private Coroutine dangerRoutine;

    private static readonly Color CrosshairIdleColor = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color CrosshairGoodColor = new Color(0.35f, 0.95f, 0.35f, 1f);
    private static readonly Color CrosshairNearColor = new Color(1f, 0.72f, 0.18f, 1f);

    // The three states of the crosshair, so AnimalScanner only chooses a state.
    public static Color CrosshairNormal => CrosshairIdleColor;
    public static Color CrosshairReady => CrosshairGoodColor;
    public static Color CrosshairTooFar => CrosshairNearColor;

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

        Listen(continueButton, OnContinue);
        Listen(newSafariButton, OnNewSafari);
        Listen(replayButton, Replay);
        Listen(factCardCloseButton, HideFactCard);
    }

    // I take every subscription back here, or Replay would run Refresh twice for every change.
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

        // The scene buttons have no listeners of their own, so I can clear all of them in one go.
        ClearListeners(continueButton);
        ClearListeners(newSafariButton);
        ClearListeners(replayButton);
        ClearListeners(factCardCloseButton);
    }

    private void Start()
    {
        SwitchPanels(true, false, false, false);
        HideControlsTip();

        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave;

        // One controls line on the start panel and the pause panel, for this platform only.
        SetText(startControlsText, ControlsLine);
        SetText(pauseControlsText, ControlsLine);

        // The sticks are for phones only. I cannot test for a touchscreen, because touch laptops report one too.
        Show(mobileControls, Application.isMobilePlatform);

        // On a desktop the player closes the card with E or a click, so the button says so.
        if (factCardCloseButton != null)
            SetText(factCardCloseButton.GetComponentInChildren<Text>(), Application.isMobilePlatform ? "Close" : "Close (E)");

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
    }

    // The three message lines are the same pattern, so they share one helper.
    public void ShowHint(string message) => ShowMessage(hintRoot, hintText, message);
    public void HideHint() => Show(hintRoot, false);
    public void ShowWarning(string message) => ShowMessage(warningRoot, warningText, message);
    public void HideWarning() => Show(warningRoot, false);

    // A toast is the same as a hint, except it puts itself away again.
    public void ShowToast(string message)
    {
        ShowMessage(toastRoot, toastText, message);
        toastHideTime = Time.time + toastSeconds;
    }

    // The pause menu calls this too, so an old "too close" banner cannot sit behind the Paused title.
    public void HideMessages()
    {
        Show(hintRoot, false);
        Show(warningRoot, false);
        Show(toastRoot, false);
    }

    // Start, Main Menu and the start of a safari all need the same four panels switched.
    private void SwitchPanels(bool start, bool hud, bool end, bool card)
    {
        Show(startPanel, start);
        Show(hudPanel, hud);
        Show(endPanel, end);
        Show(factCard, card);
        HideMessages();
    }

    private static void ShowMessage(GameObject root, Text label, string message)
    {
        SetText(label, message);
        Show(root, true);
    }

    // A green ring means a good photo is possible, an amber ring means step closer.
    public void SetCrosshair(Color ringColour, string label)
    {
        SetColour(crosshair, ringColour);
        SetText(crosshairLabel, label);

        // The label takes the same colour as the ring, so the two always agree.
        SetColour(crosshairLabel, ringColour);
    }

    // A white flash over the whole screen, so a photo feels like a camera going off.
    public void FlashPhoto()
    {
        if (photoRoutine != null)
            StopCoroutine(photoRoutine);

        photoRoutine = StartCoroutine(Flash(photoFlash, photoFlashAlpha, photoFlashSeconds));
    }

    // A red vignette at the edges of the screen, so a disturbance is felt before it is read.
    public void FlashDanger()
    {
        if (dangerRoutine != null)
            StopCoroutine(dangerRoutine);

        dangerRoutine = StartCoroutine(Flash(dangerFlash, dangerFlashAlpha, dangerFlashSeconds));
    }

    // Both flashes use this one fade, on unscaled time so it still runs while the game is paused.
    private static IEnumerator Flash(Image image, float startAlpha, float seconds)
    {
        if (image == null || seconds <= 0f)
            yield break;

        // The image keeps its own tint and only loses alpha, so a flash never changes colour.
        Color tint = image.color;
        float left = seconds;

        while (left > 0f)
        {
            tint.a = startAlpha * left / seconds;
            SetColour(image, tint);
            left -= Time.unscaledDeltaTime;
            yield return null;
        }

        tint.a = 0f;
        SetColour(image, tint);
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
        if (!firstPhoto)
        {
            SetText(factStampText, "Already in your passport.");
        }
        else
        {
            GameManager game = GameManager.Instance;
            int done = game != null ? game.Stamps : 0;
            int total = game != null && game.TotalAnimals > 0 ? game.TotalAnimals : 5;
            SetText(factStampText, "Passport stamped! " + done + " of " + total);
        }

        Show(factCard, true);
        HideHint();

        // The last photo wins the safari before this card opens, so I push the end screen back.
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
        SwitchPanels(true, false, false, false);

        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave;
    }

    // Continue reads the save back and New Safari throws it away. Then both start playing.
    private void OnContinue()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restore();
            BeginPlay();
        }
    }

    private void OnNewSafari()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetRun();
            BeginPlay();
        }
    }

    // Replay is also the Restart Safari button on the pause menu.
    public void Replay()
    {
        // I put the player back at the gate before I clear the state, so the guide line starts there again.
        if (player != null)
            player.ResetToSpawn();

        if (GameManager.Instance != null)
            GameManager.Instance.ResetRun();

        BeginPlay();
    }

    private void BeginPlay()
    {
        SwitchPanels(false, true, false, false);

        if (player != null)
            player.enabled = true;

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

            // I check each array against the slot, because I may wire fewer icons or labels than slots.
            if (stampIcons != null && i < stampIcons.Length && stampIcons[i] != null)
                stampIcons[i].color = IsStamped(slot) ? stampedColor : emptyColor;

            if (stampLabels != null && i < stampLabels.Length)
                SetText(stampLabels[i], slot.DisplayName);
        }
    }

    // True when an animal of this species has been photographed in the current run.
    private static bool IsStamped(AnimalData data)
    {
        foreach (Animal animal in Animal.All)
            if (animal.Data == data && animal.Scanned)
                return true;

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

        // The three numbers are one block, so the eye reads the result before the verdict.
        string stats = "Photos: " + stamps + " / " + total + "\n";
        stats += "Respect: " + score + " / 100\n";
        stats += "Disturbances: " + breaches;
        SetText(endSummaryText, stats);

        // The number of disturbances decides how the reserve would rate this visitor.
        string rating = "Responsible Visitor";
        string rangerLine = "You saw all five without disturbing one. That is how tourism keeps the Big Five wild.";
        Color ratingColour = responsibleColor;

        if (breaches > 0)
        {
            rating = breaches <= 2 ? "Careful Visitor" : "Reckless Visitor";
            rangerLine = breaches <= 2
                ? "A few close calls. Next time give them more space."
                : "Too many disturbances. Stressed animals leave the places tourists come to see.";
            ratingColour = breaches <= 2 ? carefulColor : recklessColor;
        }

        SetText(endRatingText, rating);

        // The rating is bigger than the numbers and coloured by how the safari went.
        SetColour(endRatingText, ratingColour);

        // Ranger Amani is the one who signs the safari off, so his line comes last.
        SetText(endRangerText, "Ranger Amani: " + rangerLine);
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
        SetText(controlsTip, ControlsLine);
        tipShowing = true;
        tipHideTime = Time.time + controlsTipSeconds;
    }

    // Hides the controls tip, either because the timer ran out or because the player moved.
    private void HideControlsTip()
    {
        tipShowing = false;
        SetText(controlsTip, string.Empty);
    }

    // Every button is hooked up and taken down the same way, so it is one helper used four times.
    private static void Listen(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void ClearListeners(Button button)
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    // True once the run is won. This is when the end screen takes over.
    private static bool RunIsWon() => GameManager.Instance != null && GameManager.Instance.HasWon;

    // A panel or a label I did not wire is skipped, so these never throw.
    private static void Show(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private static void SetText(Text label, string value)
    {
        if (label != null)
            label.text = value;
    }

    // Any graphic I did not wire is skipped, so this never throws.
    private static void SetColour(Graphic graphic, Color colour)
    {
        if (graphic != null)
            graphic.color = colour;
    }
}