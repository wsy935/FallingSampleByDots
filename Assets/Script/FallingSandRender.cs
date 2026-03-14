using Pixel;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FallingSandRender : MonoBehaviour
{
    public static FallingSandRender Instance { get; private set; }
    SpriteRenderer sr;
    Texture2D tex;

    [Header("渲染设置")]
    public int pixelPerUnit = 100;

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
        sr = GetComponent<SpriteRenderer>();
        
        var worldConfig = FallingSandWorld.Instance.WorldConfig;
        // 创建数据纹理 (CPU 写入编码数据, Shader 解码)
        tex = new Texture2D(worldConfig.width, worldConfig.height, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        // 创建 Sprite
        sr.sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, worldConfig.width, worldConfig.height), new Vector2(0.5f, 0.5f), pixelPerUnit);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        var chunks = FallingSandWorld.Instance.Chunks;
        var worldConfig = FallingSandWorld.Instance.WorldConfig;
        if (!chunks.IsCreated) return;

        float size = (float)worldConfig.chunkSize / pixelPerUnit;
        for (int i = 0; i < chunks.Length; i++)
        {
            if (!chunks[i].IsDirty(Time.frameCount)) continue;

            int2 coord = worldConfig.GetCoordsByChunk(chunks[i].pos, 0, 0);
            float x = (float)(coord.x - (worldConfig.width >> 1)) / pixelPerUnit;
            float y = (float)(coord.y - (worldConfig.height >> 1)) / pixelPerUnit;

            Gizmos.color = ((chunks[i].pos.x + chunks[i].pos.y) & 1) == 0 ? Color.black : Color.white;

            Gizmos.DrawLine(new(x, y), new(x, y + size));
            Gizmos.DrawLine(new(x, y), new(x + size, y));
            Gizmos.DrawLine(new(x + size, y), new(x + size, y + size));
            Gizmos.DrawLine(new(x, y + size), new(x + size, y + size));
        }
    }
#endif
}
