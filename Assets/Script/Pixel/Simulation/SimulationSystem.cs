using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using System;

using Random = Unity.Mathematics.Random;
namespace Pixel
{
    [BurstCompile]
    public partial struct SimulationSystem : ISystem, ISystemStartStop
    {
        private SimulationHandler handler;
        private WorldConfig worldConfig;
        private uint frameIdx;        
        private DirtyChunkManager dirtyChunkManager;
        bool isInit;

        public void OnCreate(ref SystemState state)
        {
            isInit = false;
            frameIdx = (uint)DateTime.Now.Ticks;
            state.RequireForUpdate<WorldConfig>();
            state.RequireForUpdate<PixelConfigLookup>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;

            var fsw = FallingSandWorld.Instance;
            dirtyChunkManager = SystemAPI.GetSingleton<DirtyChunkManager>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            handler = new SimulationHandler
            {
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                worldConfig = worldConfig,
                buffer = fsw.PixelBuffer,
            };
            isInit = true;
        }

        public void OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            frameIdx = frameIdx == uint.MaxValue ? 1 : frameIdx + 1;
            handler.frameIdx = frameIdx;

            dirtyChunkManager.Clear();            
            var dirtyChunks = dirtyChunkManager.GetDirtyChunks();
            var job = new UpdateDirtyChunkJob()
            {
                dirtyChunks = dirtyChunks,
                random = new(frameIdx),
                handler = handler
            };

            state.Dependency = job.Schedule(dirtyChunks.Length, 1, state.Dependency);
            state.CompleteDependency();
            dirtyChunkManager.MergeChunk();
            dirtyChunkManager.SplitChunk();
        }
    }

    [BurstCompile]
    public struct UpdateDirtyChunkJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeList<DirtyChunk> dirtyChunks;
        [NativeDisableParallelForRestriction] public SimulationHandler handler;
        [ReadOnly] public Random random;

        [BurstCompile]
        public void Execute(int index)
        {
            var dirtyChunk = dirtyChunks[index];
            bool hasChange = false;
            bool isUpExpand = false;
            bool isDownExpand = false;
            bool isLeftExpand = false;
            bool isRightExpand = false;
            int2 minXY = new(dirtyChunk.rect.x, dirtyChunk.rect.y);
            int2 maxXY = new(minXY.x + dirtyChunk.rect.width, minXY.y + dirtyChunk.rect.height);
            for (int i = minXY.y; i < maxXY.y; i++)
            {
                for (int j = minXY.x; j < maxXY.x; j++)
                {
                    int idx = handler.worldConfig.CoordsToIdx(j, i);
                    PixelData cur = handler.buffer[idx];
                    if (handler.frameIdx == cur.frameIdx) continue;                    

                    random.InitState(handler.frameIdx + math.hash(new int2(j, i)));
                    handler.HandleMove(j, i, random);
                    if (handler.buffer[idx].frameIdx == handler.frameIdx)
                    {
                        if (!hasChange)
                            hasChange = true;
                        if (j == minXY.x && !isLeftExpand)
                        {
                            dirtyChunk.rect.x -= 1;
                            isLeftExpand = true;
                        }
                        else if (j == maxXY.x-1 && !isRightExpand)
                        {
                            dirtyChunk.rect.width += 1;
                            isRightExpand = true;
                        }

                        if (i == minXY.y && !isDownExpand)
                        {
                            dirtyChunk.rect.y -= 1;
                            isDownExpand = true;
                        }
                        else if (i == maxXY.y-1 && !isUpExpand)
                        {
                            dirtyChunk.rect.height += 1;
                            isUpExpand = true;
                        }
                    }
                }
            }

            dirtyChunk.rect = dirtyChunk.rect.Clamp(new Rect(0, 0, handler.worldConfig.width, handler.worldConfig.height));
            bool isDirty = hasChange || isUpExpand || isDownExpand || isRightExpand || isLeftExpand;
            if (!isDirty)
            {
                dirtyChunk.notDirtyFrame++;
            }
            else
            {
                dirtyChunk.notDirtyFrame = 0;
            }
            dirtyChunk.isDirty = isDirty;
            dirtyChunks[index] = dirtyChunk;
        }
    }
}
