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

    [SerializeField] private PixelSet pixelSet;

    [Header("像素区块设置")]
    [SerializeField] private int chunkSize;

    private PixelConfigLookup pixelLookup;
    private WorldConfig worldConfig;
    private EntityQuery chunkQuery;
    private EntityQuery bufferQuery;
    public static FallingSandWorld Instance { get; private set; }

    public PixelConfigLookup PixelLookup => pixelLookup;
    public WorldConfig WorldConfig => worldConfig;
    public DynamicBuffer<PixelData> PixelBuffer => bufferQuery.GetSingletonBuffer<PixelData>();
    public DynamicBuffer<Chunk> Chunks => chunkQuery.GetSingletonBuffer<Chunk>();

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
            pixelBuffer.Add(new()
            {
                type = PixelType.Empty,
                frameIdx = 0,
                temperature = TempreatureConfig.Default.baseTemp
            });
        bufferQuery = em.CreateEntityQuery(typeof(PixelData));

        int2 chunkCnt = new((worldWidth + chunkSize - 1) / chunkSize,
            (worldHeight + chunkSize - 1) / chunkSize
        );
        var chunks = em.GetBuffer<Chunk>(em.CreateSingletonBuffer<Chunk>());
        chunks.EnsureCapacity(chunkCnt.x * chunkCnt.y);
        for (int i = 0; i < chunkCnt.y; i++)
        {
            for (int j = 0; j < chunkCnt.x; j++)
            {
                var chunk = new Chunk()
                {
                    pos = new(j, i),
                    forceDiryFrame = Time.frameCount,
                    isBlack = ((i + j) & 1) == 0
                };
                chunks.Add(chunk);
            }
        }
        chunkQuery = em.CreateEntityQuery(typeof(Chunk));

        worldConfig = new WorldConfig()
        {
            width = worldWidth,
            height = worldHeight,
            chunkCnt = chunkCnt,
            chunkSize = chunkSize
        };
        em.CreateSingleton(worldConfig);

        var pixelConfigSOs = pixelSet.configs;
        pixelLookup = new PixelConfigLookup(Allocator.Persistent);
        int reactionRuleOffset = 0;
        foreach (var configSo in pixelConfigSOs)
        {
            var config = configSo.config;
            if (!config.tempConfig.IsSet)
                config.tempConfig = TempreatureConfig.Default;
            if (configSo.reactionRules.Count > 0)
            {
                config.reactionRuleCount = configSo.reactionRules.Count;
                config.reactionRuleOffset = reactionRuleOffset;
                reactionRuleOffset += configSo.reactionRules.Count;
                foreach (var reactionRule in configSo.reactionRules)
                {
                    pixelLookup.AddReactionRule(reactionRule);
                }
            }
            Debug.Log($"Add Pixel Config {config.type} : temperature {config.tempConfig.baseTemp}");
            pixelLookup.AddConfig(config.type, config);
        }        
        em.CreateSingleton(pixelLookup);
    }
}
