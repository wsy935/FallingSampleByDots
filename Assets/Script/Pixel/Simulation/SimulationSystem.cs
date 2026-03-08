using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System;

using Random = Unity.Mathematics.Random;
using Unity.Jobs;
using UnityEngine;
using System.Runtime.CompilerServices;
using UnityEngine.Profiling;
namespace Pixel
{
    [BurstCompile]
    public partial struct SimulationSystem : ISystem
    {
        private uint frameIdx;
        private Random random;
        private BitMap bitMap;

        public void OnCreate(ref SystemState state)
        {
            frameIdx = (uint)DateTime.Now.Ticks;
            random = new(frameIdx);
            state.RequireForUpdate<WorldConfig>();
            state.RequireForUpdate<PixelConfigLookup>();
        }

        public void OnDestroy(ref SystemState state)
        {
            bitMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            frameIdx = frameIdx == uint.MaxValue ? 1 : frameIdx + 1;
            var handler = new SimulationHandler()
            {
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                buffer = SystemAPI.GetSingletonBuffer<PixelData>(),
                chunks = SystemAPI.GetSingletonBuffer<Chunk>(),
                worldConfig = SystemAPI.GetSingleton<WorldConfig>(),
                frameIdx = frameIdx,
                random = random
            };

            var job = new SimulateJob
            {
                handler = handler,
            };
            for (int i = 0; i < 12; i++)
            {
                job.stats = i / 4;
                job.updateBlack = (i & 1) == 0;
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
        //0 更新垂直落体，1更新斜向，2更新水平运动
        public int stats;

        [BurstCompile]
        public void Execute(int index)
        {
            var chunk = handler.chunks[index];
            if (updateBlack != chunk.isBlack) return;
            // if (!chunk.isDirty) return;

            var worldConfig = handler.worldConfig;
            int chunkSize = worldConfig.chunkSize;
            bool hasChange = false;
            for (int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkSize; j++)
                {
                    int2 pos = worldConfig.GetCoordsByChunk(chunk.pos, j, i);
                    int idx = worldConfig.CoordsToIdx(pos.x, pos.y);
                    var pixel = handler.buffer[idx];
                    if (pixel.frameIdx == handler.frameIdx)
                    {
                        hasChange = true;
                        continue;
                    }

                    PixelConfig config = handler.pixelConfigLookup.GetConfig(pixel.type);
                    if (config.isStatic)
                        continue;
                    var newPos = stats switch
                    {
                        0 => handler.MoveVertical(pos.x, pos.y, config),
                        1 => handler.MoveDiagonal(pos.x, pos.y, config),
                        2 => handler.MoveHorizontal(pos.x, pos.y, config),
                        _ => pos
                    };


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

            // chunk.isDirty = hasChange;
            // handler.chunks[index] = chunk;
        }

        private void NotifyNeighBour(int idx)
        {
            if (idx < 0 || idx >= handler.chunks.Length) return;
            var neighbourChunk = handler.chunks[idx];
            neighbourChunk.isDirty = true;
            handler.chunks[idx] = neighbourChunk;
        }
    }
}
