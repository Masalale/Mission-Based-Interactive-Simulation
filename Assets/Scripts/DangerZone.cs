using UnityEngine;

// This is the trigger on the DangerZone child of an animal prefab.
// Walking into it means the player is too close.
// I warn straight away, but I only count one breach per animal every few seconds,
// so leaning against the zone does not drain the score.
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

        // The warning shows every time I walk in, even during the cooldown,
        // because the player still has to be told they are too close.
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
