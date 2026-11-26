using TMPro;
using UnityEngine;

[DefaultExecutionOrder(300)]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class TimedPlatform : MonoBehaviour
{
    [Header("关卡计时器")]
    [SerializeField] private AirborneTimer airborneTimer;

    [Header("生效秒数")]
    [SerializeField, Min(0)]
    private int targetSecond = 6;

    [Header("判断模式：都不勾选时为等于")]
    [SerializeField] private bool greaterOrEqual = false;
    [SerializeField] private bool lessOrEqual = false;

    [Header("秒数文字")]
    [SerializeField] private TMP_Text secondText;

    [Header("未生效时的外观")]
    [SerializeField, Range(0f, 1f)]
    private float inactiveOpacity = 0.25f;

    private SpriteRenderer platformRenderer;
    private BoxCollider2D platformCollider;

    private Color originalColor;
    private Color originalNumberColor;

    // 防止模式冲突时，每个物理帧都输出警告。
    // モード競合時に毎フレーム警告が出ることを防ぐ。
    private bool conflictWarningShown;

    private void Awake()
    {
        platformRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<BoxCollider2D>();

        originalColor = platformRenderer.color;

        if (secondText != null)
        {
            originalNumberColor = secondText.color;

            Renderer numberRenderer =
                secondText.GetComponent<Renderer>();

            if (numberRenderer != null)
            {
                numberRenderer.sortingLayerID =
                    platformRenderer.sortingLayerID;

                numberRenderer.sortingOrder =
                    platformRenderer.sortingOrder + 1;
            }
        }

        if (airborneTimer == null)
        {
            Debug.LogError(
                "TimedPlatform：请绑定 Airborne Timer。",
                this
            );

            CheckModeConflict();
            UpdateNumber();
            ApplyState(false);

            enabled = false;
        }
    }

    private void Start()
    {
        RefreshState();
    }

    private void FixedUpdate()
    {
        RefreshState();
    }

    private void RefreshState()
    {
        CheckModeConflict();
        UpdateNumber();

        if (airborneTimer == null)
        {
            return;
        }

        // 使用计时器的完整秒数，舍去小数部分。
        // タイマーの小数部分を切り捨て、整数秒を使用する。
        int currentSecond = Mathf.FloorToInt(
            airborneTimer.ElapsedTime
        );

        bool isActive;

        if (greaterOrEqual && !lessOrEqual)
        {
            isActive = currentSecond >= targetSecond;
        }
        else if (lessOrEqual && !greaterOrEqual)
        {
            isActive = currentSecond <= targetSecond;
        }
        else
        {
            // 都未选择或发生冲突时，使用等于模式。
            // 未選択、または競合時は等しい場合のみ有効にする。
            isActive = currentSecond == targetSecond;
        }

        ApplyState(isActive);
    }

    private void CheckModeConflict()
    {
        bool hasConflict = greaterOrEqual && lessOrEqual;

        if (hasConflict && !conflictWarningShown)
        {
            Debug.LogWarning(
                "TimedPlatform：Greater Or Equal 与 Less Or Equal " +
                "不能同时勾选。当前暂时按“等于”模式处理。",
                this
            );
        }

        // 冲突解除后，允许下一次冲突重新输出警告。
        // 競合解消後は、次の競合時に再び警告できるようにする。
        conflictWarningShown = hasConflict;
    }

    private void UpdateNumber()
    {
        if (secondText == null)
        {
            return;
        }

        string prefix = "";

        if (greaterOrEqual && !lessOrEqual)
        {
            prefix = "≥";
        }
        else if (lessOrEqual && !greaterOrEqual)
        {
            prefix = "≤";
        }

        string label = prefix + targetSecond.ToString();

        if (secondText.text != label)
        {
            secondText.text = label;
        }
    }

    private void ApplyState(bool isActive)
    {
        platformCollider.enabled = isActive;

        float opacityMultiplier = isActive
            ? 1f
            : inactiveOpacity;

        Color platformColor = originalColor;
        platformColor.a *= opacityMultiplier;
        platformRenderer.color = platformColor;

        if (secondText != null)
        {
            Color numberColor = originalNumberColor;
            numberColor.a *= opacityMultiplier;
            secondText.color = numberColor;
        }
    }
}