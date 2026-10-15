using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Buttons and the Pause key are wired up in the Inspector.
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("How to Play, shown over the pause panel.")]
    [SerializeField] private GameObject instructionsPanel;
    [Header("Mouse")]
    [Tooltip("Sensitivity row. Hidden on phones.")]
    [SerializeField] private GameObject sensitivityRow;
    [SerializeField] private Slider sensitivitySlider;
    [Header("References")]
    [SerializeField] private UIManager ui;
    [SerializeField] private PlayerController player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (sensitivityRow != null)
            sensitivityRow.SetActive(!Application.isMobilePlatform);
        if (sensitivitySlider != null)
            sensitivitySlider.value = PlayerPrefs.GetFloat(PlayerController.SensitivityKey, 0.1f);

        // Hidden at start, so the game never opens with the pause panel showing.
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

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
            Toggle();
    }

    public void Toggle()
    {
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

        if (pausePanel != null)
            pausePanel.SetActive(true);

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

    public void Restart()
    {
        Resume();
        if (ui != null)
            ui.Replay();
    }

    public void ToMainMenu()
    {
        Resume();

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
