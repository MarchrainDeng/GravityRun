using UnityEngine;

// ﾔﾚｼﾆﾊｱﾆ・ﾂｺﾁﾈ｡ﾊｱｼ茖｣
// ･ｿ･､･ﾞｩ`ｸ・ﾂ矣､ﾋ瓶馮､｡ｵﾃ､ｹ､・｣
[DefaultExecutionOrder(300)]
[RequireComponent(typeof(Rigidbody2D))]
public class TimeRotatingPlatform : MonoBehaviour
{
    [Header("计时器")]
    [SerializeField] private AirborneTimer airborneTimer;

    [Header("旋转方向")]
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
                "TimeRotatingPlatform｣ｺﾇ・ｨ Airborne Timer｡｣",
                this
            );

            enabled = false;
        }
    }

    private void Start()
    {
        // ｱ｣ﾁ｡ｾｰﾖﾐﾉ靹ﾃｵﾄｳｼｽﾇｶﾈ｡｣
        // ･ｷｩ`･ﾇﾔOｶｨ､ｷ､ｿｳﾚｽﾇｶﾈ､Sｳﾖ､ｹ､・｣
        targetAngle = rb.rotation;
        previousTimerTime = airborneTimer.ElapsedTime;
    }

    private void FixedUpdate()
    {
        float currentTime = airborneTimer.ElapsedTime;

        // ｶﾁﾈ｡ﾓﾎﾏｷｼﾆﾊｱﾆﾄﾔｿ｣ｬｲｻﾊｹﾓﾃﾏﾖﾊｵｾｭｹｵﾄﾊｱｼ茖｣
        // 携瓶馮､ﾇ､ﾏ､ﾊ､ｯ｡｢･ｲｩ`･爭ｿ･､･ﾞｩ`､ﾎ沿ｼﾓｷﾖ､ｹﾓﾃ､ｹ､・｣
        float timerDelta = Mathf.Max(
            0f,
            currentTime - previousTimerTime
        );

        previousTimerTime = currentTime;

        // UnityｵﾄZﾖ盧ｺｷｽﾏｪﾋｳﾊｱﾕ・｣
        // Unity､ﾇ､ﾏZﾝS､ﾎﾘ胴ｽﾏｬ瓶ﾓ興ﾘ､遙｣
        float direction = clockwise ? -1f : 1f;

        targetAngle += direction * rotationSpeed * timerDelta;
        targetAngle = Mathf.Repeat(targetAngle, 360f);

        // ﾊｱｼ莽｣ﾖｹﾊｱﾄｿｱ・ﾇｶﾈｲｻｱ茱ｬﾆｽﾌｨｱ｣ｳﾖｵｱﾇｰｳｯﾏ｣
        // 瓶馮ﾍ｣ﾖｹﾖﾐ､ﾏﾄｿ侏ｽﾇｶﾈ､荀ｨ､ｺ｡｢､ｽ､ﾎﾏｭ､Sｳﾖ､ｹ､・｣
        rb.MoveRotation(targetAngle);
    }
}