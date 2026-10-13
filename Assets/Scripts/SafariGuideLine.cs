using UnityEngine;

/// <summary>
/// The guide line drawn from the player to the nearest animal that still needs
/// a stamp. It retargets whenever a stamp is collected or the state changes, and
/// hides itself when the passport is full, so the hunt is never aimless.
/// </summary>
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

        if (line == null)
            return;

        // Two points only: the player's feet and the target's base.
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

        // A line that is re-enabled mid run must find its target again.
        Retarget();
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnStampAdded -= HandleStampAdded;
        GameManager.Instance.OnStateChanged -= Retarget;
    }

    private void LateUpdate()
    {
        if (line == null || target == null)
            return;

        Vector3 from = player != null ? player.position : transform.position;
        line.SetPosition(0, from + Vector3.up * heightOffset);
        line.SetPosition(1, target.GuidePosition + Vector3.up * heightOffset);
    }

    private void HandleStampAdded(Animal animal)
    {
        // The animal that was just stamped is the one we no longer follow.
        Retarget();
    }

    /// <summary>Picks the nearest unscanned animal and shows or hides the line.</summary>
    private void Retarget()
    {
        target = FindNearestUnscanned();
        if (line != null)
            line.enabled = target != null;
    }

    private Animal FindNearestUnscanned()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return null;

        Vector3 from = player != null ? player.position : transform.position;
        Animal nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < Animal.All.Count; i++)
        {
            Animal candidate = Animal.All[i];

            if (candidate == null || candidate.Scanned)
                continue;

            // Squared distance avoids a square root per animal per retarget.
            float sqr = (candidate.GuidePosition - from).sqrMagnitude;
            if (sqr >= nearestSqr)
                continue;

            nearestSqr = sqr;
            nearest = candidate;
        }

        return nearest;
    }
}