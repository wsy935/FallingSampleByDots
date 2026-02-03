using Unity.Entities;
using UnityEngine;
using Pixel;
using Unity.Collections;

public class FallingSandWorld : MonoBehaviour
{
    [Header("基础")]
    [SerializeField] private int frame=60;

    [Header("世界设置")]
    [SerializeField] private int worldWidth = 256;
    [SerializeField] private int worldHeight = 256;
    [SerializeField] private int stepTimes = 2;
    [SerializeField] private PixelSet pixelSet;
    
    [Header("脏区块设置")]
    [SerializeField] private int maxChunkSize = 128;
    [SerializeField] private int chunkBorder = 1;
    [SerializeField] private int gridSize = 64;
    private PixelConfigLookup pixelLookup;
    private WorldConfig worldConfig;
    private PixelBuffer pixelBuffer;
    
    private DirtyChunkManager dirtyChunkManager;
    public static FallingSandWorld Instance { get; private set; }
    
    public DirtyChunkManager DirtyChunkManager => dirtyChunkManager;
    public WorldConfig WorldConfig => worldConfig;        

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
        pixelBuffer.Dispose();
        pixelLookup.Dispose();
        dirtyChunkManager.Dispose();
    }

    private void InitWorld()
    {
        pixelBuffer = new()
        {
            buffer= new(worldHeight * worldWidth, Allocator.Persistent)
        };
        for (int i = 0; i < pixelBuffer.buffer.Length; i++)
            pixelBuffer.buffer[i] = new() { type = PixelType.Empty, frameIdx = 0 };

        CreateWorldConfig();
        CreateDirtyChunkManager();
        CreatePixelConfigMap();
    }
    
    //需在worldConfig创建之后调用
    private void CreateDirtyChunkManager()
    {
        dirtyChunkManager = new DirtyChunkManager(Allocator.Persistent, pixelBuffer.buffer, worldConfig, maxChunkSize, chunkBorder, gridSize);
        dirtyChunkManager.AddChunk(new(0, 0, worldConfig.width, worldConfig.height));
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        em.CreateSingleton(dirtyChunkManager);
    }
        
    //添加PixelConfigMap单例组件,由于其包含NativeContainer,所以缓存该组件，在Destroy时释放
    private void CreatePixelConfigMap()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var pixelConfigs = pixelSet.configs;
        pixelLookup = new PixelConfigLookup(pixelConfigs.Length, Allocator.Persistent);
        foreach (var config in pixelConfigs)
        {
            pixelLookup.AddConfig(config.type, config);
        }
        em.CreateSingleton(pixelLookup);
    }

    private void CreateWorldConfig()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        worldConfig = new()
        {
            width = worldWidth,
            height = worldHeight
        };
        em.CreateSingleton(worldConfig);
    }
}
