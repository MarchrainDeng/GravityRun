using UnityEngine;

public class TeleportCameraShake : MonoBehaviour
{
    private Vector3 originalLocalPosition;

    private float elapsed;
    private float shakeDuration;
    private float shakeStrength;

    private bool isShaking;
    private bool initialized;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        initialized = true;
    }

    public void PlayShake(float duration, float strength)
    {
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeStrength = Mathf.Max(0f, strength);

        elapsed = 0f;
        isShaking = true;
    }

    private void LateUpdate()
    {
        if (!isShaking)
        {
            return;
        }

        elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(
            elapsed / shakeDuration
        );

        if (progress >= 1f)
        {
            StopShake();
            return;
        }

        // Start strong and quickly fade toward zero.
        float intensity = Mathf.Pow(1f - progress, 2f);

        Vector2 offset =
            Random.insideUnitCircle *
            shakeStrength *
            intensity;

        // Apply an offset from the original position to avoid drift.
        transform.localPosition =
            originalLocalPosition +
            new Vector3(offset.x, offset.y, 0f);
    }

    private void StopShake()
    {
        isShaking = false;

        if (initialized)
        {
            transform.localPosition = originalLocalPosition;
        }
    }

    private void OnDisable()
    {
        StopShake();
    }
}