using UnityEngine;

public class ViewingZone : MonoBehaviour
{
    [Tooltip("The animal this zone belongs to. Found on the parent when left empty.")]
    [SerializeField] private Animal animal;

    private void Awake()
    {
        if (animal == null)
            animal = GetComponentInParent<Animal>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (animal == null)
            return;

        animal.InViewing = true;

        if (animal.Scanned || animal.Data == null || UIManager.Instance == null)
            return;

        // I use the species name, not the object name, so the hint reads the same everywhere.
        string displayName = animal.Data.DisplayName;
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;

        UIManager.Instance.ShowHint("Ranger Amani: Good distance. Aim at the " + displayName + " and take your photo.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (animal == null)
            return;

        animal.InViewing = false;

        if (UIManager.Instance != null)
            UIManager.Instance.HideHint();
    }
}
