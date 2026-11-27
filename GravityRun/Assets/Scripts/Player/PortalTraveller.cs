using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
    private Collider2D blockedPortal;
    private Collider2D[] playerColliders;
    private float blockedAtFixedTime;

    private void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider2D>();
    }

    private void FixedUpdate()
    {
        // Check continuously so the lock clears while outside the portal.
        UpdateTeleportLock();
    }

    public void BlockUntilExit(Collider2D destination)
    {
        blockedPortal = destination;
        blockedAtFixedTime = Time.fixedTime;
    }

    public bool CanTeleport()
    {
        UpdateTeleportLock();

        return blockedPortal == null;
    }

    private void UpdateTeleportLock()
    {
        if (blockedPortal == null)
        {
            return;
        }

        // Prevent another teleport during the same physics step.
        if (Time.fixedTime <= blockedAtFixedTime)
        {
            return;
        }

        if (!blockedPortal.enabled ||
            !blockedPortal.gameObject.activeInHierarchy)
        {
            blockedPortal = null;
            return;
        }

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null ||
                !playerCollider.enabled ||
                !playerCollider.gameObject.activeInHierarchy ||
                playerCollider.isTrigger)
            {
                continue;
            }

            ColliderDistance2D distance = Physics2D.Distance(
                blockedPortal,
                playerCollider
            );

            // Keep the lock until every solid collider has left.
            if (distance.isValid && distance.distance <= 0f)
            {
                return;
            }
        }

        // The player has fully left the destination portal.
        blockedPortal = null;
    }
}