using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PositionRecall : MonoBehaviour
{
    [Header("角色引用")]
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private SpriteRenderer playerVisual;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private JellyVisual jellyVisual;

    [Header("记录点图片")]
    [SerializeField] private Sprite recordSprite;

    [SerializeField, Range(0f, 1f)]
    private float markerAlpha = 0.45f;

    [Header("粒子特效")]
    [SerializeField] private ParticleSystem burstPrefab;

    [Header("闪回演出")]
    [SerializeField, Min(0.01f)]
    private float compressDuration = 0.3f;

    [SerializeField, Min(0.01f)]
    private float recoverDuration = 0.3f;

    [SerializeField, Range(0.4f, 1f)]
    private float recallHeight = 0.65f;

    [Header("测试设置")]
    [SerializeField] private bool unlimitedUse = false;

    private Vector2 recordedPosition;
    private GameObject markerObject;

    private bool hasRecordedPosition;
    private bool hasUsed;
    private bool useRequested;

    private enum RecallPhase
    {
        Idle,
        Compressing,
        Recovering
    }

    private RecallPhase phase;
    private float phaseTimer;

    private RigidbodyConstraints2D savedConstraints;
    private bool playerFrozen;

    private void Awake()
    {
        if (playerRb == null ||
            playerVisual == null ||
            playerMovement == null ||
            jellyVisual == null)
        {
            Debug.LogError(
                "PositionRecall：请绑定全部角色引用。",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        // 演出期间不接受新的闪回输入。
        // 演出中は新しい位置戻し入力を受け付けない。
        if (phase != RecallPhase.Idle ||
            Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            useRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (phase != RecallPhase.Idle)
        {
            UpdateRecallEffect();
            return;
        }

        if (!useRequested)
        {
            return;
        }

        useRequested = false;

        if (hasRecordedPosition)
        {
            BeginRecall();
        }
        else if (!hasUsed || unlimitedUse)
        {
            RecordPosition();
        }
    }

    private void RecordPosition()
    {
        Sprite image = recordSprite != null
            ? recordSprite
            : playerVisual.sprite;

        if (image == null)
        {
            Debug.LogWarning(
                "PositionRecall：没有可用于记录点的图片。",
                this
            );
            return;
        }

        recordedPosition = playerRb.position;
        hasRecordedPosition = true;

        CreateMarker(image);

        // 粒子在独立世界物体上播放，不跟随角色。
        // パーティクルは独立したワールドオブジェクトとして再生する。
        PlayBurst(GetPlayerPosition());
    }

    private void BeginRecall()
    {
        useRequested = false;

        FreezePlayer();
        jellyVisual.BeginRecallSquash();

        phaseTimer = 0f;
        phase = RecallPhase.Compressing;
    }

    private void UpdateRecallEffect()
    {
        phaseTimer += Time.fixedDeltaTime;

        if (phase == RecallPhase.Compressing)
        {
            float t = Mathf.Clamp01(
                phaseTimer / Mathf.Max(0.01f, compressDuration)
            );

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            jellyVisual.SetRecallHeight(
                Mathf.Lerp(1f, recallHeight, easedT)
            );

            if (t >= 1f)
            {
                TeleportToRecordedPosition();
            }
        }
        else if (phase == RecallPhase.Recovering)
        {
            float t = Mathf.Clamp01(
                phaseTimer / Mathf.Max(0.01f, recoverDuration)
            );

            float easedT = Mathf.SmoothStep(0f, 1f, t);

            jellyVisual.SetRecallHeight(
                Mathf.Lerp(recallHeight, 1f, easedT)
            );

            if (t >= 1f)
            {
                FinishRecall();
            }
        }
    }

    private void TeleportToRecordedPosition()
    {
        // 只改变位置，不回退时间或机关。
        // 位置だけを戻し、時間やギミックは巻き戻さない。
        playerRb.position = recordedPosition;

        hasRecordedPosition = false;
        hasUsed = true;

        ClearMarker();
        PlayBurst(GetPlayerPosition());

        phaseTimer = 0f;
        phase = RecallPhase.Recovering;
    }

    private void FinishRecall()
    {
        jellyVisual.SetRecallHeight(1f);
        jellyVisual.EndRecallSquash();

        UnfreezePlayer();

        phase = RecallPhase.Idle;
        phaseTimer = 0f;
        useRequested = false;
    }

    private void FreezePlayer()
    {
        savedConstraints = playerRb.constraints;
        playerFrozen = true;

        playerMovement.SetInputLocked(true);

        // 清除速度，并冻结运动，使空中的角色也能停住。
        // 速度を消去して動きを固定し、空中でも停止させる。
        playerRb.linearVelocity = Vector2.zero;
        playerRb.angularVelocity = 0f;
        playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private void UnfreezePlayer()
    {
        if (!playerFrozen)
        {
            return;
        }

        if (playerRb != null)
        {
            playerRb.constraints = savedConstraints;
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
        }

        if (playerMovement != null)
        {
            playerMovement.SetInputLocked(false);
        }

        playerFrozen = false;
    }

    private Vector3 GetPlayerPosition()
    {
        return new Vector3(
            playerRb.position.x,
            playerRb.position.y,
            playerRb.transform.position.z
        );
    }

    private void PlayBurst(Vector3 position)
    {
        if (burstPrefab == null)
        {
            return;
        }

        // 不指定父物体，避免特效跟随角色移动。
        // 親を設定せず、キャラクターへの追従を防ぐ。
        ParticleSystem effect = Instantiate(
            burstPrefab,
            position,
            Quaternion.identity
        );

        var main = effect.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        effect.Play(true);

        // 预制体按0.5秒配置，播放后清理物体。
        // プレハブは0.5秒に設定し、再生後にオブジェクトを削除する。
        Destroy(effect.gameObject, 1f);
    }

    private void CreateMarker(Sprite image)
    {
        ClearMarker();

        markerObject = new GameObject("RecallMarker");

        markerObject.transform.SetPositionAndRotation(
            playerVisual.transform.position,
            playerVisual.transform.rotation
        );

        markerObject.transform.localScale =
            playerVisual.transform.lossyScale;

        markerObject.layer = playerVisual.gameObject.layer;

        SpriteRenderer markerRenderer =
            markerObject.AddComponent<SpriteRenderer>();

        markerRenderer.sprite = image;
        markerRenderer.sharedMaterial = playerVisual.sharedMaterial;

        markerRenderer.sortingLayerID = playerVisual.sortingLayerID;
        markerRenderer.sortingOrder = playerVisual.sortingOrder - 1;

        markerRenderer.flipX = playerVisual.flipX;
        markerRenderer.flipY = playerVisual.flipY;

        Color color = playerVisual.color;
        color.a *= markerAlpha;
        markerRenderer.color = color;
    }

    private void ClearMarker()
    {
        if (markerObject == null)
        {
            return;
        }

        Destroy(markerObject);
        markerObject = null;
    }

    private void OnDisable()
    {
        useRequested = false;

        // 演出被中断时也恢复操作，避免角色永久冻结。
        // 演出が中断されても操作を戻し、固定されたままになるのを防ぐ。
        if (playerFrozen)
        {
            if (jellyVisual != null)
            {
                jellyVisual.EndRecallSquash();
            }

            UnfreezePlayer();
        }

        phase = RecallPhase.Idle;
        phaseTimer = 0f;
    }

    private void OnDestroy()
    {
        ClearMarker();
    }
}