using UnityEngine;

/// <summary>
/// The too-close trigger on the DangerZone child of the animal prefab.
/// It warns the player at once and books a breach, but only one breach every
/// few seconds per animal so standing in the zone does not drain the score.
/// </summary>
public class DangerZone : MonoBehaviour
{
    [Header("Owner")]
    [Tooltip("The animal this zone belongs to. Found on the parent when left empty.")]
    [SerializeField] private Animal animal;

    [Header("Rules")]
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

        // The warning shows every time, even during the cooldown, because the
        // player still needs to know they are too close.
        if (UIManager.Instance != null)
            UIManager.Instance.ShowWarning("Too close, back away");

        if (GameManager.Instance == null)
            return;

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
}