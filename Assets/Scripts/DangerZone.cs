using UnityEngine;

// This is the trigger on the DangerZone child of an animal prefab.
// Walking into it means the player is too close.
// The animal always reacts, but I only count one disturbance per animal every few seconds,
// so leaning against the zone does not drain the Respect score.
public class DangerZone : MonoBehaviour
{
    [Tooltip("The animal this zone belongs to. Found on the parent when left empty.")]
    [SerializeField] private Animal animal;

    [Tooltip("Seconds before this same animal can book another disturbance.")]
    [SerializeField] private float breachCooldown = 3f;

    [Tooltip("Seconds the camera shakes for when the player is too close.")]
    [SerializeField] private float shakeSeconds = 0.3f;

    private PlayerController player;
    private float nextBreachTime;

    private void Awake()
    {
        if (animal == null)
            animal = GetComponentInParent<Animal>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.GetComponent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;

        // The warning shows every time I walk in, even during the cooldown,
        // because the player still has to be told they are too close.
        Warn();

        // The animal calls, lunges at me and settles back, so the disturbance can be felt.
        if (animal != null)
        {
            if (animal.Data != null && animal.Data.CallClip != null)
                AudioSource.PlayClipAtPoint(animal.Data.CallClip, transform.position, 1f);

            animal.Startle(other.transform.position);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.FlashDanger();

        if (player != null)
            player.Shake(shakeSeconds);

        // Only the first disturbance in the cooldown costs Respect.
        if (Time.time < nextBreachTime)
            return;

        nextBreachTime = Time.time + breachCooldown;
        GameManager.Instance.RegisterBreach(animal);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (UIManager.Instance != null)
            UIManager.Instance.HideWarning();
    }

    // Ranger Amani reads the animal's own warning line, with a plain one as a fallback.
    private void Warn()
    {
        if (UIManager.Instance == null)
            return;

        string line = animal != null && animal.Data != null ? animal.Data.WarningLine : null;

        if (string.IsNullOrEmpty(line))
            line = "You are too close. Back away slowly.";

        UIManager.Instance.ShowWarning("Ranger Amani: " + line);
    }
}