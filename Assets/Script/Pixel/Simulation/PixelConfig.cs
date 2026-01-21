using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{

    [BurstCompile]
    public struct WorldConfig
    {
        public int width;
        public int height;
        public int2 chunkCnt;
        public int chunkEdge;


        [BurstCompile]
        public int CoordsToChunkIdx(int x, int y)
        {
            return y * chunkEdge + x;
        }

        [BurstCompile]
        public int CoordsToWorldIdx(int x, int y, int2 chunkPos)
        {
            int worldX = chunkPos.x * chunkEdge + x;
            int worldY = chunkPos.y * chunkEdge + y;
            return worldY * width + worldX;
        }

        [BurstCompile]
        public int ChunkPosToIdx(int2 chunkPos)
        {
            return chunkPos.y * chunkCnt.x + chunkPos.x;
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
        private NativeArray<PixelConfig> configs;

        public PixelConfigMap(int size)
        {
            configs = new(size, Allocator.Persistent);
        }

        [BurstCompile]
        public void AddConfig(PixelType type, PixelConfig config)
        {
            int key = GetKey(type);
            configs[key] = config;
        }

        [BurstCompile]
        private int GetKey(PixelType pixelType)
        {
            return math.tzcnt((int)pixelType);
        }

        [BurstCompile]
        public PixelConfig GetConfig(PixelType type)
        {
            int key = GetKey(type);
            return configs[key];
        }

        public void Dispose()
        {
            configs.Dispose();
        }
    }
}
