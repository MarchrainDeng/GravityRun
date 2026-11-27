using UnityEngine;

public class PortalJellyEffect : MonoBehaviour
{
    [Header("Spring")]
    [SerializeField, Min(1f)]
    private float springStrength = 100f;

    [SerializeField, Min(0f)]
    private float damping = 8f;

    [Header("Pulse Strength")]
    [SerializeField, Min(0f)]
    private float enterImpulse = 3f;

    [SerializeField, Min(0f)]
    private float exitImpulse = 3f;

    [Header("Scale Limits")]
    [SerializeField, Range(0.1f, 1f)]
    private float minScaleMultiplier = 0.6f;

    [SerializeField, Range(1f, 2f)]
    private float maxScaleMultiplier = 1.4f;

    private Vector3 originalScale;
    private float scaleOffset;
    private float scaleVelocity;
    private bool initialized;

    private void Awake()
    {
        originalScale = transform.localScale;
        initialized = true;
    }

    public void PlayEnterPulse()
    {
        // A negative impulse starts by shrinking the portal.
        scaleVelocity -= enterImpulse;
    }

    public void PlayExitPulse()
    {
        // A positive impulse starts by expanding the portal.
        scaleVelocity += exitImpulse;
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        // Limit catch-up after a long frame.
        deltaTime = Mathf.Min(deltaTime, 0.1f);

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(deltaTime / 0.005f)
        );

        float stepTime = deltaTime / steps;

        float minOffset = minScaleMultiplier - 1f;
        float maxOffset = maxScaleMultiplier - 1f;

        for (int i = 0; i < steps; i++)
        {
            // Pull toward the original scale and damp the motion.
            scaleVelocity +=
                -scaleOffset * springStrength * stepTime;

            scaleVelocity *= Mathf.Exp(-damping * stepTime);
            scaleOffset += scaleVelocity * stepTime;

            if (scaleOffset < minOffset)
            {
                scaleOffset = minOffset;

                if (scaleVelocity < 0f)
                {
                    scaleVelocity = 0f;
                }
            }
            else if (scaleOffset > maxOffset)
            {
                scaleOffset = maxOffset;

                if (scaleVelocity > 0f)
                {
                    scaleVelocity = 0f;
                }
            }
        }

        // Snap to rest once the remaining movement is negligible.
        if (Mathf.Abs(scaleOffset) < 0.0001f &&
            Mathf.Abs(scaleVelocity) < 0.0001f)
        {
            scaleOffset = 0f;
            scaleVelocity = 0f;
        }

        float multiplier = 1f + scaleOffset;

        // Scale around the sprite pivot without changing its depth.
        transform.localScale = new Vector3(
            originalScale.x * multiplier,
            originalScale.y * multiplier,
            originalScale.z
        );
    }

    private void OnDisable()
    {
        scaleOffset = 0f;
        scaleVelocity = 0f;

        if (initialized)
        {
            transform.localScale = originalScale;
        }
    }
}