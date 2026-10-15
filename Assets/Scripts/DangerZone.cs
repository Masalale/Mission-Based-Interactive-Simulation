using UnityEngine;

// Respect is taken once per cooldown, so leaning on the zone does not drain it.
public class DangerZone : MonoBehaviour
{
    [Tooltip("The animal this zone belongs to. Found on the parent when left empty.")]
    [SerializeField] private Animal animal;

    [Tooltip("Seconds before the same animal costs Respect again.")]
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

        // The warning shows on every entry, even during the cooldown.
        Warn();

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