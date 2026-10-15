using UnityEngine;

// This draws the ranger's circle on the ground around one animal.
// The white ring is the viewing distance and the red ring is the danger distance,
// and both radii come from the animal's own AnimalData asset.
// The rings fade in as the player walks closer, so the savanna is not full of circles.
public class ZoneRing : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Unlit material for the rings, so the colour gradient can fade them.")]
    [SerializeField] private Material ringMaterial;
    [Tooltip("Lift in metres so the ring clears the ground.")]
    [SerializeField] private float heightOffset = 0.05f;
    [Tooltip("Metres at which the rings are fully visible.")]
    [SerializeField] private float fadeDistance = 35f;
    [SerializeField] private float maxAlpha = 0.8f;

    // A flat circle of 64 points is smooth enough at these radii.
    private const int PointCount = 64;

    // The red circle is wider than the white one, so it reads on the sand instead of sinking into it.
    private const float WhiteWidth = 0.12f;
    private const float RedWidth = 0.18f;

    // How far above each point I start looking for the ground.
    private const float GroundProbeHeight = 5f;

    // The ground is everything except the animals themselves.
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

        // I look the player up once by tag, so I do not search the scene every frame.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;

        if (animal == null || animal.Data == null)
            return;

        // These two circles are the ranger's circle for this animal.
        viewingRing = BuildRing("ViewingRing", animal.Data.ViewingRadius, WhiteWidth, WhiteRing);
        dangerRing = BuildRing("DangerRing", animal.Data.DangerRadius, RedWidth, RedRing);
    }

    private void Update()
    {
        if (viewingRing == null || dangerRing == null || player == null)
            return;

        // I fade the rings in as I get closer, so the far animals do not clutter the savanna.
        float alpha = Mathf.Clamp01(1f - Vector3.Distance(transform.position, player.position) / fadeDistance) * maxAlpha;

        // The white ring turns gold once the animal is in the passport.
        SetColour(viewingRing, animal.Scanned ? GoldRing : WhiteRing, alpha);
        SetColour(dangerRing, RedRing, alpha);
    }

    // I build one flat circle on the ground and give it its own LineRenderer.
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

        // The circle sits on the ground at the animal's own position.
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

    // The savanna is not flat, so a circle drawn at one height disappears under the bumps.
    // I drop a ray onto the ground under every point and sit the ring just above what it hits.
    private float GroundHeight(Vector3 point, float fallback)
    {
        Vector3 from = new Vector3(point.x, point.y + GroundProbeHeight, point.z);

        // The animals are on layer 6 and the zones are triggers, so neither can answer this ray.
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 20f, GroundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y + heightOffset;

        // Nothing under this point, so I keep the height of the animal instead of dropping into nothing.
        return fallback;
    }

    // The alpha of a LineRenderer comes from the colours at both ends of the line.
    private static void SetColour(LineRenderer line, Color colour, float alpha)
    {
        Color faded = new Color(colour.r, colour.g, colour.b, alpha);
        line.startColor = faded;
        line.endColor = faded;
    }
}