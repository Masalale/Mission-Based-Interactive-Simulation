using UnityEngine;

/// <summary>
/// Author-editable facts about one Big Five species: what the fact card shows,
/// the two zone radii, and the call clip. This asset is never written to at
/// runtime, Animal copies the values it needs in Awake.
/// </summary>
[CreateAssetMenu(fileName = "SO_Animal", menuName = "Safari/Animal Data")]
public class AnimalData : ScriptableObject
{
    [Header("Passport")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string habitat;
    [SerializeField] private string status;
    [SerializeField, TextArea(3, 8)] private string fact;

    [Header("Job supported")]
    [SerializeField] private string jobRole;
    [SerializeField, TextArea(3, 8)] private string jobDescription;

    [Header("Zones")]
    [Tooltip("Radius in metres of the ViewingZone trigger.")]
    [SerializeField] private float viewingRadius = 15f;
    [Tooltip("Radius in metres of the DangerZone trigger.")]
    [SerializeField] private float dangerRadius = 6f;

    [Header("Audio")]
    [SerializeField] private AudioClip callClip;

    /// <summary>Stable key used by SaveSystem, never shown to the player.</summary>
    public string Id => id;

    /// <summary>Species name used by the passport, hint and fact card.</summary>
    public string DisplayName => displayName;

    /// <summary>Where the species is found, shown on the fact card.</summary>
    public string Habitat => habitat;

    /// <summary>Conservation status, shown on the fact card.</summary>
    public string Status => status;

    /// <summary>The one fact the visitor learns when scanning this animal.</summary>
    public string Fact => fact;

    /// <summary>Short job title this animal supports, e.g. "Game guide".</summary>
    public string JobRole => jobRole;

    /// <summary>Sentence explaining how that job depends on healthy wildlife.</summary>
    public string JobDescription => jobDescription;

    /// <summary>Viewing distance in metres, copied into the ViewingZone collider.</summary>
    public float ViewingRadius => viewingRadius;

    /// <summary>Too-close distance in metres, copied into the DangerZone collider.</summary>
    public float DangerRadius => dangerRadius;

    /// <summary>Call played once on the first scan. May be null on an unfinished asset.</summary>
    public AudioClip CallClip => callClip;
}