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
    [SerializeField] private PixelSet pixelSet;
    readonly private int chunkEdge = 32;
    private int2 chunkCount;
    private PixelConfigMap pixelConfigMap;
    public static FallingSandWorld Instance { get; private set; }

    public int WorldWidth => worldWidth;
    public int WorldHeight => worldHeight;
    public int ChunkEdge => chunkEdge;
    public int2 ChunkCount => chunkCount;

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
        chunkCount = new(Mathf.CeilToInt(worldWidth / chunkEdge), Mathf.CeilToInt(worldHeight / chunkEdge));

        CreateChunk();
        CreateWorldConfig();
        CreatePixelConfigMap();
    }

    void OnDestroy()
    {
        pixelConfigMap.Dispose();
    }

    //添加PixelConfigMap单例组件,由于其包含NativeContainer,所以缓存该组件，在Destroy时释放
    private void CreatePixelConfigMap()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var pixelConfigs = pixelSet.configs;
        pixelConfigMap = new PixelConfigMap(pixelConfigs.Length);
        foreach (var config in pixelConfigs)
        {            
            pixelConfigMap.AddConfig(config.type, config);
        }
        em.CreateSingleton(pixelConfigMap);
    }

    private void CreateWorldConfig()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        WorldConfig worldConfig = new()
        {
            chunkCnt = chunkCount,
            chunkEdge = chunkEdge,
            width = worldWidth,
            height = worldHeight
        };
        em.CreateSingleton(worldConfig);
    }

    private void CreateChunk()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        for (int i = 0; i < chunkCount.y; i++)
        {
            for (int j = 0; j < chunkCount.x; j++)
            {
                var entity = em.CreateEntity();
                em.AddComponentData(entity, new PixelChunk()
                {
                    pos = new(j, i),
                    isDirty = false
                });                

                var buffer = em.AddBuffer<PixelBuffer>(entity);
                int totalSize = chunkEdge * chunkEdge;
                for (int k = 0; k < totalSize; k++)
                {
                    buffer.Add(new PixelBuffer { type = PixelType.Empty });
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
        return y * chunkEdge + x;
    }
}
