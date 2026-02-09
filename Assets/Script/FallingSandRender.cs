using Pixel;
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

    private WorldConfig worldConfig;
    private DynamicBuffer<Chunk> chunks;

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
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        sr = GetComponent<SpriteRenderer>();
        worldConfig = em.GetSingletonComponent<WorldConfig>();
        chunks = em.GetSingletonBuffer<Chunk>();

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
    }
}
