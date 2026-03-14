using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using System;


namespace Pixel
{

    [UpdateAfter(typeof(PretreatmentSystem))]
    public partial struct ReactionSystem : ISystem
    {
        private uint timeOffset;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldConfig>();
            state.RequireForUpdate<PixelConfigLookup>();
            timeOffset = (uint)DateTime.Now.Ticks;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var handler = new ReactionHandler
            {
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                buffer = SystemAPI.GetSingletonBuffer<PixelData>(),
                worldConfig = SystemAPI.GetSingleton<WorldConfig>(),
                chunks = SystemAPI.GetSingletonBuffer<Chunk>(),
                random = new(timeOffset + (uint)Time.frameCount),
                frameCount = Time.frameCount
            };

            var job = new ReactionJob
            {
                handler = handler,
            };

            state.Dependency = job.Schedule(handler.chunks.Length, 4, state.Dependency);
            state.CompleteDependency();
        }
    }

    [BurstCompile]
    public struct ReactionJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public ReactionHandler handler;

        [BurstCompile]
        public void Execute(int index)
        {
            var chunk = handler.chunks[index];
            var worldConfig = handler.worldConfig;
            int chunkSize = worldConfig.chunkSize;

            for (int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkSize; j++)
                {
                    int2 pos = worldConfig.GetCoordsByChunk(chunk.pos, j, i);

                    // 检查位置是否在世界范围内
                    if (!worldConfig.IsInWorld(pos.x, pos.y))
                        continue;

                    handler.ProcessReaction(pos.x, pos.y);
                }
            }
        }
    }
}