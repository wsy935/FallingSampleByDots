using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FallingSandDemo : MonoBehaviour
{
    [Header("世界设置")]
    [SerializeField] private int worldWidth = 256;
    [SerializeField] private int worldHeight = 256;
    [SerializeField] private int chunkSize = 32;
    [SerializeField] private int borderWidth = 1;

    private void Init()
    {
        CreateChunk();
    }    
    
    private void CreateChunk()
    {
        int2 chunkCnt = new(Mathf.CeilToInt(worldWidth / chunkSize), Mathf.CeilToInt(worldHeight / chunkSize));
        for (int i = 0; i < chunkCnt.y; i++)
        {
            for (int j = 0; j < chunkCnt.x; j++)
            {
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                var entity = em.CreateEntity();
                em.AddComponentData(entity, new PixelChunk()
                {
                    pos = new(j, i),
                    size = chunkSize,
                    borderWidth = this.borderWidth,
                    isDirty = false
                });
                var buffer = em.AddBuffer<PixelBuffer>(entity);
                buffer.Capacity = (int)Math.Sqrt(chunkSize + borderWidth);
                for(int k = 0; k < buffer.Length; k++)
                {
                    buffer.Add(new PixelBuffer() { type=PixelType.Empty});
                }
            }
        }
    }
}
