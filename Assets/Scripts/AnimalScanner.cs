using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class AnimalScanner : MonoBehaviour
{
    [Header("Ray")]
    [Tooltip("Camera the ray comes from. Found on a child if left empty.")]
    [SerializeField] private Camera scanCamera;
    [Tooltip("Animal layer. Nothing selected means no scan can hit.")]
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
        if (scanCamera == null)
            scanCamera = GetComponentInChildren<Camera>();

        if (animalMask.value == 0)
            Debug.LogWarning($"{nameof(AnimalScanner)}: LayerMask unassigned: no scan can ever hit.", this);
    }

    private void Update()
    {
        // Only colours the crosshair, so the player can see what a photo would do first.
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

    // Only the press counts, not the release.
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || scanCamera == null)
            return;

        if (!Playing())
            return;

        // A stray photo must not open a fact card over the end screen buttons.
        if (UIManager.Instance != null && UIManager.Instance.IsEndScreenOpen)
            return;

        // A mouse press with a free cursor is a click on the UI, not a photo.
        if (IsMouse(context) && Cursor.lockState != CursorLockMode.Locked)
            return;

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

        if (firstPhoto)
            GameManager.Instance.AddStamp(animal);

        if (callSource != null && shutterClip != null)
            callSource.PlayOneShot(shutterClip);

        // Every photo plays the call, not only the first one.
        if (callSource != null && animal.Data.CallClip != null)
            callSource.PlayOneShot(animal.Data.CallClip);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.FlashPhoto();
            UIManager.Instance.ShowFactCard(animal.Data, firstPhoto);
        }
    }

    private Animal AimedAnimal()
    {
        Ray ray = scanCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Triggers are ignored, so the zones cannot block the shot.
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, animalMask, QueryTriggerInteraction.Ignore))
            return null;

        return hit.collider.GetComponentInParent<Animal>();
    }

    private void SetCrosshair(Color colour, string label)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.SetCrosshair(colour, label);
    }

    private static bool Playing()
    {
        return GameManager.Instance != null && GameManager.Instance.IsPlaying && !GameManager.Instance.IsPaused;
    }

    private static bool IsMouse(InputAction.CallbackContext context)
    {
        return context.control != null && context.control.device is Mouse;
    }
}