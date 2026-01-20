using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{
    [BurstCompile]
    public struct ChunkConfig
    {
        public int edge;
        public int2 chunkCount; // Chunk网格的行列数

        [BurstCompile]
        public int CoordsToIdx(int x, int y)
        {
            return y * edge + x;
        }

        [BurstCompile]
        public int ChunkPosToIdx(int2 chunkPos)
        {
            return chunkPos.y * chunkCount.x + chunkPos.x;
        }
    }

    public struct PixelConfig
    {
        public static PixelConfig Empty => new();
        public PixelType type;
        public PixelType interactionMask;
        public FunctionPointer<SimulationHandler> handler;

        public PixelConfig(PixelType type, PixelType interactionMask, FunctionPointer<SimulationHandler> handler)
        {
            this.type = type;
            this.interactionMask = interactionMask;
            this.handler = handler;
        }
    }

    [BurstCompile]
    public struct PixelConfigMap : IDisposable
    {
        private readonly NativeHashMap<int, PixelConfig> configs;
        public bool isCreated;

        public PixelConfigMap(int length)
        {
            configs = new(length, Allocator.Persistent);
            isCreated = true;
        }

        public void AddConfig(PixelType type, PixelConfig config)
        {
            int key = (int)type;
            if (configs.ContainsKey(key))
            {
                Debug.LogError($"there alerady has a key {type} in Addconfig");
            }
            else
            {
                configs.Add(key, config);
            }
        }

        [BurstCompile]
        public PixelConfig GetConfig(PixelType type)
        {
            if (configs.TryGetValue((int)type, out var config))
                return config;
            else
            {
                return PixelConfig.Empty;
            }
        }

        public void Dispose()
        {
            if (configs.IsCreated)
            {
                isCreated = false;
                configs.Dispose();
            }
        }
    }
}
