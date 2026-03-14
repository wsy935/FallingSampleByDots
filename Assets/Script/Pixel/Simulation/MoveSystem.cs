using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System;

using Unity.Jobs;
using UnityEngine;

namespace Pixel
{
    [BurstCompile]
    [UpdateAfter(typeof(ReactionSystem))]
    public partial struct MoveSystem : ISystem
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
            var handler = new MoveHandler()
            {
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                buffer = SystemAPI.GetSingletonBuffer<PixelData>(),
                chunks = SystemAPI.GetSingletonBuffer<Chunk>(),
                worldConfig = SystemAPI.GetSingleton<WorldConfig>(),
                frameCount = Time.frameCount,
                random = new(timeOffset + (uint)Time.frameCount)
            };

            var job = new MoveJob
            {
                handler = handler,
                frameCount = Time.frameCount
            };
            for (int i = 0; i < 8; i++)
            {
                job.stats = i / 4;
                job.updateBlack = (i & 1) == 0;
                state.Dependency = job.Schedule(handler.chunks.Length, 4, state.Dependency);
                state.CompleteDependency();
            }
        }
    }

    [BurstCompile]
    public struct MoveJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public MoveHandler handler;
        public bool updateBlack;
        public int frameCount;
        //0 更新垂直落体，1更新斜向+水平运动
        public int stats;

        [BurstCompile]
        public void Execute(int index)
        {
            var chunk = handler.chunks[index];
            if (updateBlack != chunk.isBlack) return;
            if (!chunk.IsDirty(frameCount)) return;

            var worldConfig = handler.worldConfig;
            int2 originPos = worldConfig.GetCoordsByChunk(chunk.pos, 0, 0);
            int chunkSize = worldConfig.chunkSize;
            bool hasChange = false;
            for (int i = 0; i < chunkSize; i++)
            {
                for (int j = 0; j < chunkSize; j++)
                {
                    int2 pos = worldConfig.GetCoordsByChunk(chunk.pos, j, i);
                    int idx = worldConfig.CoordsToIdx(pos.x, pos.y);
                    var pixel = handler.buffer[idx];
                    if (pixel.frameIdx == frameCount)
                    {
                        hasChange = true;
                        continue;
                    }

                    PixelConfig config = handler.pixelConfigLookup.GetConfig(pixel.type);
                    if (config.isStatic)
                        continue;
                    var newPos = pos;
                    switch (stats)
                    {
                        case 0:
                            newPos = handler.MoveVertical(pos.x, pos.y, config);
                            break;
                        case 1:
                            newPos = handler.MoveDiagonal(pos.x, pos.y, config);
                            if (math.all(newPos == pos))
                                newPos = handler.MoveHorizontal(pos.x, pos.y, config);
                            break;
                    }

                    if (math.any(pos != newPos))
                    {
                        hasChange = true;

                        //当前块的边界更新或者移动到边界外时需要设置邻居
                        if (j == 0 || newPos.x <= originPos.x)
                            NotifyNeighBour(new(chunk.pos.x - 1, chunk.pos.y));
                        else if (j == chunkSize - 1 || newPos.x >= originPos.x + worldConfig.chunkSize - 1)
                            NotifyNeighBour(new(chunk.pos.x + 1, chunk.pos.y));

                        if (i == 0 || newPos.y <= originPos.y)
                            NotifyNeighBour(new(chunk.pos.x, chunk.pos.y - 1));
                        else if (i == chunkSize - 1 || newPos.y >= originPos.y + worldConfig.chunkSize - 1)
                            NotifyNeighBour(new(chunk.pos.x, chunk.pos.y + 1));
                    }
                }
            }
            switch (stats)
            {
                case 0:
                    chunk.isPhase1Change = hasChange;
                    break;
                case 1:
                    chunk.isPhase2Change = hasChange;
                    break;                
            }
            handler.chunks[index] = chunk;
        }

        private void NotifyNeighBour(int2 chunkPos)
        {
            int idx = handler.worldConfig.GetChunkIdxByChunkPos(chunkPos);
            if (idx == -1) return;
            var neighbourChunk = handler.chunks[idx];
            neighbourChunk.forceDiryFrame = frameCount;
            handler.chunks[idx] = neighbourChunk;
        }
    }
}
