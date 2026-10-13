using UnityEngine;

// The guide line from the player to the nearest animal that still needs a stamp.
// It hides itself when the passport is full, so the hunt is never aimless.
[RequireComponent(typeof(LineRenderer))]
public class SafariGuideLine : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Where the line starts, usually the player.")]
    [SerializeField] private Transform player;
    [Tooltip("Lift in metres so the line clears the ground and the animal's feet.")]
    [SerializeField] private float heightOffset = 0.3f;

    private LineRenderer line;
    private Animal target;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();

        // Two points only: the start (the player) and the end (the target).
        line.positionCount = 2;
        line.useWorldSpace = true;
    }

    private void Start()
    {
        Retarget();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStampAdded += HandleStampAdded;
            GameManager.Instance.OnStateChanged += Retarget;
        }

        // A line that is re-enabled in the middle of a run must find its target again.
        Retarget();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStampAdded -= HandleStampAdded;
            GameManager.Instance.OnStateChanged -= Retarget;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Updated every frame, so the line follows the player while they walk.
        Vector3 from = player != null ? player.position : transform.position;
        line.SetPosition(0, from + Vector3.up * heightOffset);
        line.SetPosition(1, target.GuidePosition + Vector3.up * heightOffset);
    }

    private void HandleStampAdded(Animal animal)
    {
        // The animal that was just stamped is no longer the target.
        Retarget();
    }

    // Picks the nearest unscanned animal and shows the line only when there is one.
    private void Retarget()
    {
        target = FindNearestUnscanned();
        line.enabled = target != null;
    }

    private Animal FindNearestUnscanned()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return null;

        Vector3 from = player != null ? player.position : transform.position;
        Animal nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (Animal candidate in Animal.All)
        {
            if (candidate.Scanned)
                continue;

            // Squared distance is enough to compare, and skips a square root per animal.
            float sqr = (candidate.GuidePosition - from).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = candidate;
            }
        }

        return nearest;
    }
}
