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
    [SerializeField] private int borderWidth = 1;
    [SerializeField] private PixelSet pixelSet;

    void Start()
    {
        CreateChunk();
        CreateWorldConfig();
    }

    private void CreatePixelSOConfig()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        foreach(var e in pixelSet.pixels)
        {
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new PixelSOConfig()
            {
                type = e.type,
                interactionMask = e.interactionMask
            });
        }
    }

    private void CreateWorldConfig()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;        
        WorldConfig worldConfig = new()
        {
            Width = worldWidth,
            Height = worldHeight,            
        };        
        em.CreateSingleton(worldConfig);
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
                buffer.Capacity = (int)Math.Sqrt(chunkEdge + borderWidth);
                for (int k = 0; k < buffer.Length; k++)
                {
                    buffer.Add(new PixelBuffer() { type = PixelType.Empty });
                }
            }
        }
    }
}
