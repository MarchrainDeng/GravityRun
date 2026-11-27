using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PortalTraveller))]
public class PortalTeleportEffect : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private JellyVisual jellyVisual;

    [Header("Timing")]
    [SerializeField, Min(0.01f)]
    private float enterDuration = 0.2f;

    [SerializeField, Min(0.01f)]
    private float exitDuration = 0.2f;

    [Header("Entry Shape")]
    [SerializeField, Range(0.01f, 1f)]
    private float narrowWidth = 0.08f;

    [SerializeField, Range(1f, 2f)]
    private float stretchedHeight = 1.35f;

    [Header("Exit Bounce")]
    [SerializeField, Range(1f, 1.5f)]
    private float exitBounceWidth = 1.15f;

    [SerializeField, Range(0.5f, 1f)]
    private float exitBounceHeight = 0.88f;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private PortalTraveller traveller;

    private RigidbodyConstraints2D savedConstraints;
    private Vector2 savedVelocity;
    private bool movementFrozen;

    [Header("Portal Particles")]
    [SerializeField] private ParticleSystem enterParticlesPrefab;
    [SerializeField] private ParticleSystem exitParticlesPrefab;

    public bool IsPlaying { get; private set; }

    [Header("Camera Shake")]
    [SerializeField] private TeleportCameraShake cameraShake;

    [SerializeField, Min(0.01f)]
    private float shakeDuration = 0.2f;

    [SerializeField, Min(0f)]
    private float shakeStrength = 0.05f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        traveller = GetComponent<PortalTraveller>();

        if (jellyVisual == null)
        {
            jellyVisual = GetComponentInChildren<JellyVisual>();
        }

        if (jellyVisual == null)
        {
            Debug.LogError(
                "PortalTeleportEffect: Assign a Jelly Visual.",
                this
            );

            enabled = false;
        }
    }

    public void PlayTeleport(
    Vector3 entranceCenter,
    Vector2 destinationPosition,
    Collider2D destinationTrigger,
    PortalJellyEffect exitPortalJelly = null)
    {
        if (!isActiveAndEnabled ||
            IsPlaying ||
            jellyVisual == null ||
            destinationTrigger == null)
        {
            return;
        }

        IsPlaying = true;

        StartCoroutine(TeleportRoutine(
            entranceCenter,
            destinationPosition,
            destinationTrigger,
            exitPortalJelly
        ));
    }

    private IEnumerator TeleportRoutine(
    Vector3 entranceCenter,
    Vector2 destinationPosition,
    Collider2D destinationTrigger,
    PortalJellyEffect exitPortalJelly)
    {
        FreezeMovement();
        jellyVisual.BeginPortalDeformation();

        PlayPortalParticles(
            enterParticlesPrefab,
            entranceCenter,
            true
         );

        float timer = 0f;

        // Narrow, stretch, and pull the visual toward the entrance.
        while (timer < enterDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / enterDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            float width = Mathf.Lerp(1f, narrowWidth, easedT);
            float height = Mathf.Lerp(1f, stretchedHeight, easedT);

            // Collapse completely during the final part of entry.
            float collapseT = Mathf.InverseLerp(0.6f, 1f, t);
            float visibilityScale =
                1f - Mathf.SmoothStep(0f, 1f, collapseT);

            jellyVisual.SetPortalShape(
                width * visibilityScale,
                height * visibilityScale,
                entranceCenter,
                easedT
            );

            yield return null;
        }

        // Lock return travel before moving the player.
        traveller.BlockUntilExit(destinationTrigger);

        rb.position = destinationPosition;

        // Shake once at the actual teleport moment.
        if (cameraShake != null)
        {
            cameraShake.PlayShake(
                shakeDuration,
                shakeStrength
            );
        }

        // Expand the exit portal when the player arrives.
        if (exitPortalJelly != null)
        {
            exitPortalJelly.PlayExitPulse();
        }

        // Play at the exit portal, rather than following the player.
        Vector3 exitCenter = destinationTrigger != null
            ? destinationTrigger.bounds.center
            : new Vector3(
                destinationPosition.x,
                destinationPosition.y,
                transform.position.z
            );

        PlayPortalParticles(
            exitParticlesPrefab,
            exitCenter,
            false
        );

        // Start at zero size at the exit.
        jellyVisual.SetPortalShape(
            0f,
            0f,
            Vector3.zero,
            0f
        );

        // Restore control while the exit visual continues playing.
        RestoreMovement();

        timer = 0f;

        while (timer < exitDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / exitDuration);

            float width;
            float height;

            if (t < 0.65f)
            {
                float expandT = Mathf.SmoothStep(
                    0f,
                    1f,
                    t / 0.65f
                );

                width = Mathf.Lerp(
                    narrowWidth,
                    exitBounceWidth,
                    expandT
                );

                height = Mathf.Lerp(
                    stretchedHeight,
                    exitBounceHeight,
                    expandT
                );
            }
            else
            {
                float settleT = Mathf.SmoothStep(
                    0f,
                    1f,
                    (t - 0.65f) / 0.35f
                );

                width = Mathf.Lerp(
                    exitBounceWidth,
                    1f,
                    settleT
                );

                height = Mathf.Lerp(
                    exitBounceHeight,
                    1f,
                    settleT
                );
            }

            float visibilityScale = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(t / 0.4f)
            );

            jellyVisual.SetPortalShape(
                width * visibilityScale,
                height * visibilityScale,
                Vector3.zero,
                0f
            );

            yield return null;
        }

        jellyVisual.EndPortalDeformation();
        IsPlaying = false;
    }

    private void FreezeMovement()
    {
        savedConstraints = rb.constraints;
        savedVelocity = rb.linearVelocity;
        movementFrozen = true;

        playerMovement.SetInputLocked(true);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private void RestoreMovement()
    {
        if (!movementFrozen)
        {
            return;
        }

        if (rb != null)
        {
            rb.constraints = savedConstraints;
            rb.linearVelocity = savedVelocity;
        }

        if (playerMovement != null)
        {
            playerMovement.SetInputLocked(false);
        }

        movementFrozen = false;
    }

    private void PlayPortalParticles(
    ParticleSystem prefab,
    Vector3 position,
    bool inward)
    {
        if (prefab == null)
        {
            return;
        }

        // Spawn independently so particles stay at the portal.
        ParticleSystem effect = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        var main = effect.main;

        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startDelay = 0f;

        if (inward)
        {
            // Match the inward particles to the entry animation.
            float duration = Mathf.Max(0.01f, enterDuration);

            main.duration = duration;
            main.startLifetime = duration;
            main.startSpeed = 0f;

            var shape = effect.shape;
            shape.radiusThickness = 0f;

            var velocity = effect.velocityOverLifetime;
            velocity.enabled = true;

            // Travel from the emission circle to the center.
            velocity.radial = -shape.radius / duration;
        }

        effect.Play(true);

        // Clean up after emission and particle lifetime have finished.
        float cleanupDelay =
            main.duration +
            main.startLifetime.constantMax +
            0.1f;

        Destroy(effect.gameObject, cleanupDelay);
    }

    private void OnDisable()
    {
        if (!IsPlaying)
        {
            return;
        }

        StopAllCoroutines();
        RestoreMovement();

        if (jellyVisual != null)
        {
            jellyVisual.EndPortalDeformation();
        }

        IsPlaying = false;
    }
}