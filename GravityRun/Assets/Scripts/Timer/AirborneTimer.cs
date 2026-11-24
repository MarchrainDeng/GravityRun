using TMPro;
using UnityEngine;

// 在角色移动和落地检测完成后更新计时。
// キャラクターの移動と接地判定の後にタイマーを更新する。
[DefaultExecutionOrder(200)]
public class AirborneTimer : MonoBehaviour
{
    [Header("角色")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("计时文字")]
    [SerializeField] private TMP_Text timerText;

    // 累计滞空时间，单位为秒。
    // 空中にいた累計時間。単位は秒。
    private float elapsedTime;

    // 供后续机关读取累计时间。
    // 後のギミックから累計時間を参照できる。
    public float ElapsedTime => elapsedTime;

    private void Awake()
    {
        elapsedTime = 0f;
        UpdateTimerText();
    }

    private void FixedUpdate()
    {
        if (playerMovement == null)
        {
            return;
        }

        // 只有滞空时才累计时间，落地时保留当前数值。
        // 空中にいる間だけ加算し、接地中は現在の値を維持する。
        if (!playerMovement.IsGrounded)
        {
            elapsedTime += Time.fixedDeltaTime;
        }
    }

    private void LateUpdate()
    {
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        // 将累计时间转换为百分之一秒。
        // 累計時間を100分の1秒に変換する。
        int totalCentiseconds = Mathf.FloorToInt(elapsedTime * 100f);

        int seconds = totalCentiseconds / 100;
        int centiseconds = totalCentiseconds % 100;

        // 显示格式：秒:百分之一秒。
        // 表示形式：秒:100分の1秒。
        timerText.text = $"{seconds:00}:{centiseconds:00}";
    }

    // 将计时恢复为00:00，供后续重置功能调用。
    // 後のリセット機能から呼び出し、00:00に戻す。
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerText();
    }
}