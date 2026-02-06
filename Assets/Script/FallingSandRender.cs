using Pixel;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FallingSandRender : MonoBehaviour
{
    public static FallingSandRender Instance { get; private set; }
    SpriteRenderer sr;
    Texture2D tex;
    Texture2D colorLUT;
    Material material;
    MaterialPropertyBlock mpb;

    [Header("渲染设置")]
    public int pixelPerUnit = 100;
    
    [Header("Shader 引用")]
    [SerializeField] private Shader fallingSandShader;

    [Header("调试")]
    public bool showDirtyChunks = true;
    public Color dirtyChunkColor = new Color(1f, 0f, 0f, 0.3f);

    public Texture2D Tex => tex;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        var fsw = FallingSandWorld.Instance;
        sr = GetComponent<SpriteRenderer>();
        var worldConfig = fsw.WorldConfig;

        // 创建数据纹理 (CPU 写入编码数据, Shader 解码)
        tex = new Texture2D(worldConfig.width, worldConfig.height, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // 生成 Color LUT 纹理
        GenerateColorLUT(fsw.PixelSet);

        // 创建 Sprite 和 Material
        sr.sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, worldConfig.width, worldConfig.height), new Vector2(0.5f, 0.5f), pixelPerUnit);

        // 创建材质
        if (fallingSandShader == null)
        {
            fallingSandShader = Shader.Find("FallingSand/PixelRender");
        }

        if (fallingSandShader != null)
        {
            material = new Material(fallingSandShader);
            mpb = new MaterialPropertyBlock();
            sr.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", tex);
            mpb.SetTexture("_ColorLUT", colorLUT);
            mpb.SetFloat("_LUTSize", colorLUT.height);
            sr.SetPropertyBlock(mpb);
            sr.material = material;
        }
        else
        {
            Debug.LogError("FallingSandRender: 未找到 FallingSand/PixelRender Shader!");
        }
    }

    /// <summary>
    /// 根据 PixelSet 配置生成 Color LUT 纹理
    /// LUT 布局: 4 列 x N 行 (N = 像素类型最大 ID + 1)
    /// 列0 = baseColor (RGBA)
    /// 列1 = noiseLowIntensity (编码到 R 通道)
    /// 列2 = noiseMidIntensity (编码到 R 通道)
    /// 列3 = noiseHighIntensity (编码到 R 通道)
    /// 行 = PixelType ID
    /// </summary>
    private void GenerateColorLUT(PixelSet pixelSet)
    {
        // 确定 LUT 行数：取所有像素类型中最大的 ID + 1
        int maxTypeId = 0;
        foreach (var config in pixelSet.configs)
        {
            int id = (int)config.type;
            if (id > maxTypeId) maxTypeId = id;
        }
        int lutHeight = maxTypeId + 1;

        // 创建 4xN 的 LUT 纹理
        colorLUT = new Texture2D(4, lutHeight, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // 初始化所有像素为透明黑色
        Color clearColor = new Color(0, 0, 0, 0);
        for (int y = 0; y < lutHeight; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                colorLUT.SetPixel(x, y, clearColor);
            }
        }

        // 填充每种像素类型的颜色和噪声参数
        foreach (var config in pixelSet.configs)
        {
            int row = (int)config.type;
            colorLUT.SetPixel(0, row, config.color);                                          // 列0: baseColor
            colorLUT.SetPixel(1, row, new Color(config.noiseLowIntensity, 0, 0, 1));          // 列1: 低频噪声强度
            colorLUT.SetPixel(2, row, new Color(config.noiseMidIntensity, 0, 0, 1));           // 列2: 中频噪声强度
            colorLUT.SetPixel(3, row, new Color(config.noiseHighIntensity, 0, 0, 1));          // 列3: 高频噪声强度
        }

        colorLUT.Apply(false);

        Debug.Log($"FallingSandRender: 生成 Color LUT ({colorLUT.width}x{colorLUT.height}), 包含 {pixelSet.configs.Length} 种像素类型");
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
        if (colorLUT != null)
        {
            Destroy(colorLUT);
        }
    }

    private void OnGUI()
    {
        if (!showDirtyChunks) return;

        var fsw = FallingSandWorld.Instance;
        if (fsw == null) return;

        var dirtyChunkManager = fsw.DirtyChunkManager;
        var dirtyChunks = dirtyChunkManager.GetDirtyChunks();
        if (!dirtyChunks.IsCreated || dirtyChunks.Length == 0) return;

        var worldConfig = fsw.WorldConfig;

        // 获取相机和 sprite 的世界边界
        Camera cam = Camera.main;
        if (cam == null) return;

        // 计算 sprite 在世界空间中的位置和大小
        Vector3 worldPos = transform.position;
        float worldWidth = worldConfig.width / (float)pixelPerUnit;
        float worldHeight = worldConfig.height / (float)pixelPerUnit;

        // 计算左下角位置（因为 sprite pivot 是 0.5, 0.5）
        Vector3 worldBottomLeft = worldPos - new Vector3(worldWidth * 0.5f, worldHeight * 0.5f, 0);

        // 设置绘制颜色
        GUI.color = dirtyChunkColor;

        // 遍历所有 DirtyChunk 并绘制
        for (int i = 0; i < dirtyChunks.Length; i++)
        {
            var chunk = dirtyChunks[i];
            if (!chunk.isDirty) continue;

            Pixel.Rect rect = chunk.rect;

            // 将像素坐标转换为世界坐标
            Vector3 worldChunkBottomLeft = worldBottomLeft + new Vector3(
                rect.x / (float)pixelPerUnit,
                rect.y / (float)pixelPerUnit,
                0
            );

            Vector3 worldChunkTopRight = worldBottomLeft + new Vector3(
                (rect.x + rect.width) / (float)pixelPerUnit,
                (rect.y + rect.height) / (float)pixelPerUnit,
                0
            );

            // 转换为屏幕坐标
            Vector3 screenBottomLeft = cam.WorldToScreenPoint(worldChunkBottomLeft);
            Vector3 screenTopRight = cam.WorldToScreenPoint(worldChunkTopRight);

            // Unity GUI 坐标系原点在左上角，Y 轴向下
            float screenX = screenBottomLeft.x;
            float screenY = Screen.height - screenTopRight.y;
            float screenWidth = screenTopRight.x - screenBottomLeft.x;
            float screenHeight = screenTopRight.y - screenBottomLeft.y;

            // 绘制矩形框
            UnityEngine.Rect screenRect = new UnityEngine.Rect(screenX, screenY, screenWidth, screenHeight);
            GUI.Box(screenRect, "");

            GUI.Box(new UnityEngine.Rect(screenX - 1, screenY - 1, screenWidth, 1), "");
            GUI.Box(new UnityEngine.Rect(screenX - 1, screenY + screenHeight - 1, screenWidth, 1), "");
            GUI.Box(new UnityEngine.Rect(screenX - 1, screenY, 1, screenHeight), "");
            GUI.Box(new UnityEngine.Rect(screenX + screenWidth - 1, screenY, 1, screenHeight), "");
        }

        // 重置 GUI 颜色
        GUI.color = Color.white;

        // 显示 DirtyChunk 数量信息
        GUI.Label(new UnityEngine.Rect(10, 10, 300, 20), $"DirtyChunks Count: {dirtyChunks.Length}");
    }
}
