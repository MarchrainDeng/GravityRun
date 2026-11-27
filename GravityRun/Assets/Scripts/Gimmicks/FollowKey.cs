using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FollowKey : MonoBehaviour
{
    [Header("Follow Offset")]
    [SerializeField]
    private Vector2 followOffset =
        new Vector2(-0.6f, 0.9f);

    [Header("Pickup Animation")]
    [SerializeField, Min(0f)]
    private float pickupDuration = 0.3f;

    [Header("Follow Inertia")]
    [SerializeField, Min(0.01f)]
    private float followSmoothTime = 0.2f;

    [Header("Teleport Handling")]
    [SerializeField, Min(0f)]
    private float teleportSnapDistance = 3f;

    private BoxCollider2D pickupCollider;
    private Transform followTarget;
    private float originalZ;

    private Vector3 pickupStartPosition;
    private float pickupTimer;
    private bool isPickingUp;

    private Vector3 followVelocity;
    private Vector3 previousPlayerPosition;

    public bool IsCollected { get; private set; }

    public PlayerMovement Owner { get; private set; }

    private void Awake()
    {
        pickupCollider = GetComponent<BoxCollider2D>();
        originalZ = transform.position.z;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsCollected)
        {
            return;
        }

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        PickUp(player);
    }

    private void PickUp(PlayerMovement player)
    {
        IsCollected = true;
        Owner = player;
        followTarget = player.transform;

        pickupCollider.enabled = false;

        previousPlayerPosition = followTarget.position;
        followVelocity = Vector3.zero;

        BeginPickupEffect();
    }

    private void BeginPickupEffect()
    {
        pickupStartPosition = transform.position;
        pickupTimer = 0f;
        isPickingUp = true;

        if (pickupDuration <= 0f)
        {
            SnapToFollowPosition();
        }
    }

    private void LateUpdate()
    {
        if (!IsCollected || followTarget == null)
        {
            return;
        }

        Vector3 currentPlayerPosition = followTarget.position;

        // Detect a large position change between rendered frames.
        bool playerTeleported =
            teleportSnapDistance > 0f &&
            (currentPlayerPosition - previousPlayerPosition)
                .sqrMagnitude >
            teleportSnapDistance * teleportSnapDistance;

        previousPlayerPosition = currentPlayerPosition;

        if (playerTeleported)
        {
            SnapToFollowPosition();
            return;
        }

        if (isPickingUp)
        {
            UpdatePickupEffect();
        }
        else
        {
            UpdateFollowPosition();
        }
    }

    private void UpdatePickupEffect()
    {
        pickupTimer += Time.deltaTime;

        float progress = pickupDuration <= 0f
            ? 1f
            : Mathf.Clamp01(pickupTimer / pickupDuration);

        Vector3 targetPosition = GetFollowPosition();

        transform.position = Vector3.Lerp(
            pickupStartPosition,
            targetPosition,
            progress
        );

        if (progress >= 1f)
        {
            transform.position = targetPosition;

            isPickingUp = false;
            followVelocity = Vector3.zero;
        }
    }

    private Vector3 GetFollowPosition()
    {
        Vector3 playerPosition = followTarget.position;

        // Keep the offset independent of player facing and scale.
        return new Vector3(
            playerPosition.x + followOffset.x,
            playerPosition.y + followOffset.y,
            originalZ
        );
    }

    private void UpdateFollowPosition()
    {
        // Gradually accelerate toward the target and settle when it stops.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            GetFollowPosition(),
            ref followVelocity,
            Mathf.Max(0.01f, followSmoothTime),
            Mathf.Infinity,
            Time.deltaTime
        );
    }

    private void SnapToFollowPosition()
    {
        transform.position = GetFollowPosition();

        followVelocity = Vector3.zero;
        isPickingUp = false;
    }
}