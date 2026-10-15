using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// I read input only through the New Input System, never through the old Input class.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public const string SensitivityKey = "MouseSensitivity";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Look")]
    [Tooltip("Degrees per second for the arrow keys and both on-screen sticks.")]
    [SerializeField] private float lookSpeed = 120f;
    [Tooltip("Degrees per pixel of mouse movement.")]
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

    // Wired to UIManager.HandleBump in the Inspector.
    public UnityEvent<Collider> Bumped;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerInput.actions == null)
        {
            Debug.LogError($"{nameof(PlayerController)} needs a PlayerInput with actions assigned.", this);
            enabled = false;
            return;
        }

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        if (playerCamera != null)
        {
            spawnCameraRotation = playerCamera.transform.localRotation;

            // The shake moves the camera, so I remember where it rests.
            cameraRestPosition = playerCamera.transform.localPosition;
        }

        moveAction = playerInput.actions.FindAction("Player/Move", throwIfNotFound: false);
        lookAction = playerInput.actions.FindAction("Player/Look", throwIfNotFound: false);
        mouseLookAction = playerInput.actions.FindAction("Player/MouseLook", throwIfNotFound: false);

        if (PlayerPrefs.HasKey(SensitivityKey))
            mouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, mouseSensitivity);

        // PlayerInput switches scheme by itself and drops the on-screen sticks, so I lock it here.
        // This is in Awake, not Start, because a stick can ask for a switch before Start runs.
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

        if (Paused() || (UIManager.Instance != null && UIManager.Instance.IsFactCardOpen))
            return;

        Look();
        Move();
    }

    public void ResetToSpawn()
    {
        // The CharacterController would overwrite the move, so it is switched off first.
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

    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = value;
    }

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

    private void UpdateCursor()
    {
        if (Application.isMobilePlatform)
            return;

        GameManager game = GameManager.Instance;
        bool playing = game != null && game.IsPlaying;
        bool paused = game != null && game.IsPaused;

        // Esc frees the mouse in the browser, so I wait a few frames before deciding to pause.
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

            if (UIManager.Instance == null || (!UIManager.Instance.IsFactCardOpen && !UIManager.Instance.IsEndScreenOpen))
                PauseMenu.Instance?.Pause();
        }

        cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;

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
        // Keys and sticks give a speed, so they are scaled by the frame time.
        Vector2 look = lookAction.ReadValue<Vector2>() * (lookSpeed * Time.deltaTime);

        // Mouse delta is already per frame, so it is not scaled by deltaTime.
        if (mouseLookAction != null)
            look += mouseLookAction.ReadValue<Vector2>() * mouseSensitivity;

        // Pitch moves only the camera, so looking up does not walk backwards.
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;

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

        if (Time.time < nextBumpTime)
            return;

        nextBumpTime = Time.time + bumpCooldown;

        if (bumpSource != null && bumpClip != null)
            bumpSource.PlayOneShot(bumpClip);

        Bumped.Invoke(hit.collider);
    }
}