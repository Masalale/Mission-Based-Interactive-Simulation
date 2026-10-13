using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One Big Five animal on the prefab root. It owns the AnimalData slot, keeps
/// the runtime state (scanned, viewing, breaches) that must never be written
/// back to the asset, and copies the zone radii into the child colliders.
/// </summary>
public class Animal : MonoBehaviour
{
    // Every live animal registers itself so menus, the guide line and the
    // save system can search the reserve without any Find calls in Update.
    private static readonly List<Animal> all = new List<Animal>();

    /// <summary>All enabled animals in the scene. Read-only for every caller.</summary>
    public static IReadOnlyList<Animal> All => all;

    [Header("Data")]
    [Tooltip("Species asset for this animal, e.g. SO_Lion.")]
    [SerializeField] private AnimalData data;

    [Header("Zones")]
    [Tooltip("Trigger sphere that lets the player scan safely.")]
    [SerializeField] private SphereCollider viewingCollider;
    [Tooltip("Trigger sphere that costs points.")]
    [SerializeField] private SphereCollider dangerCollider;

    [Header("Guide line")]
    [Tooltip("Where the SafariGuideLine aims, usually the base of the model.")]
    [SerializeField] private Transform guidePoint;

    /// <summary>Species asset. Can be null if the prefab was not set up.</summary>
    public AnimalData Data => data;

    /// <summary>True once a stamp has been collected for this animal.</summary>
    public bool Scanned { get; private set; }

    /// <summary>True while the player stands inside the viewing zone. The trigger owns this.</summary>
    public bool InViewing { get; set; }

    /// <summary>How many times the player has broken this animal's danger zone.</summary>
    public int Breaches { get; private set; }

    /// <summary>Where the guide line should end. Falls back to the root if unassigned.</summary>
    public Vector3 GuidePosition => guidePoint != null ? guidePoint.position : transform.position;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{nameof(Animal)} on {name} has no AnimalData assigned.", this);
            return;
        }

        // Copy the radii out of the asset so a ScriptableObject is never written to
        // at runtime and every animal of the same species shares one source of truth.
        if (viewingCollider != null)
            viewingCollider.radius = data.ViewingRadius;

        if (dangerCollider != null)
            dangerCollider.radius = data.DangerRadius;
    }

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    /// <summary>Called by GameManager when a stamp is collected.</summary>
    public void MarkScanned()
    {
        Scanned = true;
    }

    /// <summary>Called by GameManager when the player breaks this animal's danger zone.</summary>
    public void RegisterBreach()
    {
        Breaches++;
    }

    /// <summary>
    /// Clears the per-run state for a new safari. Deliberately not named Reset:
    /// Unity calls Reset() as an editor message and never at runtime, so the
    /// project would silently skip it during play.
    /// </summary>
    public void ResetState()
    {
        Scanned = false;
        Breaches = 0;
        // InViewing is left alone: the viewing zone trigger owns it.
    }
}