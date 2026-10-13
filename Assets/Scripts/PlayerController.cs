using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person movement and look for the reserve walker.
/// Built on the New Input System only: every action is read through PlayerInput,
/// never through the legacy Input class.
/// Runs before UIManager so its Start still locks the control scheme before the
/// start screen disables this component.
/// </summary>
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
    [Tooltip("Degrees per pixel for the mouse. Mouse delta is per frame already, so it is never scaled by deltaTime.")]
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

    /// <summary>Raised when the player walks into a solid body. UIManager shows the bump toast.</summary>
    public event Action<Collider> Bumped;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError($"{nameof(PlayerController)} needs a PlayerInput with actions assigned.", this);
            enabled = false;
            return;
        }

        // Replay puts the player back here, so remember the gate position, the
        // body rotation and the camera's starting look before anything moves.
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        spawnCameraRotation = playerCamera != null
            ? playerCamera.transform.localRotation
            : Quaternion.identity;

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
        if (playerInput == null || playerInput.actions == null)
            return;

        // PlayerInput is enabled by now, so the scheme can be locked.
        // The on-screen sticks add a virtual gamepad while the mouse is still
        // in use. Auto-switching would mask the bindings that do not match the
        // active device and the sticks would drop out. Locking the scheme and
        // clearing the binding mask and the device list lets keyboard, mouse and
        // the on-screen gamepad drive the one action map together.
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

    /// <summary>
    /// Puts the player back at the gate with a level camera and no leftover
    /// fall speed. UIManager calls this when Replay is pressed.
    /// </summary>
    public void ResetToSpawn()
    {
        if (controller == null)
            return;

        // The CharacterController overwrites the transform from its own internal
        // position, so it has to be off while the spawn transform is written back.
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
        // Sticks and arrow keys report a rate, so scale by lookSpeed and deltaTime.
        Vector2 look = lookAction.ReadValue<Vector2>() * (lookSpeed * Time.deltaTime);

        // The mouse already reports a per-frame delta. Scaling it by deltaTime
        // would make sensitivity depend on frame rate, so do not.
        if (lookHoldAction != null && lookHoldAction.IsPressed() && mouseLookAction != null)
        {
            look += mouseLookAction.ReadValue<Vector2>() * mouseSensitivity;
        }

        // Yaw turns the body, pitch turns the camera only.
        transform.Rotate(0f, look.x, 0f);

        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 direction = (transform.right * move.x + transform.forward * move.y);

        // Keep the horizontal component at a fixed speed regardless of stick tilt.
        direction = Vector3.ClampMagnitude(direction, 1f) * moveSpeed;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f; // small stick to the ground so slopes stay grounded
        velocity.y += gravity * Time.deltaTime;

        controller.Move((direction + Vector3.up * velocity.y) * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // The ground reports a hit every single frame the player is walking, so
        // a surface we can stand on is never a bump.
        if (hit.normal.y > 0.5f)
            return;

        // One bump per cooldown keeps the sound and the toast from spamming
        // while the player is grinding along an animal or a rock.
        if (Time.time < nextBumpTime)
            return;

        nextBumpTime = Time.time + bumpCooldown;

        if (bumpSource != null && bumpClip != null)
            bumpSource.PlayOneShot(bumpClip);

        Bumped?.Invoke(hit.collider);
    }
}