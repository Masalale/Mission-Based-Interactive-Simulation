using UnityEngine;
using UnityEngine.InputSystem;

// This is the script that does the scanning.
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
    [Tooltip("Plays the animal call on a first scan.")]
    [SerializeField] private AudioSource callSource;

    private InputAction interactAction;

    private void Awake()
    {
        // Use the camera under the player if I did not set one by hand.
        if (scanCamera == null)
            scanCamera = GetComponentInChildren<Camera>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput.actions != null)
            interactAction = playerInput.actions.FindAction("Player/Interact", throwIfNotFound: false);

        // Without a layer mask every ray misses, and that looks like a broken scanner rather than a missing reference.
        if (animalMask.value == 0)
            Debug.LogWarning($"{nameof(AnimalScanner)}: LayerMask unassigned: no scan can ever hit.", this);
    }

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.performed += OnInteract;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteract;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (scanCamera == null)
            return;

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;

        // The middle of the camera view is where the player is looking, the same point as the crosshair.
        Ray ray = scanCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // I ignore the triggers, so the viewing and danger zones cannot block the shot.
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, animalMask, QueryTriggerInteraction.Ignore))
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("Nothing in your sights. Aim at an animal.");
            return;
        }

        Animal animal = hit.collider.GetComponentInParent<Animal>();
        if (animal == null || animal.Data == null)
            return;

        if (!animal.InViewing)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowToast("Move into the viewing zone first.");
            return;
        }

        bool firstScan = !animal.Scanned;

        // Only the first scan of an animal earns a stamp.
        if (firstScan)
            GameManager.Instance.AddStamp(animal);

        // I play the call every time, so the animal answers a repeat scan as well.
        if (callSource != null && animal.Data.CallClip != null)
            callSource.PlayOneShot(animal.Data.CallClip);

        // A repeat scan costs nothing. It only reopens the card so the fact can be read again.
        if (UIManager.Instance != null)
            UIManager.Instance.ShowFactCard(animal.Data, firstScan);
    }

    private void OnDrawGizmosSelected()
    {
        if (scanCamera == null)
            return;

        // A yellow line in the Scene view, so I can see how far the scan reaches.
        Gizmos.color = Color.yellow;
        Vector3 from = scanCamera.transform.position;
        Gizmos.DrawLine(from, from + scanCamera.transform.forward * maxDistance);
    }
}
