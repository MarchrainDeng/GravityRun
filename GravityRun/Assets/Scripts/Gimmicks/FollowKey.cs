using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FollowKey : MonoBehaviour
{
    [Header("跟随位置：相对于角色中心的世界坐标偏移")]
    [SerializeField]
    private Vector2 followOffset =
        new Vector2(-0.6f, 0.9f);

    private BoxCollider2D pickupCollider;
    private Transform followTarget;
    private float originalZ;

    // 保留拾取状态，方便后续终点检查。
    // 後のゴール判定で使えるよう、取得状態を保持する。
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

        // 拾取后关闭触发区域，避免继续触发其他接触。
        // 取得後は判定を無効にして、他の接触を防ぐ。
        pickupCollider.enabled = false;

        BeginPickupEffect();
    }

    private void BeginPickupEffect()
    {
        // 当前直接到达跟随位置。
        // 現在は追従位置へ即座に移動する。
        // 后续可在此加入从原位置飞向角色的过渡动画。
        // 後から元の位置からプレイヤーへ飛ぶ演出を追加できる。
        transform.position = GetFollowPosition();
    }

    private void LateUpdate()
    {
        if (!IsCollected || followTarget == null)
        {
            return;
        }

        UpdateFollowPosition();
    }

    private Vector3 GetFollowPosition()
    {
        Vector3 playerPosition = followTarget.position;

        // 使用固定世界方向，不因角色朝向或缩放而翻转。
        // ワールド方向を使い、向きや拡縮で左右が反転しないようにする。
        return new Vector3(
            playerPosition.x + followOffset.x,
            playerPosition.y + followOffset.y,
            originalZ
        );
    }

    private void UpdateFollowPosition()
    {
        Vector3 targetPosition = GetFollowPosition();

        // 当前直接跟随，后续可在此加入弹簧或惯性计算。
        // 現在は直接追従し、後からバネや慣性の計算を追加できる。
        transform.position = targetPosition;
    }
}