using System;
using UnityEngine;
using UnityEngine.InputSystem;

// First-person walking and looking, read only through the New Input System (PlayerInput).
// Runs before UIManager, so its Start locks the control scheme before the start screen disables it.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
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
    private InputAction lookHoldAction;
    private float pitch;
    private Vector3 velocity;
    private float nextBumpTime;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Quaternion spawnCameraRotation;

    // Raised when the player walks into a solid body. UIManager shows the bump toast.
    public event Action<Collider> Bumped;

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

        // Replay puts the player back here, so remember the start position, the body rotation and the camera's look.
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        if (playerCamera != null)
            spawnCameraRotation = playerCamera.transform.localRotation;

        moveAction = playerInput.actions.FindAction("Player/Move", throwIfNotFound: false);
        lookAction = playerInput.actions.FindAction("Player/Look", throwIfNotFound: false);
        mouseLookAction = playerInput.actions.FindAction("Player/MouseLook", throwIfNotFound: false);
        lookHoldAction = playerInput.actions.FindAction("Player/LookHold", throwIfNotFound: false);

        if (moveAction == null || lookAction == null)
        {
            Debug.LogError($"{nameof(PlayerController)} could not find Player/Move or Player/Look.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (playerInput.actions == null)
            return;

        // The on-screen sticks add a virtual gamepad while the mouse is still in use. Auto-switching would hide
        // the bindings that do not match the active device, and the sticks would drop out. Locking the scheme and
        // clearing the binding mask and the device list lets keyboard, mouse and the on-screen gamepad work together.
        playerInput.neverAutoSwitchControlSchemes = true;
        playerInput.actions.bindingMask = null;
        playerInput.actions.devices = null;
    }

    private void Update()
    {
        if (moveAction == null || lookAction == null)
            return;

        Look();
        Move();
    }

    // Puts the player back at the start with a level camera and no leftover fall speed. UIManager calls this on Replay.
    public void ResetToSpawn()
    {
        // The CharacterController overwrites the transform from its own internal position,
        // so it must be off while the spawn position is written back.
        bool wasEnabled = controller.enabled;
        controller.enabled = false;

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        pitch = 0f;
        velocity = Vector3.zero;
        if (playerCamera != null)
            playerCamera.transform.localRotation = spawnCameraRotation;

        controller.enabled = wasEnabled;
    }

    private void Look()
    {
        // The sticks and arrow keys give a rate, so scale them by lookSpeed and deltaTime.
        Vector2 look = lookAction.ReadValue<Vector2>() * (lookSpeed * Time.deltaTime);

        // The mouse is only used while the look button is held. Its delta is not multiplied by deltaTime.
        if (lookHoldAction != null && lookHoldAction.IsPressed() && mouseLookAction != null)
            look += mouseLookAction.ReadValue<Vector2>() * mouseSensitivity;

        // Yaw turns the body. Pitch turns only the camera.
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        // Clamp to length 1, so diagonal walking is not faster and the speed is the same whatever the stick tilt.
        direction = Vector3.ClampMagnitude(direction, 1f) * moveSpeed;

        // A small downward speed keeps the controller on the ground when walking down slopes.
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        controller.Move((direction + Vector3.up * velocity.y) * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Standing on the ground reports a hit every frame, so a surface we can stand on is never a bump.
        if (hit.normal.y > 0.5f)
            return;

        // One bump per cooldown, so the sound and toast do not spam while the player slides along a rock.
        if (Time.time < nextBumpTime)
            return;

        nextBumpTime = Time.time + bumpCooldown;

        if (bumpSource != null && bumpClip != null)
            bumpSource.PlayOneShot(bumpClip);

        Bumped?.Invoke(hit.collider);
    }
}
