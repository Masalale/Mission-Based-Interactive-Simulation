using System.Collections;
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

    [Header("Reaction")]
    [Tooltip("How fast the animal turns its head towards me, in degrees per second.")]
    [SerializeField] private float turnSpeed = 25f;
    [Tooltip("How far the animal lunges when it startles, in metres.")]
    [SerializeField] private float lungeDistance = 1.2f;
    [SerializeField] private float lungeSeconds = 0.15f;
    [SerializeField] private float pauseSeconds = 0.3f;
    [SerializeField] private float returnSeconds = 0.6f;

    public AnimalData Data => data;
    public bool Scanned { get; private set; }

    // True while the animal is doing its bluff charge, so it stops turning to look at me.
    public bool IsStartled { get; private set; }

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

    private void Start()
    {
        // I look the player up once by tag, so I do not search the scene every frame.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void Update()
    {
        // The animal notices me. While I am inside the ranger's circle it slowly turns to face me.
        if (!InViewing || IsStartled || player == null)
            return;

        Vector3 towards = player.position - transform.position;
        towards.y = 0f;

        // Nothing to turn towards if I am standing on the animal.
        if (towards.sqrMagnitude < 0.01f)
            return;

        Quaternion wanted = Quaternion.LookRotation(towards.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * Time.deltaTime);
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

    // The animal reacts to a disturbance: a short bluff charge at the player, then it goes back.
    // DangerZone calls this every time I walk into the red circle.
    public void Startle(Vector3 playerPosition)
    {
        if (IsStartled)
            return;

        StartCoroutine(StartleRoutine(playerPosition));
    }

    private IEnumerator StartleRoutine(Vector3 playerPosition)
    {
        IsStartled = true;

        // I remember where I was standing so the charge can never leave me anywhere else.
        Vector3 start = transform.position;
        Vector3 towards = new Vector3(playerPosition.x - start.x, 0f, playerPosition.z - start.z);

        // The lunge is flat, so I slide along the ground instead of hopping up a slope.
        Vector3 step = towards.sqrMagnitude > 0.01f ? towards.normalized * lungeDistance : Vector3.zero;

        // And it stops well short of the player, so a charge can never actually reach them.
        float room = towards.magnitude - 2f;
        step *= Mathf.Clamp01(room / lungeDistance);

        // I lunge, I pause, and I walk back to exactly where I started.
        yield return MoveOver(lungeSeconds, start, start + step);
        yield return new WaitForSeconds(pauseSeconds);
        yield return MoveOver(returnSeconds, start + step, start);

        transform.position = start;
        IsStartled = false;
    }

    // I slide the animal from one point to another over a number of seconds.
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