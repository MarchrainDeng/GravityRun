using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class JellyVisual : MonoBehaviour
{
    [Header("RB")]
    [SerializeField] private Rigidbody2D playerRb;

    [Header("参数")]
    [SerializeField, Min(1f)] private float springStrength = 100f;
    [SerializeField, Min(0f)] private float damping = 8f;
    [SerializeField, Min(0f)] private float inertiaStrength = 0.08f;
    [SerializeField, Min(0f)] private float maxOffset = 0.35f;

    [Header("弹力范围")]
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

    [Header("ﾌｾﾓ・莊ﾘｹ郞ｳ")]
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

    [Header("下坠力度")]
    [SerializeField, Range(0f, 0.3f)]
    private float fallStretch = 0.12f;

    [SerializeField, Min(0.1f)]
    private float fullStretchSpeed = 12f;

    [Header("变形度")]
    [SerializeField, Range(0.4f, 1f)]
    private float minHeightScale = 0.65f;

    [SerializeField, Range(1f, 1.5f)]
    private float maxHeightScale = 1.25f;

    // ｸﾟｶﾈﾏ犖ﾔﾔｭﾐﾎｵﾄｱ莉ｯﾁｿ｣ｬ0ｱ桄ｾﾔｭﾊｼｸﾟｶﾈ｡｣
    // ﾔｪ､ﾎｸﾟ､ｵ､ｫ､鬢ﾎ我ｻｯﾁｿ｡｣0､ﾏﾔｪ､ﾎｸﾟ､ｵ｡｣
    private float squashOffset;
    private float squashVelocity;

    private float previousVerticalSpeed;
    private bool wasGrounded;

    [SerializeField] private Sprite airborneSprite;

    private bool recallSquashActive;
    private float recallHeightScale = 1f;

    // ｿｪﾊｼﾓﾉﾉﾁｻﾘﾑﾝｳﾘﾖﾆﾉ﨓衵ﾎﾗｴ｡｣
    // ﾎｻﾖﾃ諾､ｷﾑﾝｳﾋ､隍・ﾎﾗｴﾖﾆﾓ_ﾊｼ､ｹ､・｣
    public void BeginRecallSquash()
    {
        recallSquashActive = true;
        recallHeightScale = 1f;

        offset = 0f;
        offsetVelocity = 0f;
        squashOffset = 0f;
        squashVelocity = 0f;
    }

    // ﾉ靹ﾃﾑﾝｳﾐｵﾄﾉ﨓蟶ﾟｶﾈｱﾈﾀ｡｣
    // ﾑﾝｳﾐ､ﾎｸﾟ､ｵｱｶﾂﾊ､Oｶｨ､ｹ､・｣
    public void SetRecallHeight(float value)
    {
        recallHeightScale = Mathf.Clamp(value, 0.4f, 1f);
    }

    // ｻﾖｸｴﾍｨｳ｣ｵﾄｹ郞ｳﾄ｣ﾄ筍｣
    // ﾍｨｳ｣､ﾎ･ｼ･・`･ｷ･ﾟ･螂・`･ｷ･逾ﾋ諾､ｹ｡｣
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
                "JellyVisual｣ｺﾇ・ｨ Player Rb｣ｬｲ｢ﾉ靹ﾃｽｺﾄﾒ Sprite｡｣",
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

        // ﾔｭｾｫﾁ鰒ｻﾌ盪ｩﾐﾎﾗｴ｣ｬﾓﾉﾐﾂﾍｺﾔﾔﾊｾ｡｣
        // ﾔｪ､ﾎ･ｹ･ﾗ･鬣､･ﾈ､ﾏﾐﾎﾗｴ､ﾎﾈ｡ｵﾃ､ﾋﾊｹ､､｡｢ﾐﾂ､ｷ､､･皈ﾃ･ｷ･螟ﾇﾃ霆ｭ､ｹ､・｣
        sourceRenderer.enabled = false;
    }

    private float GetLocalHorizontalSpeed()
    {
        // ﾗｪｻｻｵｽﾍ篁ﾛｾﾖｲｿｿﾕｼ茱ｬﾊｹﾋﾅｺﾄｹﾟﾐﾔｷｽﾏﾔﾈｻﾕﾈｷ｡｣
        // ｱ桄ｾ､ﾎ･愰`･ｫ･・ﾕ馮､ﾘ我轍､ｷ｡｢宙ｿs矣､竭Tﾐﾔｷｽﾏﾏ､・ｻ､・｣
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

        // ﾉﾁｻﾘﾑﾝｳﾚｼ茣ｻｵｼﾓﾋｮﾆｽｻﾎｶｯ｡｣
        // ﾎｻﾖﾃ諾､ｷﾑﾝｳﾐ､ﾏﾗﾒ､ﾎ兎､・ﾘ､ﾍ､ﾊ､､｡｣
        if (recallSquashActive)
        {
            offset = 0f;
            offsetVelocity = 0f;
            return;
        }

        float currentSpeed = GetLocalHorizontalSpeed();
        float speedChange = currentSpeed - previousSpeed;
        previousSpeed = currentSpeed;

        // ｱｾﾌ袞ﾒｼﾓﾋﾙﾊｱ｣ｬﾍ篁ﾛｶ･ｲｿｻﾃﾏﾄｹﾟﾐﾔ｡｣
        // ｱｾﾌ螟ｬﾓﾒ､ﾘｼﾓﾋﾙ､ｹ､・ﾈ｡｢ﾉﾏｲｿ､ﾋ､ﾏﾗｭ､ﾎ禅ﾐﾔ､・ｨ､・｣
        offsetVelocity -= speedChange * inertiaStrength;

        // ｷﾖﾐ｡ｲｽｼﾆﾋ羞ｯｻﾉ｣ｬｼﾙｲﾎﾊｽﾏｴｱｵﾄｲｻﾎﾈｶｨ｡｣
        // ﾐ｡､ｵ､ﾊｿﾌ､ﾟ､ﾇ･ﾐ･ﾍ､桐网ｷ｡｢ｲｻｰｲｶｨ､ﾊ・､ｭ､ﾖ､ｨ､・｣
        int steps = Mathf.CeilToInt(Time.fixedDeltaTime / 0.005f);
        float stepTime = Time.fixedDeltaTime / steps;

        for (int i = 0; i < steps; i++)
        {
            offsetVelocity += -offset * springStrength * stepTime;
            offsetVelocity *= Mathf.Exp(-damping * stepTime);
            offset += offsetVelocity * stepTime;

            // ﾏﾞﾖﾆﾗ鋗菻ﾎ｣ｬｲ｢ﾏ﨤ｼﾌﾐ箏ﾄﾋﾙｶﾈ｡｣
            // ﾗ鋗菻ﾎﾁｿ､ﾆﾏﾞ､ｷ｡｢ﾍ竄ﾈ､ﾘﾟM､猴ﾙｶﾈ､ｹ､皃・｣
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

        // ｸﾟｶﾈｽｵｵﾍﾊｱｼﾓｿ惞ｬｸﾟｶﾈﾔﾓﾊｱｱ蒄ｭ｣ｬｽ・ﾆｱ｣ｳﾖﾃ貊｡｣
        // ｸﾟ､ｵ､ｬ徘､・ﾈｺ盥愛荀ｷ｡｢ｸﾟ､ｵ､ｬ沿､ｨ､・ﾈｺ盥p､鬢ｹ｡｣
        float widthScale = 1f / heightScale;

        // ﾔｭﾍﾄﾋｮﾆｽﾖﾐﾐﾄ｣ｬｼ貶ﾝｲｻﾔﾚﾖﾐﾐﾄｵﾄSpriteﾖ瞎ﾄ｡｣
        // ﾖﾐﾑ・ﾔﾍ筅ﾎ･ﾔ･ﾜ･ﾃ･ﾈ､ﾋ､竚搶熙ｹ､・皈ﾃ･ｷ･螟ﾎﾋｮﾆｽﾖﾐﾐﾄ｡｣
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

            // ﾎｧﾈﾆﾋｮﾆｽﾖﾐﾐﾄﾋﾅｿ昮ﾈ｣ｬｲ｢ｱ｣ﾁﾒｹﾟﾐﾔﾍ萇奛｣
            // ﾋｮﾆｽﾖﾐﾐﾄ､ﾊ､ﾋｷ荀ｨ｡｢ﾗﾒ､ﾎ禅ﾐﾔ､篝Sｳﾖ､ｹ､・｣
            vertex.x =
                centerX +
                (original.x - centerX) * widthScale +
                offset * bendWeight;

            // ﾒﾔｵﾗｲｿﾎｪｻｼｸﾄｱ荳ﾟｶﾈ｣ｬｱﾜﾃ簷ｹﾋｱｽﾅｵﾗﾉﾏｸ｡｡｣
            // ﾏﾂｶﾋ､ﾊ､ﾋｸﾟ､ｵ､荀ｨ｡｢・ｿs瓶､ﾋﾗ耿ｪ､ｬｸ｡､ｯ､ﾎ､ﾀ､ｰ｡｣
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

        // ｴﾋｲ簗ﾔｰ賁ｹﾓﾃﾎｴｴ・ﾄｶﾀﾁ｢ﾍｼﾆｬ｡｣
        // ､ｳ､ﾎ･ﾆ･ｹ･ﾈｰ讀ﾇ､ﾏ･｢･ﾈ･鬣ｹﾎｴﾊｹﾓﾃ､ﾎｻｭﾏｹﾓﾃ､ｹ､・｣
        if (sprite.packed)
        {
            Debug.LogError(
                "JellyVisual｣ｺﾇ・ﾈｽｫｴﾋﾍｼﾆｬｴﾓ Sprite Atlas ﾖﾐﾒﾆｳ｡｣",
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
                "JellyVisual｣ｺﾕﾒｲｻｵｽｾｫﾁ鰕ﾔﾊｾﾓﾃ Shader｡｣",
                this
            );
            return;
        }

        Rect rect = sprite.rect;
        Texture2D texture = sprite.texture;
        float pixelsPerUnit = sprite.pixelsPerUnit;

        // ｸﾝﾍｼﾆｬｳﾟｴ郤ﾍﾖ瞎ﾄ｣ｬｱ｣ｳﾖﾔｭﾊｼｱﾈﾀﾓ・ｻﾖﾃ｡｣
        // ｻｭﾏｵ･､･ｺ､ﾈ･ﾔ･ﾜ･ﾃ･ﾈ､ｫ､鰆ｪ､ﾎｱﾈﾂﾊ､ﾈﾎｻﾖﾃ､Sｳﾖ､ｹ､・｣
        float width = rect.width / pixelsPerUnit;
        height = rect.height / pixelsPerUnit;

        float leftX = -sprite.pivot.x / pixelsPerUnit;
        bottomY = -sprite.pivot.y / pixelsPerUnit;

        // ﾊｹﾓﾃｹｲﾏ昮･ｵ羞ﾄｹ贇｣
        // 嵓ｵ网ｲﾓﾐ､ｹ､・ｻ､ﾊ･ｰ･・ﾃ･ﾉ､ﾉ､ｹ､・｣
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

                // ﾍｼﾆｬﾗ・・・ｻﾒｻｶﾔﾓｦ｡｣
                // ｻｭﾏﾋ､ﾈ･皈ﾃ･ｷ･袮ﾋ､搶熙ｵ､ｻ､・｣
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

        // ｳｼﾗｴﾌｬｱ｣ｳﾖﾔｭﾍｼﾐﾎﾗｴ｡｣
        // ｳﾚﾗｴ腺､ﾏﾔｪｻｭﾏﾎﾐﾎﾗｴ､Sｳﾖ､ｹ､・｣
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

        // ｼ・簪ﾏｵﾄﾋﾙｶﾈﾍｻｱ茱ｬｼ貶ﾝｵﾘﾃ賣ｾｺﾍﾍﾁﾀﾇﾌｾ｡｣
        // ﾉﾏﾏｭ､ﾎｼｱ､ﾊﾋﾙｶﾈ我ｻｯ､ﾊｳｷ｡｢･ｳ･隧`･ﾆ･ｸ･罕ﾗ､ﾋ､竚搶熙ｹ､・｣
        bool justJumped =
            !grounded &&
            verticalSpeed > 1f &&
            speedChange > 1f;

        if (justJumped)
        {
            // ｸﾟｶﾈｵｯｻﾉﾒｻｸﾂﾑｹﾋﾄｳ蠱ｿ｡｣
            // ｸﾟ､ｵ､ﾎ･ﾐ･ﾍ､ﾋ・ｿsｷｽﾏﾎ･､･ﾑ･・ｹ､・ｨ､・｣
            squashVelocity -= jumpSquashImpulse;
        }

        // ｴﾓｿﾕﾖﾐｱ萸ｪﾂ莊ﾘﾊｱ｣ｬｸﾝﾂ蔆ﾂﾋﾙｶﾈｾｨﾑｹﾋｦｶﾈ｡｣
        // ｿﾕﾖﾐ､ｫ､鮨ﾓｵﾘ､ｷ､ｿ・｡｢ﾂ蔆ﾂﾋﾙｶﾈ､ﾋ場､ｸ､ﾆ・ｿs､ｹ､・｣
        if (grounded && !wasGrounded)
        {
            float landingSpeed = Mathf.Max(0f, -previousVerticalSpeed);

            float impulse = Mathf.Min(
                landingSpeed * landingSquashStrength,
                maxLandingImpulse
            );

            squashVelocity -= impulse;
        }

        // ﾏﾂﾂ蕚ｽｿ・ｬﾄｿｱ・ﾎﾗｴﾔｽﾏｸｳ､｡｣
        // ﾂ蔆ﾂﾋﾙｶﾈ､ｬﾋﾙ､､､ﾛ､ﾉ｡｢ｿk餃､ﾎﾐﾎﾗｴ､ｿ侏､ﾋ､ｹ､・｣
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

        // ﾂ莊ﾘﾊｹﾓﾃﾔｭﾍｼﾆｬ｣ｬﾖﾍｿﾕﾊｹﾓﾃｿﾕﾖﾐﾍｼﾆｬ｡｣
        // ｽﾓｵﾘﾖﾐ､ﾏﾔｪｻｭﾏ｢ｿﾕﾖﾐ､ﾇ､ﾏｿﾕﾖﾐﾓﾃｻｭﾏｹﾓﾃ､ｹ､・｣
        Sprite targetSprite = sourceRenderer.sprite;

        // ﾖﾍｿﾕｻﾔﾚｲ･ｷﾅﾉﾁｻﾘﾑﾝｳｱ｣ｬﾊｹﾓﾃﾌｾﾍｼﾆｬ｡｣
        // ｿﾕﾖﾐ｡｢､ﾞ､ｿ､ﾏﾎｻﾖﾃ諾､ｷﾑﾝｳﾐ､ﾏ･ｸ･罕ﾗｻｭﾏｹﾓﾃ､ｹ､・｣
        if ((!playerMovement.IsGrounded || recallSquashActive) &&
            airborneSprite != null)
        {
            targetSprite = airborneSprite;
        }

        if (targetSprite == null)
        {
            return;
        }

        // ｽﾐｻｻﾌｼ｣ｬｱ｣ﾁｱﾇｰﾍﾍｵｯｻﾉﾗｴﾌｬ｡｣
        // ･ﾆ･ｯ･ｹ･ﾁ･网ﾀ､ｱ､ﾐ､・讀ｨ｡｢･皈ﾃ･ｷ･螟ﾈ･ﾐ･ﾍ､ﾎﾗｴ腺､Sｳﾖ､ｹ､・｣
        Texture targetTexture = targetSprite.texture;

        if (runtimeMaterial.GetTexture("_MainTex") != targetTexture)
        {
            runtimeMaterial.SetTexture("_MainTex", targetTexture);
        }
    }
}