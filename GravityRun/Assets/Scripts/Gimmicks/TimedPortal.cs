using TMPro;
using UnityEngine;

[DefaultExecutionOrder(300)]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class TimedPortal : MonoBehaviour
{
    [Header("Paired Portal")]
    [SerializeField] private TimedPortal pairedPortal;

    [Header("Arrival Position")]
    [SerializeField] private Transform arrivalPoint;

    [Header("Level Timer")]
    [SerializeField] private AirborneTimer airborneTimer;

    [Header("Activation Second")]
    [SerializeField, Min(0)]
    private int targetSecond = 6;

    [Header("Comparison Mode: Neither Selected Means Equal")]
    [SerializeField] private bool greaterOrEqual = false;
    [SerializeField] private bool lessOrEqual = false;

    [Header("Number Label")]
    [SerializeField] private TMP_Text secondText;

    [Header("Inactive Appearance")]
    [SerializeField, Range(0f, 1f)]
    private float inactiveOpacity = 0.25f;

    private SpriteRenderer portalRenderer;
    private BoxCollider2D portalTrigger;

    private Color originalColor;
    private Color originalNumberColor;

    private bool conflictWarningShown;

    [Header("Portal Visual")]
    [SerializeField] private SpriteRenderer portalVisual;
    [SerializeField] private PortalJellyEffect portalJelly;

    private void Awake()
    {
        // Use the visual child when assigned.
        portalRenderer = portalVisual != null
            ? portalVisual
            : GetComponent<SpriteRenderer>();

        portalTrigger = GetComponent<BoxCollider2D>();

        portalTrigger.isTrigger = true;
        originalColor = portalRenderer.color;

        if (secondText != null)
        {
            originalNumberColor = secondText.color;

            // Render the number label in front of the portal.
            Renderer numberRenderer =
                secondText.GetComponent<Renderer>();

            if (numberRenderer != null)
            {
                numberRenderer.sortingLayerID =
                    portalRenderer.sortingLayerID;

                numberRenderer.sortingOrder =
                    portalRenderer.sortingOrder + 1;
            }
        }

        if (airborneTimer == null)
        {
            Debug.LogError(
                "TimedPortal: Assign an Airborne Timer.",
                this
            );
        }

        if (pairedPortal == null || pairedPortal == this)
        {
            Debug.LogWarning(
                "TimedPortal: Assign a different portal as the paired portal.",
                this
            );
        }
    }

    private void Start()
    {
        RefreshAppearance();
    }

    private void FixedUpdate()
    {
        RefreshAppearance();
    }

    private bool IsTimeActive()
    {
        if (!isActiveAndEnabled || airborneTimer == null)
        {
            return false;
        }

        // Compare whole seconds, ignoring the fractional part.
        int currentSecond = Mathf.FloorToInt(
            airborneTimer.ElapsedTime
        );

        if (greaterOrEqual && !lessOrEqual)
        {
            return currentSecond >= targetSecond;
        }

        if (lessOrEqual && !greaterOrEqual)
        {
            return currentSecond <= targetSecond;
        }

        // Fall back to equality when neither or both modes are selected.
        return currentSecond == targetSecond;
    }

    private void RefreshAppearance()
    {
        bool conflict = greaterOrEqual && lessOrEqual;

        if (conflict && !conflictWarningShown)
        {
            Debug.LogWarning(
                "TimedPortal: Greater Or Equal and Less Or Equal " +
                "cannot both be selected. Using Equal mode instead.",
                this
            );
        }

        conflictWarningShown = conflict;

        float opacityMultiplier = IsTimeActive()
            ? 1f
            : inactiveOpacity;

        Color portalColor = originalColor;
        portalColor.a *= opacityMultiplier;
        portalRenderer.color = portalColor;

        if (secondText != null)
        {
            string prefix = "";

            if (greaterOrEqual && !lessOrEqual)
            {
                prefix = "¡Ý";
            }
            else if (lessOrEqual && !greaterOrEqual)
            {
                prefix = "¡Ü";
            }

            string label = prefix + targetSecond.ToString();

            if (secondText.text != label)
            {
                secondText.text = label;
            }

            Color numberColor = originalNumberColor;
            numberColor.a *= opacityMultiplier;
            secondText.color = numberColor;
        }

        // Keep the trigger enabled even when inactive.
        // Time conditions prevent teleportation.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTeleport(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Allow activation when the player is already inside the portal.
        TryTeleport(other);
    }

    private void TryTeleport(Collider2D other)
    {
        // Both portals must be valid when the effect starts.
        if (!IsTimeActive() ||
            pairedPortal == null ||
            pairedPortal == this ||
            !pairedPortal.IsTimeActive())
        {
            return;
        }

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        PortalTraveller traveller =
            player.GetComponent<PortalTraveller>();

        PortalTeleportEffect teleportEffect =
            player.GetComponent<PortalTeleportEffect>();

        if (playerRb == null ||
            traveller == null ||
            teleportEffect == null ||
            !teleportEffect.isActiveAndEnabled ||
            teleportEffect.IsPlaying)
        {
            return;
        }

        // Do not interrupt another effect that freezes the player.
        RigidbodyConstraints2D frozenPosition =
            RigidbodyConstraints2D.FreezePosition;

        if ((playerRb.constraints & frozenPosition) ==
            frozenPosition)
        {
            return;
        }

        if (!traveller.CanTeleport())
        {
            return;
        }

        Vector3 entranceCenter = portalTrigger.bounds.center;

        // Start the entrance portal pulse immediately.
        if (portalJelly != null)
        {
            portalJelly.PlayEnterPulse();
        }

        teleportEffect.PlayTeleport(
            entranceCenter,
            pairedPortal.GetArrivalPosition(),
            pairedPortal.portalTrigger,
            pairedPortal.portalJelly
        );
    }

    private Vector2 GetArrivalPosition()
    {
        // Use the portal center if no arrival point is assigned.
        return arrivalPoint != null
            ? (Vector2)arrivalPoint.position
            : (Vector2)transform.position;
    }
}