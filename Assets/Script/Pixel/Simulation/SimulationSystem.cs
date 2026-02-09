using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System;

using Random = Unity.Mathematics.Random;
using Unity.Jobs;
using UnityEngine;
using System.Runtime.CompilerServices;
namespace Pixel
{
    [BurstCompile]
    public partial struct SimulationSystem : ISystem, ISystemStartStop
    {
        private uint frameIdx;
        private Random random;
        private BitMap bitMap;
        private WorldConfig worldConfig;
        private PixelConfigLookup pixelConfigLookup;
        private DynamicBuffer<PixelData> pixelBuffer;
        private DynamicBuffer<Chunk> chunks;
        private SimulationHandler handler;
        bool isInit;

        public void OnCreate(ref SystemState state)
        {
            isInit = false;
            frameIdx = (uint)DateTime.Now.Ticks;
            random = new(frameIdx);
            state.RequireForUpdate<WorldConfig>();
            state.RequireForUpdate<PixelConfigLookup>();
        }

        public void OnDestroy(ref SystemState state)
        {
            bitMap.Dispose();
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;
            isInit = true;
            pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            pixelBuffer = SystemAPI.GetSingletonBuffer<PixelData>();
            chunks = SystemAPI.GetSingletonBuffer<Chunk>();

            handler = new SimulationHandler
            {
                pixelConfigLookup = pixelConfigLookup,
                worldConfig = worldConfig,
                chunks = chunks,
                buffer = pixelBuffer,
                random = random
            };
            bitMap = new(worldConfig.width, worldConfig.height, Allocator.Persistent);
        }

        public void OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            frameIdx = frameIdx == uint.MaxValue ? 1 : frameIdx + 1;
            handler.frameIdx = frameIdx;

            for (int i = 0; i < 4; i++)
            {
                var job = new SimulateJob
                {
                    handler = handler,
                    updateBlack = (i & 1) == 0
                };
                state.Dependency = job.Schedule(handler.chunks.Length, 4, state.Dependency);
                state.CompleteDependency();
            }
        }
    }

    [BurstCompile]
    public struct SimulateJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public SimulationHandler handler;
        public bool updateBlack;

        [BurstCompile]
        public void Execute(int index)
        {
            var chunk = handler.chunks[index];
            if (updateBlack != chunk.isBlack) return;
            if (!chunk.isDirty) return;

            var worldConfig = handler.worldConfig;
            int chunkSize = worldConfig.chunkSize;
            bool hasChange = false;
            for (int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkSize; j++)
                {
                    int2 pos = worldConfig.GetCoordsByChunk(chunk.pos, j, i);
                    if (!worldConfig.IsInWorld(pos.x, pos.y)) continue;

                    int idx = worldConfig.CoordsToIdx(pos.x, pos.y);
                    var pixel = handler.buffer[idx];
                    if (pixel.frameIdx == handler.frameIdx)
                    {
                        hasChange = true;
                        continue;
                    }
                    PixelConfig config = handler.pixelConfigLookup.GetConfig(pixel.type);

                    var newPos = handler.HandleMove(pos.x, pos.y, config);

                    if (math.any(pos != newPos))
                    {
                        hasChange = true;
                        //跨越到其他块
                        int chunkIdx = worldConfig.GetChunkIdx(newPos.x, newPos.y);
                        if (chunkIdx != index)
                            NotifyNeighBour(chunkIdx);

                        //当前块的边界更新时需要设置邻居
                        if (j == 0)
                            NotifyNeighBour(index - 1);
                        else if (j == chunkSize - 1)
                            NotifyNeighBour(index + 1);

                        if (i == 0)
                            NotifyNeighBour(index - worldConfig.chunkCnt.x);
                        else if (i == chunkSize - 1)
                            NotifyNeighBour(index + worldConfig.chunkCnt.x);
                    }
                }
            }
            if (hasChange)
            {
                chunk.notDirtyFrame = 0;
                chunk.isDirty = hasChange;
            }
            else
            {
                chunk.notDirtyFrame++;
                if (chunk.notDirtyFrame > 1)
                    chunk.isDirty = false;
            }
            handler.chunks[index] = chunk;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyNeighBour(int idx)
        {
            if (idx < 0 || idx >= handler.chunks.Length) return;
            var neighbourChunk = handler.chunks[idx];
            neighbourChunk.isDirty = true;
            handler.chunks[idx] = neighbourChunk;
        }
    }
}
