using UnityEngine;
using UnityEngine.InputSystem;

// The hero script. On Interact, one ray from the screen centre turns a hit animal into a stamp.
// This is the only place in the project that calls GameManager.AddStamp.
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
        if (scanCamera == null)
            scanCamera = GetComponentInChildren<Camera>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput.actions != null)
            interactAction = playerInput.actions.FindAction("Player/Interact", throwIfNotFound: false);

        // Without a mask every ray misses, which looks like a broken scanner rather than a missing reference.
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

        Ray ray = scanCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Ignore the triggers, so the ViewingZone and DangerZone cannot block the shot.
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

        if (firstScan)
            GameManager.Instance.AddStamp(animal);

        // The animal answers every scan with its call, not only the first one.
        if (callSource != null && animal.Data.CallClip != null)
            callSource.PlayOneShot(animal.Data.CallClip);

        // A repeat scan costs nothing and only reopens the card, so the fact can be read again.
        if (UIManager.Instance != null)
            UIManager.Instance.ShowFactCard(animal.Data, firstScan);
    }

    private void OnDrawGizmosSelected()
    {
        if (scanCamera == null)
            return;

        Gizmos.color = Color.yellow;
        Vector3 from = scanCamera.transform.position;
        Gizmos.DrawLine(from, from + scanCamera.transform.forward * maxDistance);
    }
}
