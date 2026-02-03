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
        private int stepTimes;
        private WorldConfig worldConfig;
        private uint frameIdx;
        private DirtyChunkManager dirtyChunkManager;
        private Random random;
        bool isInit;

        public void OnCreate(ref SystemState state)
        {
            isInit = false;
            frameIdx = (uint)DateTime.Now.Ticks;
            random = new(frameIdx);
            state.RequireForUpdate<WorldConfig>();
            state.RequireForUpdate<PixelConfigLookup>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            if (isInit) return;

            dirtyChunkManager = SystemAPI.GetSingleton<DirtyChunkManager>();
            worldConfig = SystemAPI.GetSingleton<WorldConfig>();
            handler = new SimulationHandler
            {
                pixelConfigLookup = SystemAPI.GetSingleton<PixelConfigLookup>(),
                worldConfig = worldConfig,
                buffer = SystemAPI.GetSingleton<PixelBuffer>().buffer,
                random = random
            };
            isInit = true;
        }

        public void OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            frameIdx = frameIdx == uint.MaxValue ? 1 : frameIdx + 1;
            handler.frameIdx = frameIdx;

            //Job可能会扩展Chunk的边界，并且在Job执行前可能会添加重叠的Chunk，如果在Job执行后才同步则可能产生数据竞争            
            //因此需要在job执行之前Reset，确保处理的dirtyChunks
            dirtyChunkManager.Reset();

            var dirtyChunks = dirtyChunkManager.GetDirtyChunks();
            var job = new UpdateDirtyChunkJob()
            {
                dirtyChunks = dirtyChunks,                
                handler = handler
            };
            
            state.Dependency = job.Schedule(dirtyChunks.Length, 1, state.Dependency);
            state.CompleteDependency();                                         
        }
    }

    [BurstCompile]
    public struct UpdateDirtyChunkJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeList<DirtyChunk> dirtyChunks;
        [NativeDisableParallelForRestriction] public SimulationHandler handler;        

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
                int j, increment;
                if (handler.random.NextBool() || true)
                {
                    j = minXY.x;
                    increment = 1;
                }
                else
                {
                    j = maxXY.x;
                    increment = -1;
                }
                for (; j < maxXY.x && j>=0; j +=increment)
                {
                    int idx = handler.worldConfig.CoordsToIdx(j, i);
                    PixelData cur = handler.buffer[idx];
                    if (handler.frameIdx == cur.frameIdx) continue;
                    
                    handler.HandleMove(j, i);
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
