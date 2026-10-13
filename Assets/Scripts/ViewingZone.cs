using System;
using UnityEngine;

/// <summary>
/// The safe-viewing trigger on the ViewingZone child of the animal prefab.
/// While the player stands inside it, scanning is allowed and a hint explains
/// the next action. The radius comes from AnimalData via Animal.Awake.
/// </summary>
public class ViewingZone : MonoBehaviour
{
    [Header("Owner")]
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

        // An already stamped animal has nothing left to teach, so stay quiet.
        if (animal.Scanned || animal.Data == null || UIManager.Instance == null)
            return;

        // The hint uses the species name, not the GameObject name, so it reads
        // the same on the prefab and in the scene.
        string displayName = animal.Data.DisplayName;
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;

        UIManager.Instance.ShowHint("Aim at the " + displayName + " and press Scan");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (animal != null)
            animal.InViewing = false;

        if (UIManager.Instance != null)
            UIManager.Instance.HideHint();
    }
}