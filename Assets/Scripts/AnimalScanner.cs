using UnityEngine;
using UnityEngine.InputSystem;

// This is the script that does the scanning.
// The scan is shown to the player as taking a photo, and Interact is the shutter button.
// I shoot one ray from the middle of the screen when the player presses Interact.
// A hit animal inside its viewing zone gives a stamp.
// AnimalScanner is the only caller of GameManager.AddStamp, so the stamp is only ever earned here.
[RequireComponent(typeof(PlayerInput))]
public class AnimalScanner : MonoBehaviour
{
    [Header("Ray")]
    [Tooltip("Camera the ray comes from. Found on a child if left empty.")]
    [SerializeField] private Camera scanCamera;
    [Tooltip("Animal layer. With Nothing selected no scan can ever hit.")]
    [SerializeField] private LayerMask animalMask;
    [Tooltip("Maximum scan distance in metres. Must be at least the viewing radius.")]
    [SerializeField] private float maxDistance = 20f;

    [Header("Audio")]
    [Tooltip("Plays the shutter click and then the animal call, on every photo.")]
    [SerializeField] private AudioSource callSource;
    [Tooltip("The short camera click that goes before the animal answers.")]
    [SerializeField] private AudioClip shutterClip;

    private void Awake()
    {
        // Use the camera under the player if I did not set one by hand.
        if (scanCamera == null)
            scanCamera = GetComponentInChildren<Camera>();

        // Without a layer mask every ray misses, and that looks like a broken scanner rather than a missing reference.
        if (animalMask.value == 0)
            Debug.LogWarning($"{nameof(AnimalScanner)}: LayerMask unassigned: no scan can ever hit.", this);
    }

    private void Update()
    {
        // I cast the same ray every frame while the safari runs, only to colour the crosshair.
        // Nothing is scored here, so the player can see what the photo will do before pressing the button.
        if (!Playing() || scanCamera == null)
        {
            SetCrosshair(UIManager.CrosshairNormal, string.Empty);
            return;
        }

        Animal aimed = AimedAnimal();

        if (aimed == null)
            SetCrosshair(UIManager.CrosshairNormal, string.Empty);
        else if (aimed.Data != null && aimed.InViewing)
            SetCrosshair(UIManager.CrosshairReady, aimed.Data.DisplayName);
        else
            SetCrosshair(UIManager.CrosshairTooFar, "Get closer");
    }

    // The Interact action on the player calls this. Only the press counts, not the release.
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || scanCamera == null)
            return;

        if (!Playing())
            return;

        // The end screen has its own buttons, so a stray photo must not open a fact card over them.
        if (UIManager.Instance != null && UIManager.Instance.IsEndScreenOpen)
            return;

        // A mouse press while the cursor is free was a click on the UI, so it is not a photo.
        if (IsMouse(context) && Cursor.lockState != CursorLockMode.Locked)
            return;

        // Interact also closes the fact card, so the player can read it and put it away without a mouse.
        if (UIManager.Instance != null && UIManager.Instance.IsFactCardOpen)
        {
            UIManager.Instance.HideFactCard();
            return;
        }

        Animal animal = AimedAnimal();

        if (animal == null || animal.Data == null)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("Nothing in frame. Aim at an animal.");
            return;
        }

        if (!animal.InViewing)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("Too far for a good photo. Step inside the ranger's circle.");
            return;
        }

        bool firstPhoto = !animal.Scanned;

        // Only the first photo of an animal earns a stamp.
        if (firstPhoto)
            GameManager.Instance.AddStamp(animal);

        // The shutter goes first, so a photo sounds like a camera and not like a menu.
        if (callSource != null && shutterClip != null)
            callSource.PlayOneShot(shutterClip);

        // The call plays on every photo, so the animal answers a repeat photo as well.
        if (callSource != null && animal.Data.CallClip != null)
            callSource.PlayOneShot(animal.Data.CallClip);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.FlashPhoto();
            UIManager.Instance.ShowFactCard(animal.Data, firstPhoto);
        }
    }

    // The animal in the middle of the screen, or null. Both the crosshair and the photo use this.
    private Animal AimedAnimal()
    {
        // The middle of the camera view is where the player is looking, the same point as the crosshair.
        Ray ray = scanCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // I ignore the triggers, so the viewing and danger zones cannot block the shot.
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, animalMask, QueryTriggerInteraction.Ignore))
            return null;

        return hit.collider.GetComponentInParent<Animal>();
    }

    private void SetCrosshair(Color colour, string label)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.SetCrosshair(colour, label);
    }

    // True only while a safari is running and not sitting on the pause menu.
    private static bool Playing()
    {
        return GameManager.Instance != null && GameManager.Instance.IsPlaying && !GameManager.Instance.IsPaused;
    }

    // A mouse press only means a photo while the mouse is locked to the game.
    private static bool IsMouse(InputAction.CallbackContext context)
    {
        return context.control != null && context.control.device is Mouse;
    }

    private void OnDrawGizmosSelected()
    {
        if (scanCamera == null)
            return;

        // A yellow line in the Scene view, so I can see how far the photo ray reaches.
        Gizmos.color = Color.yellow;
        Vector3 from = scanCamera.transform.position;
        Gizmos.DrawLine(from, from + scanCamera.transform.forward * maxDistance);
    }
}