using UnityEngine;

public class ZoneRing : MonoBehaviour
{
    [Header("References")]
    [Tooltip("URP Particles Unlit. It multiplies the line colours.")]
    [SerializeField] private Material ringMaterial;
    [Tooltip("Lift in metres so the ring clears the ground.")]
    [SerializeField] private float heightOffset = 0.05f;
    [Tooltip("Metres at which the rings are fully visible.")]
    [SerializeField] private float fadeDistance = 35f;
    [SerializeField] private float maxAlpha = 0.8f;

    private const int PointCount = 64;

    // Red is wider, so it does not sink into the sand.
    private const float WhiteWidth = 0.12f;
    private const float RedWidth = 0.18f;

    private const float GroundProbeHeight = 5f;

    // Everything except layer 6, which is the animals.
    private const int GroundMask = ~(1 << 6);

    private static readonly Color WhiteRing = new Color(1f, 1f, 1f, 1f);
    private static readonly Color GoldRing = new Color(1f, 0.8f, 0.1f, 1f);
    private static readonly Color RedRing = new Color(1f, 0.1f, 0.08f, 1f);

    private Animal animal;
    private Transform player;
    private LineRenderer viewingRing;
    private LineRenderer dangerRing;

    private void Start()
    {
        animal = GetComponent<Animal>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;

        if (animal == null || animal.Data == null)
            return;

        viewingRing = BuildRing("ViewingRing", animal.Data.ViewingRadius, WhiteWidth, WhiteRing);
        dangerRing = BuildRing("DangerRing", animal.Data.DangerRadius, RedWidth, RedRing);
    }

    private void Update()
    {
        if (viewingRing == null || dangerRing == null || player == null)
            return;

        float alpha = Mathf.Clamp01(1f - Vector3.Distance(transform.position, player.position) / fadeDistance) * maxAlpha;

        SetColour(viewingRing, animal.Scanned ? GoldRing : WhiteRing, alpha);
        SetColour(dangerRing, RedRing, alpha);
    }

    private LineRenderer BuildRing(string name, float radius, float width, Color colour)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.loop = true;
        line.positionCount = PointCount;
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        if (ringMaterial != null)
            line.sharedMaterial = ringMaterial;

        Vector3 centre = transform.position;

        for (int i = 0; i < PointCount; i++)
        {
            float angle = i * Mathf.PI * 2f / PointCount;
            Vector3 point = centre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            line.SetPosition(i, new Vector3(point.x, GroundHeight(point, centre.y + heightOffset), point.z));
        }

        SetColour(line, colour, maxAlpha);
        return line;
    }

    // The ground is uneven, so each point is raised to the ground under it.
    private float GroundHeight(Vector3 point, float fallback)
    {
        Vector3 from = new Vector3(point.x, point.y + GroundProbeHeight, point.z);

        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 20f, GroundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y + heightOffset;

        // No ground under this point, so keep the animal's height.
        return fallback;
    }

    // A LineRenderer takes its alpha from the colours at its ends.
    private static void SetColour(LineRenderer line, Color colour, float alpha)
    {
        Color faded = new Color(colour.r, colour.g, colour.b, alpha);
        line.startColor = faded;
        line.endColor = faded;
    }
}