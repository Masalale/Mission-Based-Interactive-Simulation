using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person movement and look for the reserve walker.
/// Built on the New Input System only: every action is read through PlayerInput,
/// never through the legacy Input class.
/// </summary>
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

    [Header("References")]
    [Tooltip("Camera child of the player, at eye height.")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource bumpSource;
    [SerializeField] private AudioClip bumpClip;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction mouseLookAction;
    private InputAction lookHoldAction;
    private float pitch;
    private Vector3 velocity;

    /// <summary>Raised when the player walks into a solid body. UIManager shows the bump toast.</summary>
    public event Action<Collider> Bumped;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError($"{nameof(PlayerController)} needs a PlayerInput with actions assigned.", this);
            enabled = false;
            return;
        }

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

    private void Update()
    {
        if (moveAction == null || lookAction == null)
            return;

        Look();
        Move();
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
        if (bumpSource != null && bumpClip != null)
            bumpSource.PlayOneShot(bumpClip);

        Bumped?.Invoke(hit.collider);
    }
}