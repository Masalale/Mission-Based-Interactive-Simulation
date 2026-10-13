using UnityEngine;

// The facts about one Big Five species. Read only at runtime, never written to.
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

    [Header("Zones and audio")]
    [SerializeField] private float viewingRadius = 15f;
    [SerializeField] private float dangerRadius = 6f;
    [SerializeField] private AudioClip callClip;

    public string Id => id;
    public string DisplayName => displayName;
    public string Habitat => habitat;
    public string Status => status;
    public string Fact => fact;
    public string JobRole => jobRole;
    public string JobDescription => jobDescription;
    public float ViewingRadius => viewingRadius;
    public float DangerRadius => dangerRadius;
    public AudioClip CallClip => callClip;
}
