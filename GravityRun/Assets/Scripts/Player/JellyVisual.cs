using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class JellyVisual : MonoBehaviour
{
    [Header("角色本体")]
    [SerializeField] private Rigidbody2D playerRb;

    [Header("果冻设置")]
    [SerializeField, Min(1f)] private float springStrength = 100f;
    [SerializeField, Min(0f)] private float damping = 8f;
    [SerializeField, Min(0f)] private float inertiaStrength = 0.08f;
    [SerializeField, Min(0f)] private float maxOffset = 0.35f;

    [Header("弯曲设置")]
    [SerializeField, Range(1f, 4f)]
    private float bendPower = 1.5f;

    private SpriteRenderer sourceRenderer;
    private GameObject meshObject;
    private Mesh mesh;
    private Material runtimeMaterial;

    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    private float bottomY;
    private float height;

    private float offset;
    private float offsetVelocity;
    private float previousSpeed;

    [Header("跳跃与落地果冻")]
    [SerializeField] private PlayerMovement playerMovement;

    [SerializeField, Min(1f)]
    private float squashSpringStrength = 150f;

    [SerializeField, Min(0f)]
    private float squashDamping = 10f;

    [SerializeField, Min(0f)]
    private float jumpSquashImpulse = 2.5f;

    [SerializeField, Min(0f)]
    private float landingSquashStrength = 0.35f;

    [SerializeField, Min(0f)]
    private float maxLandingImpulse = 5f;

    [Header("下落拉伸")]
    [SerializeField, Range(0f, 0.3f)]
    private float fallStretch = 0.12f;

    [SerializeField, Min(0.1f)]
    private float fullStretchSpeed = 12f;

    [Header("压缩与拉伸限制")]
    [SerializeField, Range(0.4f, 1f)]
    private float minHeightScale = 0.65f;

    [SerializeField, Range(1f, 1.5f)]
    private float maxHeightScale = 1.25f;

    // 高度相对原形的变化量，0表示原始高度。
    // 元の高さからの変化量。0は元の高さ。
    private float squashOffset;
    private float squashVelocity;

    private float previousVerticalSpeed;
    private bool wasGrounded;

    [Header("空中外观")]
    [SerializeField] private Sprite airborneSprite;

    private bool recallSquashActive;
    private float recallHeightScale = 1f;

    // 开始由闪回演出控制身体形状。
    // 位置戻し演出による形状制御を開始する。
    public void BeginRecallSquash()
    {
        recallSquashActive = true;
        recallHeightScale = 1f;

        offset = 0f;
        offsetVelocity = 0f;
        squashOffset = 0f;
        squashVelocity = 0f;
    }

    // 设置演出中的身体高度比例。
    // 演出中の高さ倍率を設定する。
    public void SetRecallHeight(float value)
    {
        recallHeightScale = Mathf.Clamp(value, 0.4f, 1f);
    }

    // 恢复通常的果冻模拟。
    // 通常のゼリーシミュレーションに戻す。
    public void EndRecallSquash()
    {
        recallSquashActive = false;
        recallHeightScale = 1f;

        squashOffset = 0f;
        squashVelocity = 0f;

        if (playerRb != null)
        {
            previousVerticalSpeed = playerRb.linearVelocity.y;
        }

        if (playerMovement != null)
        {
            wasGrounded = playerMovement.IsGrounded;
        }
    }

    private void Start()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();

        if (playerRb == null || sourceRenderer.sprite == null)
        {
            Debug.LogError(
                "JellyVisual：请绑定 Player Rb，并设置胶囊 Sprite。",
                this
            );

            enabled = false;
            return;
        }

        CreateMesh();

        if (mesh == null)
        {
            enabled = false;
            return;
        }

        previousSpeed = GetLocalHorizontalSpeed();

        // 原精灵只提供形状，由新网格负责显示。
        // 元のスプライトは形状の取得に使い、新しいメッシュで描画する。
        sourceRenderer.enabled = false;
    }

    private float GetLocalHorizontalSpeed()
    {
        // 转换到外观局部空间，使缩放后的惯性方向仍然正确。
        // 表示のローカル空間へ変換し、拡縮後も慣性方向を合わせる。
        Vector3 worldVelocity = new Vector3(
            playerRb.linearVelocity.x,
            0f,
            0f
        );

        return transform.InverseTransformVector(worldVelocity).x;
    }

    private void FixedUpdate()
    {
        if (mesh == null)
        {
            return;
        }

        UpdateSquash();

        // 闪回演出期间不叠加水平晃动。
        // 位置戻し演出中は左右の揺れを重ねない。
        if (recallSquashActive)
        {
            offset = 0f;
            offsetVelocity = 0f;
            return;
        }

        float currentSpeed = GetLocalHorizontalSpeed();
        float speedChange = currentSpeed - previousSpeed;
        previousSpeed = currentSpeed;

        // 本体向右加速时，外观顶部获得向左的惯性。
        // 本体が右へ加速すると、上部には左向きの慣性を与える。
        offsetVelocity -= speedChange * inertiaStrength;

        // 分小步计算弹簧，减少参数较大时的不稳定。
        // 小さな刻みでバネを計算し、不安定な動きを抑える。
        int steps = Mathf.CeilToInt(Time.fixedDeltaTime / 0.005f);
        float stepTime = Time.fixedDeltaTime / steps;

        for (int i = 0; i < steps; i++)
        {
            offsetVelocity += -offset * springStrength * stepTime;
            offsetVelocity *= Mathf.Exp(-damping * stepTime);
            offset += offsetVelocity * stepTime;

            // 限制最大变形，并消除继续向外的速度。
            // 最大変形量を制限し、外側へ進む速度を止める。
            if (Mathf.Abs(offset) > maxOffset)
            {
                offset = Mathf.Clamp(offset, -maxOffset, maxOffset);

                if (offset * offsetVelocity > 0f)
                {
                    offsetVelocity = 0f;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (mesh == null)
        {
            return;
        }

        UpdateAppearance();

        float heightScale = recallSquashActive
            ? recallHeightScale
    :       Mathf.Clamp(
                1f + squashOffset,
                minHeightScale,
                maxHeightScale
            );

        // 高度降低时加宽，高度增加时变窄，近似保持面积。
        // 高さが減ると横幅を増やし、高さが増えると横幅を減らす。
        float widthScale = 1f / heightScale;

        // 原网格的水平中心，兼容不在中心的Sprite轴心。
        // 中央以外のピボットにも対応するメッシュの水平中心。
        Sprite sprite = sourceRenderer.sprite;

        float centerX =
            (sprite.rect.width * 0.5f - sprite.pivot.x)
            / sprite.pixelsPerUnit;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 original = originalVertices[i];

            float normalizedHeight = Mathf.Clamp01(
                (original.y - bottomY) / height
            );

            float bendWeight = Mathf.Pow(
                normalizedHeight,
                bendPower
            );

            Vector3 vertex = original;

            // 围绕水平中心缩放宽度，并保留左右惯性弯曲。
            // 水平中心を基準に幅を変え、左右の慣性も維持する。
            vertex.x =
                centerX +
                (original.x - centerX) * widthScale +
                offset * bendWeight;

            // 以底部为基准改变高度，避免压缩时脚底上浮。
            // 下端を基準に高さを変え、圧縮時に足元が浮くのを防ぐ。
            vertex.y =
                bottomY +
                (original.y - bottomY) * heightScale;

            deformedVertices[i] = vertex;
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateBounds();
    }

    private void CreateMesh()
    {
        Sprite sprite = sourceRenderer.sprite;

        // 此测试版使用未打包的独立图片。
        // このテスト版ではアトラス未使用の画像を使用する。
        if (sprite.packed)
        {
            Debug.LogError(
                "JellyVisual：请先将此图片从 Sprite Atlas 中移除。",
                this
            );
            return;
        }

        Shader shader = Shader.Find(
            "Universal Render Pipeline/2D/Sprite-Unlit-Default"
        );

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError(
                "JellyVisual：找不到精灵显示用 Shader。",
                this
            );
            return;
        }

        Rect rect = sprite.rect;
        Texture2D texture = sprite.texture;
        float pixelsPerUnit = sprite.pixelsPerUnit;

        // 根据图片尺寸和轴心，保持原始比例与位置。
        // 画像サイズとピボットから元の比率と位置を維持する。
        float width = rect.width / pixelsPerUnit;
        height = rect.height / pixelsPerUnit;

        float leftX = -sprite.pivot.x / pixelsPerUnit;
        bottomY = -sprite.pivot.y / pixelsPerUnit;

        // 使用共享顶点的规则网格。
        // 頂点を共有する均一なグリッドを作成する。
        const int columns = 24;
        const int rows = 24;

        int verticesPerRow = columns + 1;
        int vertexCount = verticesPerRow * (rows + 1);

        originalVertices = new Vector3[vertexCount];
        deformedVertices = new Vector3[vertexCount];

        Vector2[] uvs = new Vector2[vertexCount];
        Color[] colors = new Color[vertexCount];
        int[] triangles = new int[columns * rows * 6];

        for (int y = 0; y <= rows; y++)
        {
            float normalizedY = (float)y / rows;

            for (int x = 0; x <= columns; x++)
            {
                float normalizedX = (float)x / columns;
                int index = y * verticesPerRow + x;

                Vector3 position = new Vector3(
                    leftX + normalizedX * width,
                    bottomY + normalizedY * height,
                    0f
                );

                originalVertices[index] = position;
                deformedVertices[index] = position;

                // 图片坐标与网格坐标一一对应。
                // 画像座標とメッシュ座標を対応させる。
                uvs[index] = new Vector2(
                    (rect.x + normalizedX * rect.width) / texture.width,
                    (rect.y + normalizedY * rect.height) / texture.height
                );

                colors[index] = sourceRenderer.color;
            }
        }

        int triangleIndex = 0;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int bottomLeft = y * verticesPerRow + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + verticesPerRow;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        mesh = new Mesh();
        mesh.name = "Jelly Grid Mesh";
        mesh.MarkDynamic();

        mesh.vertices = deformedVertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        runtimeMaterial = new Material(shader);
        runtimeMaterial.SetTexture("_MainTex", texture);

        if (runtimeMaterial.HasProperty("_Color"))
        {
            runtimeMaterial.SetColor("_Color", Color.white);
        }

        if (runtimeMaterial.HasProperty("_RendererColor"))
        {
            runtimeMaterial.SetColor("_RendererColor", Color.white);
        }

        meshObject = new GameObject("JellyMesh");
        meshObject.layer = gameObject.layer;
        meshObject.transform.SetParent(transform, false);

        MeshFilter filter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = runtimeMaterial;

        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder;

        // 初始状态保持原图形状。
        // 初期状態は元画像の形状を維持する。
        offset = 0f;
        offsetVelocity = 0f;
    }

    private void Subdivide(
        Vector2 a, Vector2 b, Vector2 c,
        Vector2 uvA, Vector2 uvB, Vector2 uvC,
        int depth,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles)
    {
        if (depth == 0)
        {
            int start = vertices.Count;

            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);

            uvs.Add(uvA);
            uvs.Add(uvB);
            uvs.Add(uvC);

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            return;
        }

        Vector2 ab = (a + b) * 0.5f;
        Vector2 bc = (b + c) * 0.5f;
        Vector2 ca = (c + a) * 0.5f;

        Vector2 uvAB = (uvA + uvB) * 0.5f;
        Vector2 uvBC = (uvB + uvC) * 0.5f;
        Vector2 uvCA = (uvC + uvA) * 0.5f;

        Subdivide(a, ab, ca, uvA, uvAB, uvCA,
            depth - 1, vertices, uvs, triangles);

        Subdivide(ab, b, bc, uvAB, uvB, uvBC,
            depth - 1, vertices, uvs, triangles);

        Subdivide(ca, bc, c, uvCA, uvBC, uvC,
            depth - 1, vertices, uvs, triangles);

        Subdivide(ab, bc, ca, uvAB, uvBC, uvCA,
            depth - 1, vertices, uvs, triangles);
    }

    private void OnDestroy()
    {
        if (sourceRenderer != null)
        {
            sourceRenderer.enabled = true;
        }

        if (meshObject != null)
        {
            Destroy(meshObject);
        }

        if (mesh != null)
        {
            Destroy(mesh);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    private void UpdateSquash()
    {
        if (playerMovement == null || playerRb == null)
        {
            return;
        }

        float verticalSpeed = playerRb.linearVelocity.y;
        bool grounded = playerMovement.IsGrounded;

        float speedChange = verticalSpeed - previousVerticalSpeed;

        // 检测向上的速度突变，兼容地面跳跃和土狼跳跃。
        // 上向きの急な速度変化を検出し、コヨーテジャンプにも対応する。
        bool justJumped =
            !grounded &&
            verticalSpeed > 1f &&
            speedChange > 1f;

        if (justJumped)
        {
            // 给高度弹簧一个向下压缩的冲量。
            // 高さのバネに圧縮方向のインパルスを与える。
            squashVelocity -= jumpSquashImpulse;
        }

        // 从空中变为落地时，根据落下速度决定压缩力度。
        // 空中から接地した際、落下速度に応じて圧縮する。
        if (grounded && !wasGrounded)
        {
            float landingSpeed = Mathf.Max(0f, -previousVerticalSpeed);

            float impulse = Mathf.Min(
                landingSpeed * landingSquashStrength,
                maxLandingImpulse
            );

            squashVelocity -= impulse;
        }

        // 下落越快，目标形状越细长。
        // 落下速度が速いほど、縦長の形状を目標にする。
        float targetOffset = 0f;

        if (!grounded && verticalSpeed < 0f)
        {
            float fallRatio = Mathf.Clamp01(
                -verticalSpeed / Mathf.Max(0.1f, fullStretchSpeed)
            );

            targetOffset = fallRatio * fallStretch;
        }

        int steps = Mathf.CeilToInt(Time.fixedDeltaTime / 0.005f);
        float stepTime = Time.fixedDeltaTime / steps;

        float minOffset = minHeightScale - 1f;
        float maxOffset = maxHeightScale - 1f;

        for (int i = 0; i < steps; i++)
        {
            float force =
                (targetOffset - squashOffset) * squashSpringStrength;

            squashVelocity += force * stepTime;
            squashVelocity *= Mathf.Exp(-squashDamping * stepTime);
            squashOffset += squashVelocity * stepTime;

            if (squashOffset < minOffset)
            {
                squashOffset = minOffset;

                if (squashVelocity < 0f)
                {
                    squashVelocity = 0f;
                }
            }
            else if (squashOffset > maxOffset)
            {
                squashOffset = maxOffset;

                if (squashVelocity > 0f)
                {
                    squashVelocity = 0f;
                }
            }
        }

        previousVerticalSpeed = verticalSpeed;
        wasGrounded = grounded;
    }

    private void UpdateAppearance()
    {
        if (runtimeMaterial == null ||
            sourceRenderer == null ||
            playerMovement == null)
        {
            return;
        }

        // 落地使用原图片，滞空使用空中图片。
        // 接地中は元画像、空中では空中用画像を使用する。
        Sprite targetSprite = sourceRenderer.sprite;

        // 滞空或正在播放闪回演出时，使用跳跃图片。
        // 空中、または位置戻し演出中はジャンプ画像を使用する。
        if ((!playerMovement.IsGrounded || recallSquashActive) &&
            airborneSprite != null)
        {
            targetSprite = airborneSprite;
        }

        if (targetSprite == null)
        {
            return;
        }

        // 仅切换贴图，保留当前网格和弹簧状态。
        // テクスチャだけを切り替え、メッシュとバネの状態を維持する。
        Texture targetTexture = targetSprite.texture;

        if (runtimeMaterial.GetTexture("_MainTex") != targetTexture)
        {
            runtimeMaterial.SetTexture("_MainTex", targetTexture);
        }
    }
}