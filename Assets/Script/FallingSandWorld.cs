using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Pixel;
public class FallingSandWorld : MonoBehaviour
{
    [Header("世界设置")]
    [SerializeField] private int worldWidth = 256;
    [SerializeField] private int worldHeight = 256;
    [SerializeField] private int chunkEdge = 8;
    [SerializeField] private int borderSize = 1;
    [SerializeField] private PixelSet pixelSet;

    public static FallingSandWorld Instance { get; private set; }
    
    public int ChunkEdge => chunkEdge;
    public int ChunkBorderSize => borderSize;
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
    }

    void Start()
    {
        CreateChunk();
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
                var buffer = em.AddBuffer<PixelBuffer>(entity);
                buffer.Capacity = (int)Math.Sqrt(chunkEdge + borderSize);
                for (int k = 0; k < buffer.Length; k++)
                {
                    buffer.Add(new PixelBuffer() { type = PixelType.Empty });
                }
            }
        }
    }
}
