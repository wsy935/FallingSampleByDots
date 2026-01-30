using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Pixel;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

public class FallingSandWorld : MonoBehaviour
{
    [Header("世界设置")]
    [SerializeField] private int worldWidth = 256;
    [SerializeField] private int worldHeight = 256;
    [SerializeField] private PixelSet pixelSet;
    private PixelConfigLookup pixelLookup;
    private WorldConfig worldConfig;
    //NativeArray是值类型，修改时需要重新赋值，所以放弃使用二维数组
    private NativeArray<PixelType> pixelBuffer;
    public static FallingSandWorld Instance { get; private set; }

    public WorldConfig WorldConfig => worldConfig;    
    public NativeArray<PixelType> PixelBuffer => pixelBuffer;

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
    }

    void OnDestroy()
    {
        pixelBuffer.Dispose();
        pixelLookup.Dispose();
    }
    
    private void InitWorld()
    {
        pixelBuffer = new(worldHeight * worldWidth, Allocator.Persistent);
        for (int i = 0; i < pixelBuffer.Length; i++)
            pixelBuffer[i] = PixelType.Empty;
        CreateWorldConfig();
        CreatePixelConfigMap();    
    }

    //添加PixelConfigMap单例组件,由于其包含NativeContainer,所以缓存该组件，在Destroy时释放
    private void CreatePixelConfigMap()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var pixelConfigs = pixelSet.configs;
        pixelLookup = new PixelConfigLookup(pixelConfigs.Length,Allocator.Persistent);
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
