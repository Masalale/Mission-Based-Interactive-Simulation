using UnityEngine;

// The too-close trigger on the DangerZone child of an animal prefab.
// It warns at once, but books a breach at most once per cooldown per animal.
public class DangerZone : MonoBehaviour
{
    [Tooltip("The animal this zone belongs to. Found on the parent when left empty.")]
    [SerializeField] private Animal animal;

    [Tooltip("Seconds before this same animal can book another breach.")]
    [SerializeField] private float breachCooldown = 3f;

    private float nextBreachTime;

    private void Awake()
    {
        if (animal == null)
            animal = GetComponentInParent<Animal>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // The warning shows every time, even during the cooldown, because the player still needs to know.
        if (UIManager.Instance != null)
            UIManager.Instance.ShowWarning("Too close, back away");

        if (GameManager.Instance == null || Time.time < nextBreachTime)
            return;

        if (animal == null)
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
}
