using Unity.Entities;
using UnityEngine;
using Pixel;
using Unity.Collections;
using Unity.Mathematics;

public class FallingSandWorld : MonoBehaviour
{
    [Header("基础")]
    [SerializeField] private int frame = 60;

    [Header("世界设置")]
    [SerializeField] private int worldWidth = 256;
    [SerializeField] private int worldHeight = 256;
    [SerializeField] private int stepTimes = 2;
    [SerializeField] private PixelSet pixelSet;

    [Header("脏区块设置")]
    [SerializeField] private int maxChunkSize = 128;
    [SerializeField] private int chunkBorder = 1;
    [SerializeField] private int gridSize = 64;

    [Header("像素区块设置")]
    [SerializeField] private int chunkSize;

    private PixelConfigLookup pixelLookup;

    public static FallingSandWorld Instance { get; private set; }

    public PixelSet PixelSet => pixelSet;

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
        InitWorld();
    }

    void Start()
    {
        Application.targetFrameRate = frame;
    }

    void OnDestroy()
    {
        pixelLookup.Dispose();
    }

    private void InitWorld()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var pixelBuffer = em.GetBuffer<PixelData>(em.CreateSingletonBuffer<PixelData>());
        int totalSize = worldHeight * worldWidth;
        pixelBuffer.EnsureCapacity(totalSize);
        for (int i = 0; i < totalSize; i++)
            pixelBuffer.Add(new() { type = PixelType.Empty, frameIdx = 0 });

        int2 chunkCnt = new((worldWidth + chunkSize - 1) / chunkSize,
            (worldHeight + chunkSize - 1) / chunkSize
        );
        var chunkBuffer = em.GetBuffer<Chunk>(em.CreateSingletonBuffer<Chunk>());
        chunkBuffer.EnsureCapacity(chunkCnt.x * chunkCnt.y);        
        for (int i = 0; i < chunkCnt.y; i++)
        {
            for (int j = 0; j < chunkCnt.x; j++)
            {
                var chunk = new Chunk()
                {
                    pos = new(j, i),
                    isDirty = true,
                    isBlack = ((i + j) & 1) == 0
                };
                chunkBuffer.Add(chunk);
            }
        }

        var worldConfig = new WorldConfig()
        {
            width = worldWidth,
            height = worldHeight,
            chunkCnt = chunkCnt,
            chunkSize = chunkSize
        };
        em.CreateSingleton(worldConfig);

        var pixelConfigs = pixelSet.configs;
        pixelLookup = new PixelConfigLookup(Allocator.Persistent);
        foreach (var config in pixelConfigs)
        {
            pixelLookup.AddConfig(config.type, config);
        }
        em.CreateSingleton(pixelLookup);
    }
}
