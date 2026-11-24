using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("跳跃设置")]
    [SerializeField, Min(0f)] private float jumpSpeed = 10f;

    [SerializeField, Range(0f, 1f)]
    private float jumpCutMultiplier = 0.45f;

    [Header("土狼时间")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.15f;

    [Header("落地检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField]
    private Vector2 groundCheckSize =
        new Vector2(0.5f, 0.08f);
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private float coyoteTimer;

    // 缓存输入，避免物理更新漏掉短按。
    // 短い入力を物理更新で取りこぼさないように保存する。
    private bool jumpRequested;
    private bool jumpReleased;
    private bool jumpHeld;

    // 本次跳跃是否还允许松键截断上升速度。
    // 今回のジャンプで、ボタンを離した際に上昇速度を下げられるか。
    private bool canCutJump;

    // 供后续计时系统读取落地状态。
    // 後のタイマーシステムから接地状態を参照できる。
    public bool IsGrounded { get; private set; }

    private bool inputLocked;

    // 锁定或恢复角色输入，并清除尚未执行的跳跃。
    // 入力をロック・解除し、未処理のジャンプ入力を消去する。
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        moveInput = 0f;
        jumpRequested = false;
        jumpReleased = false;
        jumpHeld = false;
        coyoteTimer = 0f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (inputLocked)
        {
            return;
        }

        ReadMoveInput();
        ReadJumpInput();
    }

    private void FixedUpdate()
    {
        // 锁定期间仍然更新落地状态，供计时器使用。
        // ロック中も接地状態を更新し、タイマーから参照できるようにする。
        CheckGround();

        if (inputLocked)
        {
            return;
        }

        UpdateCoyoteTime();

        HandleMove();
        HandleJump();

        jumpRequested = false;
        jumpReleased = false;
    }

    private void ReadMoveInput()
    {
        moveInput = ReadKeyboardInput();
    }

    private float ReadKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return 0f;
        }

        float input = 0f;

        if (Keyboard.current.dKey.isPressed)
        {
            input += 1f;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            input -= 1f;
        }

        return input;
    }

    private void ReadJumpInput()
    {
        if (Keyboard.current == null)
        {
            jumpHeld = false;
            return;
        }

        var spaceKey = Keyboard.current.spaceKey;

        if (spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }

        if (spaceKey.wasReleasedThisFrame)
        {
            jumpReleased = true;
        }

        jumpHeld = spaceKey.isPressed;
    }

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            IsGrounded = false;
            return;
        }

        bool touchesGround = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            groundLayer
        ) != null;

        // 上升时不视为落地，避免起跳瞬间重复获得跳跃机会。
        // 上昇中は接地扱いにせず、離陸直後の再ジャンプを防ぐ。
        IsGrounded = touchesGround && rb.linearVelocity.y <= 0.01f;
    }

    private void UpdateCoyoteTime()
    {
        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
            canCutJump = false;
        }
        else
        {
            coyoteTimer = Mathf.Max(
                0f,
                coyoteTimer - Time.fixedDeltaTime
            );
        }
    }

    private void HandleMove()
    {
        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed,
            rb.linearVelocity.y
        );
    }

    private void HandleJump()
    {
        // 在地面上，或仍处于土狼时间内时，允许起跳。
        // 接地中、またはコヨーテタイム中ならジャンプできる。
        if (jumpRequested && (IsGrounded || coyoteTimer > 0f))
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpSpeed
            );

            // 主动起跳会消耗本次跳跃机会。
            // ジャンプすると今回のジャンプ権を消費する。
            coyoteTimer = 0f;
            IsGrounded = false;
            canCutJump = true;
        }

        // 上升期间松开 Space，只降低一次上升速度。
        // 上昇中にSpaceを離すと、一度だけ上昇速度を下げる。
        if (canCutJump &&
            (jumpReleased || !jumpHeld) &&
            rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );

            canCutJump = false;
        }

        // 已到达最高点或开始下落后，不再处理截断。
        // 頂点に達した後、または下降中は上昇速度の変更を行わない。
        if (rb.linearVelocity.y <= 0f)
        {
            canCutJump = false;
        }
    }

    private void OnDisable()
    {
        moveInput = 0f;
        jumpRequested = false;
        jumpReleased = false;
        jumpHeld = false;
        canCutJump = false;
        coyoteTimer = 0f;
        IsGrounded = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        // 选中角色时显示脚底检测范围。
        // キャラクター選択時に足元の検出範囲を表示する。
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            groundCheck.position,
            new Vector3(groundCheckSize.x, groundCheckSize.y, 0f)
        );
    }
}