using System.Collections.Generic;
using UnityEngine;

// One animal of the Big Five.
// It keeps a link to its AnimalData asset and to the state of the run in progress.
public class Animal : MonoBehaviour
{
    // Every animal that is switched on puts itself in this list, so the guide line and the save
    // can go through all of them without finding them in the scene by hand.
    private static readonly List<Animal> all = new List<Animal>();

    public static IReadOnlyList<Animal> All => all;

    [Header("Data")]
    [Tooltip("Species asset for this animal, e.g. SO_Lion.")]
    [SerializeField] private AnimalData data;

    [Header("Zones")]
    [SerializeField] private SphereCollider viewingCollider;
    [SerializeField] private SphereCollider dangerCollider;

    [Header("Guide line")]
    [Tooltip("Where the guide line ends, usually the base of the model.")]
    [SerializeField] private Transform guidePoint;

    public AnimalData Data => data;
    public bool Scanned { get; private set; }

    // The ViewingZone trigger sets this, not this script.
    public bool InViewing { get; set; }

    public Vector3 GuidePosition => guidePoint != null ? guidePoint.position : transform.position;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{nameof(Animal)} on {name} has no AnimalData assigned.", this);
            return;
        }

        // I take the two radii from the asset and set them on the colliders here.
        // The asset itself is never written to while the game is running.
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

    public void MarkScanned()
    {
        Scanned = true;
    }

    // I did not call this Reset, because Unity would run Reset() as an editor message, not from my code.
    public void ResetState()
    {
        Scanned = false;

        // I leave InViewing alone, because the ViewingZone trigger owns that flag.
    }
}
