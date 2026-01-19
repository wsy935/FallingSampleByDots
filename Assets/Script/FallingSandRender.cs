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
    Color32[] pixelBuffer;
    public int pixelPerUnit = 100;

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
        tex = new(fsw.WorldWidth, fsw.WorldHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        pixelBuffer = new Color32[fsw.WorldHeight * fsw.WorldWidth];
        sr.sprite = Sprite.Create(tex, new(0, 0, fsw.WorldWidth, fsw.WorldHeight), new(0.5f, 0.5f), pixelPerUnit);
    }

    private void Update()
    {
        UpdateTexture();
    }

    private void UpdateTexture()
    {
        var fsw = FallingSandWorld.Instance;
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(PixelBuffer), typeof(PixelChunk));
        var entitys = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entitys)
        {
            var chunk = em.GetComponentData<PixelChunk>(entity);
            var buffer = em.GetBuffer<PixelBuffer>(entity, true);
            for (int i = 0; i < fsw.ChunkEdge; i++)
            {
                for (int j = 0; j < fsw.ChunkEdge; j++)
                {
                    int worldIdx = fsw.GetWorldIdx(in chunk, i, j);
                    int chunkIdx = fsw.GetChunkIdx(i + fsw.ChunkBorder, j + fsw.ChunkBorder);

                    // 从 buffer 中读取像素类型并转换为颜色
                    var pixelType = buffer[chunkIdx].type;
                    if (fsw.PixelMap.TryGetValue(pixelType, out var pixelSO))
                    {
                        pixelBuffer[worldIdx] = pixelSO.color;
                    }
                    else
                    {
                        pixelBuffer[worldIdx] = Color.black; // 默认颜色
                    }
                }
            }
        }

        tex.SetPixels32(pixelBuffer);
        tex.Apply();
    }
}
