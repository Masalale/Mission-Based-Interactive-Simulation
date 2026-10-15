using UnityEngine;

// This draws the guide line from the player to the nearest animal that still needs a stamp.
// I wanted a new player to know which way to walk first, so the line points at that animal.
// The line hides itself when the passport is full, so it is never pointing at nothing.
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

        // The line only needs two points: the player and the target.
        line.positionCount = 2;
        line.useWorldSpace = true;
    }

    private void Start()
    {
        Retarget();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // I do this in LateUpdate, after the player has moved, so the line starts at the player
        // and not at where the player was earlier in the frame.
        Vector3 from = player != null ? player.position : transform.position;
        line.SetPosition(0, from + Vector3.up * heightOffset);
        line.SetPosition(1, target.GuidePosition + Vector3.up * heightOffset);
    }

    // Find the nearest animal that still needs a stamp, and show the line only if there is one.
    // GameManager calls this on every change, so the line moves on after each photo.
    public void Retarget()
    {
        target = FindNearestUnscanned();
        line.enabled = target != null;
    }

    private Animal FindNearestUnscanned()
    {
        // The line only points anywhere while a safari is actually going.
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return null;

        Vector3 from = player != null ? player.position : transform.position;
        Animal nearest = null;
        float nearestSqr = float.MaxValue;

        foreach (Animal candidate in Animal.All)
        {
            if (candidate.Scanned)
                continue;

            // I compare squared distances, because that is enough to find the closest one.
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
