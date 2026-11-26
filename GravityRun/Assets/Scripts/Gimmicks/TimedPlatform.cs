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

    [Header("秒数文字")]
    [SerializeField] private TMP_Text secondText;

    [Header("未生效时的外观")]
    [SerializeField, Range(0f, 1f)]
    private float inactiveOpacity = 0.25f;

    private SpriteRenderer platformRenderer;
    private BoxCollider2D platformCollider;

    private Color originalColor;
    private Color originalNumberColor;

    private void Awake()
    {
        platformRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<BoxCollider2D>();

        originalColor = platformRenderer.color;

        if (secondText != null)
        {
            originalNumberColor = secondText.color;

            // 数字显示在平台前面。
            // 数字をプラットフォームより手前に描画する。
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
        if (airborneTimer == null)
        {
            return;
        }

        int currentSecond = Mathf.FloorToInt(
            airborneTimer.ElapsedTime
        );

        bool isActive = currentSecond == targetSecond;

        UpdateNumber();
        ApplyState(isActive);
    }

    private void UpdateNumber()
    {
        if (secondText == null)
        {
            return;
        }

        // 自动显示Inspector中设置的生效秒数。
        // Inspectorで設定した有効秒数を自動表示する。
        string number = targetSecond.ToString();

        if (secondText.text != number)
        {
            secondText.text = number;
        }
    }

    private void ApplyState(bool isActive)
    {
        platformCollider.enabled = isActive;

        float opacityMultiplier = isActive
            ? 1f
            : inactiveOpacity;

        // 平台和数字使用相同的不透明度倍率。
        // プラットフォームと数字に同じ不透明度倍率を適用する。
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