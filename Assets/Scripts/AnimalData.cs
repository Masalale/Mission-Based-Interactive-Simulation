using UnityEngine;

[CreateAssetMenu(fileName = "SO_Animal", menuName = "Safari/Animal Data")]
public class AnimalData : ScriptableObject
{
    [Header("Passport")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string habitat;
    [SerializeField] private string status;
    [SerializeField, TextArea(3, 8)] private string fact;

    [Header("Safe viewing")]
    [SerializeField, TextArea(2, 6)] private string viewingTip;
    [SerializeField] private string warningLine;

    [Header("Zones and audio")]
    [SerializeField] private float viewingRadius = 15f;
    [SerializeField] private float dangerRadius = 6f;
    [SerializeField] private AudioClip callClip;

    // The save file keys on this id, so never change it after the asset is made.
    public string Id => id;
    public string DisplayName => displayName;
    public string Habitat => habitat;
    public string Status => status;
    public string Fact => fact;
    public string ViewingTip => viewingTip;
    public string WarningLine => warningLine;
    public float ViewingRadius => viewingRadius;
    public float DangerRadius => dangerRadius;
    public AudioClip CallClip => callClip;
}