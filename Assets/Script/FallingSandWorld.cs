using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Pixel;
using System.Collections.Generic;
using System.Linq;

public class FallingSandWorld : MonoBehaviour
{
    [Header("世界设置")]
    [SerializeField] private int worldWidth = 256;
    [SerializeField] private int worldHeight = 256;
    [SerializeField] private int chunkEdge = 8;
    [SerializeField] private int chunkBorder = 1;
    private int chunkRealEdge;
    [SerializeField] private PixelSet pixelSet;
    private Dictionary<PixelType, PixelSO> pixelMap;
    public static FallingSandWorld Instance { get; private set; }

    public int WorldWidth => worldWidth;
    public int WorldHeight => worldHeight;
    public int ChunkEdge => chunkEdge;
    public int ChunkBorder => chunkBorder;
    
    public PixelSet PixelSet => pixelSet;
    public Dictionary<PixelType, PixelSO> PixelMap => pixelMap;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        pixelMap = pixelSet.pixels.ToDictionary(e => e.type);
        foreach (var e in pixelSet.pixels)
        {
            e.ComplieHandler();
        }

        CreateChunk();
        chunkRealEdge = chunkBorder * 2 + chunkEdge;
    }

    private void CreateChunk()
    {
        int2 chunkCnt = new(Mathf.CeilToInt(worldWidth / chunkEdge), Mathf.CeilToInt(worldHeight / chunkEdge));
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        for (int i = 0; i < chunkCnt.y; i++)
        {
            for (int j = 0; j < chunkCnt.x; j++)
            {
                var entity = em.CreateEntity();
                em.AddComponentData(entity, new PixelChunk()
                {
                    pos = new(j, i),
                    isDirty = false
                });

                if ((i + j) % 2 == 0)
                {
                    em.AddComponent<WhiteChunkTag>(entity);
                }
                else
                {
                    em.AddComponent<BlackChunkTag>(entity);
                }

                var buffer = em.AddBuffer<PixelBuffer>(entity);
                int totalSize = (chunkBorder*2 +chunkEdge) * (chunkBorder*2 +chunkEdge);
                buffer.Capacity = totalSize;
                for (int k = 0; k < totalSize; k++)
                {
                    buffer.Add(new PixelBuffer() { type = PixelType.Empty });
                }
            }
        }
    }

    public int GetWorldIdx(in PixelChunk pixelChunk, int x, int y)
    {
        var pos = pixelChunk.pos;
        //先从局部坐标转化到世界坐标再转换
        int worldX = pos.x * chunkEdge + x;
        int worldY = pos.y * chunkEdge + y;
        return worldY * worldWidth + worldX;
    }

    public int GetChunkIdx(int x, int y)
    {
        return y * chunkRealEdge + x;
    }
}
