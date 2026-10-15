using UnityEngine;

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

        // LateUpdate, so the line starts where the player is after moving.
        Vector3 from = player != null ? player.position : transform.position;
        line.SetPosition(0, from + Vector3.up * heightOffset);
        line.SetPosition(1, target.GuidePosition + Vector3.up * heightOffset);
    }

    // GameManager calls this after each change, so the line moves on after a photo.
    public void Retarget()
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
