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
    private const float RingWidth = 0.12f;

    private static readonly Color WhiteRing = new Color(1f, 1f, 1f, 1f);
    private static readonly Color GoldRing = new Color(1f, 0.8f, 0.1f, 1f);
    private static readonly Color RedRing = new Color(0.9f, 0.15f, 0.12f, 1f);

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
        viewingRing = BuildRing("ViewingRing", animal.Data.ViewingRadius, WhiteRing);
        dangerRing = BuildRing("DangerRing", animal.Data.DangerRadius, RedRing);
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
    private LineRenderer BuildRing(string name, float radius, Color colour)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.loop = true;
        line.positionCount = PointCount;
        line.useWorldSpace = true;
        line.startWidth = RingWidth;
        line.endWidth = RingWidth;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        if (ringMaterial != null)
            line.sharedMaterial = ringMaterial;

        // The circle sits flat on the ground at the animal's own position.
        Vector3 centre = transform.position + Vector3.up * heightOffset;
        for (int i = 0; i < PointCount; i++)
        {
            float angle = i * Mathf.PI * 2f / PointCount;
            line.SetPosition(i, centre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        SetColour(line, colour, maxAlpha);
        return line;
    }

    // The alpha of a LineRenderer comes from the colours at both ends of the line.
    private static void SetColour(LineRenderer line, Color colour, float alpha)
    {
        Color faded = new Color(colour.r, colour.g, colour.b, alpha);
        line.startColor = faded;
        line.endColor = faded;
    }
}