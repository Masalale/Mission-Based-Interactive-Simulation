using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animal : MonoBehaviour
{
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

    [Header("Reaction")]
    [Tooltip("Degrees per second the animal turns to face me.")]
    [SerializeField] private float turnSpeed = 25f;
    [Tooltip("Metres the animal lunges when it startles.")]
    [SerializeField] private float lungeDistance = 1.2f;
    [SerializeField] private float lungeSeconds = 0.15f;
    [SerializeField] private float pauseSeconds = 0.3f;
    [SerializeField] private float returnSeconds = 0.6f;

    public AnimalData Data => data;
    public bool Scanned { get; private set; }

    public bool IsStartled { get; private set; }

    // Set when the player breaks this animal's danger zone.
    public bool Disturbed { get; private set; }

    // The ViewingZone trigger sets this, not this script.
    public bool InViewing { get; set; }

    public Vector3 GuidePosition => guidePoint != null ? guidePoint.position : transform.position;

    private Transform player;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{nameof(Animal)} on {name} has no AnimalData assigned.", this);
            return;
        }

        // I copy the radii onto the colliders. The asset is never written to while playing.
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

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void Update()
    {
        if (!InViewing || IsStartled || player == null)
            return;

        Vector3 towards = player.position - transform.position;
        towards.y = 0f;

        // LookRotation fails on a zero vector, so I stop if I am standing on the animal.
        if (towards.sqrMagnitude < 0.01f)
            return;

        Quaternion wanted = Quaternion.LookRotation(towards.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * Time.deltaTime);
    }

    public void MarkScanned()
    {
        Scanned = true;
    }

    public void MarkDisturbed()
    {
        Disturbed = true;
    }

    // Not named Reset: Unity would call a method with that name as an editor message.
    public void ResetState()
    {
        Scanned = false;
        Disturbed = false;
    }

    public void Startle(Vector3 playerPosition)
    {
        if (IsStartled)
            return;

        StartCoroutine(StartleRoutine(playerPosition));
    }

    private IEnumerator StartleRoutine(Vector3 playerPosition)
    {
        IsStartled = true;

        Vector3 start = transform.position;
        Vector3 towards = new Vector3(playerPosition.x - start.x, 0f, playerPosition.z - start.z);

        // Flat lunge: the animal slides along the ground instead of up a slope.
        Vector3 step = towards.sqrMagnitude > 0.01f ? towards.normalized * lungeDistance : Vector3.zero;

        // Stop 2 m short of the player, so a charge never reaches them.
        float room = towards.magnitude - 2f;
        step *= Mathf.Clamp01(room / lungeDistance);

        yield return MoveOver(lungeSeconds, start, start + step);
        yield return new WaitForSeconds(pauseSeconds);
        yield return MoveOver(returnSeconds, start + step, start);

        transform.position = start;
        IsStartled = false;
    }

    private IEnumerator MoveOver(float seconds, Vector3 from, Vector3 to)
    {
        float timer = 0f;

        while (timer < seconds)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(timer / seconds));
            yield return null;
        }
    }
}