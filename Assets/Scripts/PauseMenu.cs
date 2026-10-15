using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// This is the pause menu. Esc or the II button opens it while a safari is running.
// Pausing stops the clock and the sound and frees the mouse. Resume puts both back.
// The buttons and the Pause key are wired up in the Inspector, so this script only does the work.
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("How to Play opens this over the pause panel, so Resume closes it too.")]
    [SerializeField] private GameObject instructionsPanel;
    [Header("Mouse")]
    [Tooltip("The whole sensitivity row, so it can be hidden on phones.")]
    [SerializeField] private GameObject sensitivityRow;
    [SerializeField] private Slider sensitivitySlider;
    [Header("References")]
    [SerializeField] private UIManager ui;
    [SerializeField] private PlayerController player;

    private void Awake()
    {
        // There should only be one of me in the scene. If there are two, I remove the extra one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // A phone has no mouse to be sensitive about, so the slider only shows on a desktop.
        if (sensitivityRow != null)
            sensitivityRow.SetActive(!Application.isMobilePlatform);
        if (sensitivitySlider != null)
            sensitivitySlider.value = PlayerPrefs.GetFloat(PlayerController.SensitivityKey, 0.1f);

        // The panel is built switched off, so the game never starts with the safari stopped.
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        // Leaving the panel switched off while paused would freeze the game for good.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            Resume();
    }

    // The Pause action on the player calls this. Only the press counts, not the release.
    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
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

        // Esc can resume while How to Play is open over this menu, so that panel closes too.
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
    }

    // Restart Safari throws the run away and starts a fresh one, the same as Replay in UIManager.
    public void Restart()
    {
        Resume();
        if (ui != null)
            ui.Replay();
    }

    // Main Menu keeps the save, so Start Safari can carry it on.
    public void ToMainMenu()
    {
        Resume();

        // UIManager does the same two steps for the end screen, so there is only one copy.
        if (ui != null)
            ui.ToMainMenu();
    }

    // The slider writes the value straight away, so it survives a restart of the game.
    public void OnSensitivityChanged(float value)
    {
        if (player != null)
            player.SetMouseSensitivity(value);

        PlayerPrefs.SetFloat(PlayerController.SensitivityKey, value);
        PlayerPrefs.Save();
    }
}
