using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Moves and turns the player in first person.
// I read input only through the New Input System, never through the old Input class.
// This script runs before UIManager, so the control scheme is locked before the start screen.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    // The pause menu slider and this script share this one PlayerPrefs key.
    public const string SensitivityKey = "MouseSensitivity";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Look")]
    [Tooltip("Degrees per second for the arrow keys and both on-screen sticks.")]
    [SerializeField] private float lookSpeed = 120f;
    [Tooltip("Degrees per pixel for the mouse. Mouse delta is already per frame, so it is not multiplied by deltaTime.")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Bump feedback")]
    [Tooltip("Seconds before another bump can play its sound and toast.")]
    [SerializeField] private float bumpCooldown = 1f;

    [Header("References")]
    [Tooltip("Camera child of the player, at eye height.")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource bumpSource;
    [SerializeField] private AudioClip bumpClip;

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction mouseLookAction;
    private float pitch;
    private Vector3 velocity;
    private float nextBumpTime;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Quaternion spawnCameraRotation;
    private Vector3 cameraRestPosition;
    private bool cursorWasLocked;
    private int lockedFrames;
    private int unlockedFrames;
    private Coroutine shakeRoutine;

    // I call this when the player walks into something solid. The patch wires it to UIManager.HandleBump.
    public UnityEvent<Collider> Bumped;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Fall back to the camera under the player so I do not have to wire it by hand.
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerInput.actions == null)
        {
            Debug.LogError($"{nameof(PlayerController)} needs a PlayerInput with actions assigned.", this);
            enabled = false;
            return;
        }

        // I remember the start so Replay can put the player back exactly here.
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        if (playerCamera != null)
        {
            spawnCameraRotation = playerCamera.transform.localRotation;

            // The shake adds a small offset to the camera, so I keep where it really sits.
            cameraRestPosition = playerCamera.transform.localPosition;
        }

        // I look the three actions up by name instead of holding references, so the script is easy to reuse.
        moveAction = playerInput.actions.FindAction("Player/Move", throwIfNotFound: false);
        lookAction = playerInput.actions.FindAction("Player/Look", throwIfNotFound: false);
        mouseLookAction = playerInput.actions.FindAction("Player/MouseLook", throwIfNotFound: false);

        // The pause slider saves my sensitivity, so I start with whatever was chosen last time.
        if (PlayerPrefs.HasKey(SensitivityKey))
            mouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, mouseSensitivity);

        // The sticks add a fake gamepad while the mouse is still in use, and PlayerInput was switching
        // schemes on its own and hiding the bindings of the other device, so the sticks dropped out.
        // Locking the scheme and clearing the mask and the device list lets keyboard, mouse and the
        // on-screen gamepad all use the same action map. This is in Awake, not Start, because a stick
        // that is active when the scene loads asks PlayerInput to switch scheme before Start runs.
        playerInput.neverAutoSwitchControlSchemes = true;
        playerInput.actions.bindingMask = null;
        playerInput.actions.devices = null;

        if (moveAction == null || lookAction == null)
        {
            Debug.LogError($"{nameof(PlayerController)} could not find Player/Move or Player/Look.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (moveAction == null || lookAction == null)
            return;

        UpdateCursor();

        // While the pause menu is open or a fact card is on screen, the player does not move or look.
        if (Paused() || (UIManager.Instance != null && UIManager.Instance.IsFactCardOpen))
            return;

        Look();
        Move();
    }

    // Puts the player back at the start. UIManager calls this when I press Replay.
    public void ResetToSpawn()
    {
        // The CharacterController writes over the transform from its own saved position,
        // so it has to be off while I move the player back.
        bool wasEnabled = controller.enabled;
        controller.enabled = false;

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        pitch = 0f;
        velocity = Vector3.zero;
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = spawnCameraRotation;
            playerCamera.transform.localPosition = cameraRestPosition;
        }

        controller.enabled = wasEnabled;
    }

    // The pause slider changes it while the game is stopped, so I take the new value straight away.
    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = value;
    }

    // A disturbance shakes the camera a little, so the player feels it and not only reads it.
    public void Shake(float seconds)
    {
        if (playerCamera == null || shakeRoutine != null)
            return;

        shakeRoutine = StartCoroutine(ShakeRoutine(seconds));
    }

    private IEnumerator ShakeRoutine(float seconds)
    {
        Transform cameraTransform = playerCamera.transform;

        while (seconds > 0f)
        {
            cameraTransform.localPosition = cameraRestPosition + new Vector3(UnityEngine.Random.Range(-0.06f, 0.06f), UnityEngine.Random.Range(-0.06f, 0.06f), 0f);
            seconds -= Time.unscaledDeltaTime;
            yield return null;
        }

        cameraTransform.localPosition = cameraRestPosition;
        shakeRoutine = null;
    }

    // On a phone there is no mouse to lock, so all of this only runs on a desktop.
    private void UpdateCursor()
    {
        if (Application.isMobilePlatform)
            return;

        GameManager game = GameManager.Instance;
        bool playing = game != null && game.IsPlaying;
        bool paused = game != null && game.IsPaused;

        // The browser takes the pointer lock back on Esc, so an unlocked mouse while the safari runs
        // means the player wants to pause. I wait a moment first, so the Esc key can do it properly.
        if (playing && !paused && Cursor.lockState == CursorLockMode.Locked)
            lockedFrames++;
        else
            lockedFrames = 0;

        if (cursorWasLocked && lockedFrames > 5 && Cursor.lockState != CursorLockMode.Locked)
            unlockedFrames++;
        else
            unlockedFrames = 0;

        if (unlockedFrames > 2)
        {
            unlockedFrames = 0;

            // Only a live safari pauses, never the fact card or the end screen.
            if (UIManager.Instance == null || (!UIManager.Instance.IsFactCardOpen && !UIManager.Instance.IsEndScreenOpen))
                PauseMenu.Instance?.Pause();
        }

        cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;

        // While the safari runs I hide the mouse and let it look all the time, with no button held.
        if (playing && !paused)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;

            Cursor.visible = false;
        }
        else if (paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private bool Paused()
    {
        return GameManager.Instance != null && GameManager.Instance.IsPaused;
    }

    private void Look()
    {
        // The sticks and the arrow keys give a speed, so I scale them by the frame time.
        Vector2 look = lookAction.ReadValue<Vector2>() * (lookSpeed * Time.deltaTime);

        // The mouse is always read while the safari runs. Its delta is already for this frame,
        // so I do not scale it by deltaTime again.
        if (mouseLookAction != null)
            look += mouseLookAction.ReadValue<Vector2>() * mouseSensitivity;

        // Yaw turns the whole body. Pitch turns only the camera, so looking up does not walk backwards.
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        // Length 1 at most, so walking diagonally is not faster than walking straight.
        direction = Vector3.ClampMagnitude(direction, 1f) * moveSpeed;

        // A small downward speed keeps me on the ground when I walk down a slope.
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        controller.Move((direction + Vector3.up * velocity.y) * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Standing on the ground reports a hit every frame, so anything I can walk on is not a bump.
        if (hit.normal.y > 0.5f)
            return;

        // One bump per cooldown, so the sound and the toast do not repeat while I slide along a rock.
        if (Time.time < nextBumpTime)
            return;

        nextBumpTime = Time.time + bumpCooldown;

        if (bumpSource != null && bumpClip != null)
            bumpSource.PlayOneShot(bumpClip);

        Bumped.Invoke(hit.collider);
    }
}