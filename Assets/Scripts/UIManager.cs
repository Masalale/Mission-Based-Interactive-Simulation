using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// This script runs every screen of the safari: start, HUD, passport, messages, fact card and end screen.
// I put it all in one script so the gameplay scripts never need a reference to the UI.
// The buttons call the public methods below. They are wired up in the Inspector with On Click.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private GameObject factCard;
    [SerializeField] private GameObject mobileControls;

    [Header("Quit")]
    [Tooltip("A browser tab cannot close itself, so both Quit buttons are hidden on WebGL.")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button endQuitButton;

    [Header("Passport")]
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

    private void Start()
    {
        SwitchPanels(true, false, false, false);

        // The sticks are for phones only. I cannot test for a touchscreen, because touch laptops report one too.
        Show(mobileControls, Application.isMobilePlatform);

        // A browser tab cannot close itself, so Quit only shows where it can do something.
        Show(ObjectOf(quitButton), !IsWebGL);
        Show(ObjectOf(endQuitButton), !IsWebGL);

        // I switch the player off until a run starts, so I cannot walk around on the start screen.
        if (player != null)
            player.enabled = false;

        Refresh();
    }

    // The three message lines are the same pattern, so they share one helper.
    public void ShowHint(string message) => ShowMessage(hintRoot, hintText, message);
    public void HideHint() => Show(hintRoot, false);
    public void ShowWarning(string message) => ShowMessage(warningRoot, warningText, message);
    public void HideWarning() => Show(warningRoot, false);

    // A toast is the same as a hint, except it puts itself away again after a few seconds.
    public void ShowToast(string message)
    {
        ShowMessage(toastRoot, toastText, message);

        // A new toast starts the timer again, so the old timer cannot hide it early.
        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), toastSeconds);
    }

    private void HideToast() => Show(toastRoot, false);

    // The pause menu calls this too, so an old "too close" banner cannot sit behind the Paused title.
    public void HideMessages()
    {
        Show(hintRoot, false);
        Show(warningRoot, false);
        Show(toastRoot, false);
    }

    // Start, Main Menu and the start of a safari all need the same panels switched.
    private void SwitchPanels(bool start, bool hud, bool end, bool card)
    {
        Show(startPanel, start);
        Show(hudPanel, hud);
        Show(endPanel, end);
        Show(factCard, card);
        Show(instructionsPanel, false);
        HideMessages();
    }

    // Application.Quit does nothing in a browser, so the Quit button is hidden there.
    private static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

    // Every menu needs the mouse back, because the player is switched off and cannot give it.
    private static void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void ShowMessage(GameObject root, Text label, string message)
    {
        SetText(label, message);
        FitBoxToText(label);

        // A box with no words in it stays hidden, so no empty pill is left on the screen.
        Show(root, !string.IsNullOrEmpty(message));
    }

    // The message boxes hug their text, but a long line wraps instead of running off the screen.
    // The patch gives every message text a LayoutElement, so I only set its width here.
    private static void FitBoxToText(Text label)
    {
        LayoutElement box = label != null ? label.GetComponent<LayoutElement>() : null;

        if (box != null)
            box.preferredWidth = Mathf.Min(label.preferredWidth + 1f, 760f);
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

    // Main Menu from the pause menu or the end screen keeps the save and brings this screen back.
    public void ShowStartPanel()
    {
        SwitchPanels(true, false, false, false);
        FreeCursor();
    }

    // Main Menu is the same two steps wherever the button sits: leave the run, then show the start panel.
    public void ToMainMenu()
    {
        PauseMenu.Instance?.Resume();

        if (GameManager.Instance != null)
            GameManager.Instance.LeaveRun();

        ShowStartPanel();
    }

    // Quit is not on WebGL, where it would do nothing.
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // One Start button: it carries on a save that is still running, and otherwise starts a fresh safari.
    public void OnStart()
    {
        if (GameManager.Instance == null)
            return;

        if (SaveSystem.HasSave && !SaveIsFinished())
            GameManager.Instance.Restore();
        else
            GameManager.Instance.ResetRun();

        BeginPlay();
    }

    // A save of a finished safari is not worth carrying on, so Start Safari throws that one away.
    private static bool SaveIsFinished()
    {
        if (!SaveSystem.TryLoad(out SaveData data))
            return false;

        int total = GameManager.Instance != null ? GameManager.Instance.TotalAnimals : Animal.All.Count;
        return total > 0 && data.scannedIds.Count >= total;
    }

    // Replay is Restart Safari on the pause menu. The end screen uses Main Menu, then Start Safari.
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

        Refresh();

        // A save restored at 5 of 5 is a finished safari, so I go straight to the end screen.
        if (RunIsWon())
            ShowEnd();
    }

    // GameManager calls this whenever a number changes, so the HUD always shows the latest one.
    public void Refresh()
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

    // GameManager calls this on the last stamp of the safari, and the end screen takes over.
    public void ShowEnd()
    {
        Show(endPanel, true);
        Show(factCard, false);

        // The end screen is on its own, so the HUD and its pause button go away behind it.
        Show(hudPanel, false);

        // Nothing from the safari is left floating over the end screen.
        HideMessages();

        if (player != null)
            player.enabled = false;

        // The player is off, so the mouse has to come back for the buttons.
        FreeCursor();

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

    // The player calls this when it walks into something solid, and I turn that into a toast.
    public void HandleBump(Collider hit)
    {
        // The collider is on the Body child, so I look up for the animal on the parent.
        Animal animal = hit.GetComponentInParent<Animal>();

        if (animal != null && animal.Data != null)
            ShowToast("Blocked by the " + animal.Data.DisplayName);
        else
            ShowToast("Blocked by a rock or tree");
    }

    // True once the run is won. This is when the end screen takes over.
    private static bool RunIsWon() => GameManager.Instance != null && GameManager.Instance.HasWon;

    // A panel or a label I did not wire is skipped, so these never throw.
    private static void Show(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private static GameObject ObjectOf(Button button)
    {
        return button != null ? button.gameObject : null;
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
