using UnityEngine;

// 在计时器更新后读取时间。
// タイマー更新後に時間を取得する。
[DefaultExecutionOrder(300)]
[RequireComponent(typeof(Rigidbody2D))]
public class TimeRotatingPlatform : MonoBehaviour
{
    [Header("关卡计时器")]
    [SerializeField] private AirborneTimer airborneTimer;

    [Header("旋转设置")]
    [SerializeField] private bool clockwise = true;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 30f;

    private Rigidbody2D rb;

    private float previousTimerTime;
    private float targetAngle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (airborneTimer == null)
        {
            Debug.LogError(
                "TimeRotatingPlatform：请绑定 Airborne Timer。",
                this
            );

            enabled = false;
        }
    }

    private void Start()
    {
        // 保留场景中设置的初始角度。
        // シーンで設定した初期角度を維持する。
        targetAngle = rb.rotation;
        previousTimerTime = airborneTimer.ElapsedTime;
    }

    private void FixedUpdate()
    {
        float currentTime = airborneTimer.ElapsedTime;

        // 读取游戏计时器的增量，不使用现实经过的时间。
        // 実時間ではなく、ゲームタイマーの増加分を使用する。
        float timerDelta = Mathf.Max(
            0f,
            currentTime - previousTimerTime
        );

        previousTimerTime = currentTime;

        // Unity的Z轴负方向为顺时针。
        // UnityではZ軸の負方向が時計回り。
        float direction = clockwise ? -1f : 1f;

        targetAngle += direction * rotationSpeed * timerDelta;
        targetAngle = Mathf.Repeat(targetAngle, 360f);

        // 时间停止时目标角度不变，平台保持当前朝向。
        // 時間停止中は目標角度を変えず、その向きを維持する。
        rb.MoveRotation(targetAngle);
    }
}