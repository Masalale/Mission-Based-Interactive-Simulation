using UnityEngine;

[CreateAssetMenu(fileName = "SO_Animal", menuName = "Safari/Animal Data")]
public class AnimalData : ScriptableObject
{
    [Header("Passport")]
    [SerializeField] private string displayName;
    [SerializeField] private string swahiliName;
    [SerializeField] private string places;
    [SerializeField] private string warningSigns;

    [Header("Safe viewing")]
    [SerializeField] private string doTip;
    [SerializeField] private string dontTip;
    [SerializeField] private string warningLine;

    [Header("Zones and audio")]
    [SerializeField] private float viewingRadius = 15f;
    [SerializeField] private float dangerRadius = 6f;
    [SerializeField] private AudioClip callClip;

    public string DisplayName => displayName;
    public string SwahiliName => swahiliName;
    public string Places => places;
    public string WarningSigns => warningSigns;
    public string DoTip => doTip;
    public string DontTip => dontTip;
    public string WarningLine => warningLine;
    public float ViewingRadius => viewingRadius;
    public float DangerRadius => dangerRadius;
    public AudioClip CallClip => callClip;
}
