using System.Collections.Generic;
using UnityEngine;

// One Big Five animal. Holds its AnimalData and the state of the current run.
public class Animal : MonoBehaviour
{
    // Every enabled animal adds itself here, so the guide line and the save can find them.
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

    // Set by the ViewingZone trigger, not by this script.
    public bool InViewing { get; set; }

    public int Breaches { get; private set; }

    public Vector3 GuidePosition => guidePoint != null ? guidePoint.position : transform.position;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{nameof(Animal)} on {name} has no AnimalData assigned.", this);
            return;
        }

        // Copy the radii from the asset. The asset itself is never written to at runtime.
        viewingCollider.radius = data.ViewingRadius;
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

    public void RegisterBreach()
    {
        Breaches++;
    }

    // Not named Reset: Unity calls Reset() only in the Editor, never in Play mode.
    public void ResetState()
    {
        Scanned = false;
        Breaches = 0;
    }
}
