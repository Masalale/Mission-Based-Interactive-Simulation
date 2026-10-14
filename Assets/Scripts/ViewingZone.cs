using UnityEngine;

// This is the trigger on the ViewingZone child of an animal prefab.
// It is the sphere where the player is allowed to scan from.
public class ViewingZone : MonoBehaviour
{
    [Tooltip("The animal this zone belongs to. Found on the parent when left empty.")]
    [SerializeField] private Animal animal;

    private void Awake()
    {
        // The animal sits on the prefab root and this trigger is a child of it.
        if (animal == null)
            animal = GetComponentInParent<Animal>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only the player counts. Scenery moving through the zone does not.
        if (!other.CompareTag("Player"))
            return;

        if (animal == null)
            return;

        animal.InViewing = true;

        // I stay quiet for an animal that is already in the passport, because the hint is not needed again.
        if (animal.Scanned || animal.Data == null || UIManager.Instance == null)
            return;

        // I use the species name from the data asset and not the object name,
        // so the hint reads the same in the prefab and in the scene.
        string displayName = animal.Data.DisplayName;
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;

        UIManager.Instance.ShowHint("Aim at the " + displayName + " and press Scan");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (animal == null)
            return;

        // Stepping out clears the flag, so scanning from outside the zone is not allowed.
        animal.InViewing = false;

        if (UIManager.Instance != null)
            UIManager.Instance.HideHint();
    }
}
