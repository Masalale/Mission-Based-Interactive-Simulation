using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// All the screens live here, so gameplay scripts never need a reference to the UI.
// Buttons call the public methods below, wired up with On Click in the Inspector.
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
    [Tooltip("Both Quit buttons. Hidden on WebGL.")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button endQuitButton;

    [Header("Passport")]
    [SerializeField] private AnimalData[] passportOrder;
    [SerializeField] private Image[] stampIcons;
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
    [Tooltip("Photo flash is white. Danger flash is red at the edges.")]
    [SerializeField] private Image photoFlash;
    [SerializeField] private Image dangerFlash;
    [SerializeField] private float photoFlashAlpha = 0.7f;
    [SerializeField] private float photoFlashSeconds = 0.25f;
    [SerializeField] private float dangerFlashAlpha = 0.6f;
    [SerializeField] private float dangerFlashSeconds = 0.6f;

    [Header("Fact card")]
    [SerializeField] private Text factTitleText;
    [SerializeField] private Text factApproachText;
    [SerializeField] private Text factPlacesText;
    [SerializeField] private Text factSignsText;
    [SerializeField] private Text factDoText;
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

    public static Color CrosshairNormal => CrosshairIdleColor;
    public static Color CrosshairReady => CrosshairGoodColor;
    public static Color CrosshairTooFar => CrosshairNearColor;

    public bool IsFactCardOpen => factCard != null && factCard.activeSelf;

    public bool IsEndScreenOpen => endPanel != null && endPanel.activeSelf;

    private void Awake()
    {
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
        ShowStartPanel();

        Show(mobileControls, Application.isMobilePlatform);

        // A browser tab cannot quit itself, so both Quit buttons are hidden on WebGL.
        Show(ObjectOf(quitButton), !IsWebGL);
        Show(ObjectOf(endQuitButton), !IsWebGL);

        Refresh();
    }

    public void ShowHint(string message)
    {
        ShowMessage(hintRoot, hintText, message);

        // A new hint replaces the welcome line, so the old timer must not hide it early.
        CancelInvoke(nameof(HideHint));
    }

    public void HideHint() => Show(hintRoot, false);
    public void ShowWarning(string message) => ShowMessage(warningRoot, warningText, message);
    public void HideWarning() => Show(warningRoot, false);

    public void ShowToast(string message)
    {
        ShowMessage(toastRoot, toastText, message);

        // A new toast starts the timer again, so the old timer cannot hide it early.
        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), toastSeconds);
    }

    private void HideToast() => Show(toastRoot, false);

    public void ShowHud(bool visible) => Show(hudPanel, visible);

    public void HideMessages()
    {
        Show(hintRoot, false);
        Show(warningRoot, false);
        Show(toastRoot, false);
    }

    private void SwitchPanels(bool start, bool hud, bool end, bool card)
    {
        Show(startPanel, start);
        Show(hudPanel, hud);
        Show(endPanel, end);
        Show(factCard, card);
        Show(instructionsPanel, false);
        HideMessages();
    }

    private static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

    // Menus need the mouse back, because the player script locks it while playing.
    private static void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void ShowMessage(GameObject root, Text label, string message)
    {
        SetText(label, message);

        Show(root, !string.IsNullOrEmpty(message));
    }

    public void SetCrosshair(Color ringColour, string label)
    {
        SetColour(crosshair, ringColour);
        SetText(crosshairLabel, label);

        SetColour(crosshairLabel, ringColour);
    }

    public void FlashPhoto()
    {
        if (photoRoutine != null)
            StopCoroutine(photoRoutine);

        photoRoutine = StartCoroutine(Flash(photoFlash, photoFlashAlpha, photoFlashSeconds));
    }

    public void FlashDanger()
    {
        if (dangerRoutine != null)
            StopCoroutine(dangerRoutine);

        dangerRoutine = StartCoroutine(Flash(dangerFlash, dangerFlashAlpha, dangerFlashSeconds));
    }

    // Unscaled time, so the flash still fades while the game is paused.
    private static IEnumerator Flash(Image image, float startAlpha, float seconds)
    {
        if (image == null || seconds <= 0f)
            yield break;

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

    public void ShowFactCard(AnimalData data, bool firstPhoto, bool disturbed)
    {
        if (data == null)
            return;

        string animalName = data.DisplayName.ToLower();

        SetText(factTitleText, data.DisplayName + " · " + data.SwahiliName);

        if (disturbed)
            SetText(factApproachText, "• Your approach: Disturbed. You got too close and the " + animalName + " charged.");
        else
            SetText(factApproachText, "• Your approach: Calm. You kept your distance and the " + animalName + " stayed relaxed.");

        SetText(factPlacesText, "• Where to see: " + data.Places);
        SetText(factSignsText, "• Warning signs: " + data.WarningSigns);
        SetText(factDoText, "• Do: " + data.DoTip + "\n• Don't: " + data.DontTip);

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

        // The end screen waits until this card is closed.
        if (RunIsWon())
            Show(endPanel, false);
    }

    public void HideFactCard()
    {
        Show(factCard, false);

        if (RunIsWon())
            ShowEnd();
    }

    public void ShowStartPanel()
    {
        SwitchPanels(true, false, false, false);
        FreeCursor();

        if (player != null)
            player.enabled = false;

        // The menu is quiet, so the safari sounds only start with Start Safari.
        AudioListener.pause = true;
    }

    public void ToMainMenu()
    {
        PauseMenu.Instance?.Resume();

        if (GameManager.Instance != null)
            GameManager.Instance.LeaveRun();

        ShowStartPanel();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnStart()
    {
        if (GameManager.Instance != null)
            Replay();
    }

    public void Replay()
    {
        // Move the player back first, so the guide line starts from the gate.
        if (player != null)
            player.ResetToSpawn();

        if (GameManager.Instance != null)
            GameManager.Instance.ResetRun();

        BeginPlay();
    }

    private void BeginPlay()
    {
        SwitchPanels(false, true, false, false);
        AudioListener.pause = false;

        if (player != null)
            player.enabled = true;

        Refresh();

        ShowHint("Ranger Amani: Karibu! Kenya's Big Five are out today: lion, leopard, elephant, rhino and buffalo. Find all five.");
        Invoke(nameof(HideHint), 8f);

        if (RunIsWon())
            ShowEnd();
    }

    public void Refresh()
    {
        GameManager game = GameManager.Instance;
        int score = game != null ? game.Score : 0;
        int stamps = game != null ? game.Stamps : 0;
        int total = game != null ? game.TotalAnimals : 0;

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

            // The icons may be wired short, so check the array.
            if (stampIcons != null && i < stampIcons.Length && stampIcons[i] != null)
                stampIcons[i].color = IsStamped(slot) ? stampedColor : emptyColor;
        }
    }

    private static bool IsStamped(AnimalData data)
    {
        foreach (Animal animal in Animal.All)
            if (animal.Data == data && animal.Scanned)
                return true;

        return false;
    }

    public void ShowEnd()
    {
        Show(endPanel, true);
        Show(factCard, false);

        Show(hudPanel, false);

        HideMessages();

        if (player != null)
            player.enabled = false;

        FreeCursor();

        GameManager game = GameManager.Instance;
        int stamps = game != null ? game.Stamps : 0;
        int score = game != null ? game.Score : 0;
        int breaches = game != null ? game.Breaches : 0;

        // Fall back to 5 while the animals are not in the scene yet.
        int total = game != null && game.TotalAnimals > 0 ? game.TotalAnimals : 5;

        string stats = "Photos: " + stamps + " / " + total + "\n";
        stats += "Respect: " + score + " / 100\n";
        stats += "Disturbances: " + breaches;
        SetText(endSummaryText, stats);

        string rating = "Responsible Visitor";
        string rangerLine = "Asante! You met all of Kenya's Big Five without disturbing one. That is how tourism keeps them wild.";
        Color ratingColour = responsibleColor;

        if (breaches > 0)
        {
            rating = breaches <= 2 ? "Careful Visitor" : "Reckless Visitor";
            rangerLine = breaches <= 2
                ? "Asante! You met Kenya's Big Five, with a few close calls. Next time give them more space."
                : "Hatari! You met Kenya's Big Five and somehow lived to tell the tale. Next time, admire them from further away.";
            ratingColour = breaches <= 2 ? carefulColor : recklessColor;
        }

        SetText(endRatingText, rating);

        SetColour(endRatingText, ratingColour);

        SetText(endRangerText, "Ranger Amani: " + rangerLine);
    }

    public void HandleBump(Collider hit)
    {
        // The collider sits on the Body child, so the Animal is found on a parent.
        Animal animal = hit.GetComponentInParent<Animal>();

        if (animal != null && animal.Data != null)
            ShowToast("Blocked by the " + animal.Data.DisplayName);
        else
            ShowToast("Blocked by a rock or tree");
    }

    private static bool RunIsWon() => GameManager.Instance != null && GameManager.Instance.HasWon;

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

    private static void SetColour(Graphic graphic, Color colour)
    {
        if (graphic != null)
            graphic.color = colour;
    }
}
