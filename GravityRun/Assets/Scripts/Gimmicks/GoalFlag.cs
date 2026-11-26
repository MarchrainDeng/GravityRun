using UnityEngine;

public class GoalFlag : MonoBehaviour
{
    private bool hasArrived;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasArrived)
        {
            return;
        }

        // 通过移动脚本识别角色，兼容子物体上的碰撞体。
        // 移動スクリプトでプレイヤーを判定し、子のColliderにも対応する。
        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        // 每面旗帜只触发一次。
        // 各旗につき一度だけ実行する。
        hasArrived = true;

        Debug.Log("arrived flag");
    }
}