using UnityEngine;

public class GoalDoor : MonoBehaviour
{
    [Header("用于此门的钥匙")]
    [SerializeField] private FollowKey requiredKey;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 识别接触门的角色，兼容子物体上的碰撞体。
        // ドアに触れたプレイヤーを判定し、子のColliderにも対応する。
        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        // 钥匙必须已被拾取，并且属于当前角色。
        // 鍵が取得済みで、現在のプレイヤーが持っていることを確認する。
        bool hasKey =
            requiredKey != null &&
            requiredKey.IsCollected &&
            requiredKey.Owner == player;

        if (hasKey)
        {
            Debug.Log("arrived with key");
        }
        else
        {
            Debug.Log("arrived without key");
        }
    }
}