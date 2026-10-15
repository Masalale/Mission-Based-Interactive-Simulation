using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// This is the pause menu. Esc or the II button opens it while a safari is running.
// Pausing stops the clock and the sound and frees the mouse. Resume puts both back.
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [Header("Mouse")]
    [Tooltip("The whole sensitivity row, so it can be hidden on phones.")]
    [SerializeField] private GameObject sensitivityRow;
    [SerializeField] private Slider sensitivitySlider;
    [Header("References")]
    [SerializeField] private UIManager ui;
    [SerializeField] private PlayerController player;

    private InputAction pauseAction;

    private void Awake()
    {
        // There should only be one of me in the scene. If there are two, I remove the extra one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // The player holds the one PlayerInput, so I take the Pause action from there.
        PlayerInput playerInput = Object.FindFirstObjectByType<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
            pauseAction = playerInput.actions.FindAction("Player/Pause", throwIfNotFound: false);

        // A phone has no mouse to be sensitive about, so the slider only shows on a desktop.
        if (sensitivityRow != null)
            sensitivityRow.SetActive(!Application.isMobilePlatform);
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat(PlayerController.SensitivityKey, 0.1f);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);
        if (menuButton != null)
            menuButton.onClick.AddListener(ToMainMenu);
        if (pauseButton != null)
            pauseButton.onClick.AddListener(Toggle);

        // The panel is built switched off, so the game never starts with the safari stopped.
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.performed += OnPause;
    }

    private void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.performed -= OnPause;

        // Leaving the panel switched off while paused would freeze the game for good.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            Resume();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        Toggle();
    }

    // The II button in the HUD, and the only way to pause on a phone.
    public void Toggle()
    {
        // I only pause a safari that is running, and never over the fact card or the end screen.
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || ui != null && (ui.IsFactCardOpen || ui.IsEndScreenOpen))
            return;

        if (GameManager.Instance.IsPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || GameManager.Instance.IsPaused)
            return;

        GameManager.Instance.SetPaused(true);

        // The panel is built switched off, so the game never starts with the safari stopped.
        if (pausePanel != null)
            pausePanel.SetActive(true);

        // The HUD messages are gone, so an old warning cannot sit behind the Paused title.
        if (ui != null)
            ui.HideMessages();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.SetPaused(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // Restart Safari is the same as the Play Again button on the end screen.
    private void Restart()
    {
        Resume();
        if (ui != null)
            ui.Replay();
    }

    // Main Menu keeps the save, so Start Safari can carry it on.
    private void ToMainMenu()
    {
        Resume();

        // UIManager does the same two steps for the end screen, so there is only one copy.
        if (ui != null)
            ui.ToMainMenu();
    }

    // The slider writes the value straight away, so it survives a restart of the game.
    private void OnSensitivityChanged(float value)
    {
        if (player != null)
            player.SetMouseSensitivity(value);

        PlayerPrefs.SetFloat(PlayerController.SensitivityKey, value);
        PlayerPrefs.Save();
    }
}