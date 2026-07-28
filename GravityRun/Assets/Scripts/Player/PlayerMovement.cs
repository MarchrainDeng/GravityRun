using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("手柄设置")]
    [SerializeField, Range(0f, 1f)]
    private float stickDeadZone = 0.15f;

    private Rigidbody2D rb;

    // 当前水平输入，范围为 -1 到 1
    private float moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        ReadMoveInput();
    }

    private void FixedUpdate()
    {
        HandleMove();
    }

    /// <summary>
    /// 读取键盘和手柄的水平移动输入
    /// </summary>
    private void ReadMoveInput()
    {
        float keyboardInput = ReadKeyboardInput();
        float gamepadInput = ReadGamepadInput();

        // 左摇杆有明显输入时优先使用手柄，否则使用键盘
        if (Mathf.Abs(gamepadInput) > 0f)
        {
            moveInput = gamepadInput;
        }
        else
        {
            moveInput = keyboardInput;
        }
    }

    /// <summary>
    /// 读取键盘 A、D 输入
    /// </summary>
    private float ReadKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return 0f;
        }

        float input = 0f;

        // D 键提供正方向输入
        if (Keyboard.current.dKey.isPressed)
        {
            input += 1f;
        }

        // A 键提供负方向输入
        if (Keyboard.current.aKey.isPressed)
        {
            input -= 1f;
        }

        // 同时按下 A 和 D 时，结果为 0
        return input;
    }

    /// <summary>
    /// 读取手柄左摇杆的水平输入
    /// </summary>
    private float ReadGamepadInput()
    {
        if (Gamepad.current == null)
        {
            return 0f;
        }

        float input = Gamepad.current.leftStick.x.ReadValue();

        // 忽略摇杆轻微漂移
        if (Mathf.Abs(input) < stickDeadZone)
        {
            return 0f;
        }

        return input;
    }

    /// <summary>
    /// 根据输入设置角色水平速度
    /// </summary>
    private void HandleMove()
    {
        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed,
            rb.linearVelocity.y
        );
    }
}