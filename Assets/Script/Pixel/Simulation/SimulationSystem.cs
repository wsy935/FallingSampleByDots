using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using System;

using Random = Unity.Mathematics.Random;
using NUnit.Framework;
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
        private PixelBuffer pixelBuffer;
        private DirtyChunkManager dirtyChunkManager;
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
            pixelBuffer = SystemAPI.GetSingleton<PixelBuffer>();
            dirtyChunkManager = SystemAPI.GetSingleton<DirtyChunkManager>();
            handler = new SimulationHandler
            {
                pixelConfigLookup = pixelConfigLookup,
                worldConfig = worldConfig,
                buffer = pixelBuffer.buffer,
                random = random
            };
            bitMap = new(worldConfig.width, worldConfig.height, Allocator.Persistent);
        }

        public void OnStopRunning(ref SystemState state) { }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            dirtyChunkManager.Reset();
            var dirtyChunks = dirtyChunkManager.GetDirtyChunks();
            for (int i = 0; i < handler.worldConfig.stepTimes; i++)
            {
                frameIdx = frameIdx == uint.MaxValue ? 1 : frameIdx + 1;
                handler.frameIdx = frameIdx;
                bitMap.Clear();                
                for (int j = 0; j < dirtyChunks.Length; j++)
                {
                    HandleDirtyChunk(ref dirtyChunks.ElementAt(j));
                }
            }
        }

        public void HandleDirtyChunk(ref DirtyChunk dirtyChunk)
        {            
            bool hasChange = false;
            bool isUpExpand = false;
            bool isDownExpand = false;
            bool isLeftExpand = false;
            bool isRightExpand = false;

            //处理当前块
            Rect rect = dirtyChunk.rect;
            for (int i = rect.y; i < rect.MaxY; i++)
            {
                int j, increment;
                if (handler.random.NextBool())
                {
                    j = rect.x;
                    increment = 1;
                }
                else
                {
                    j = rect.MaxX - 1;
                    increment = -1;
                }
                for (; j < rect.MaxX && j >= rect.x; j += increment)
                {
                    if (TrySimulate(j, i))
                    {
                        hasChange = true;
                        CheckRectExpand(j, i, ref dirtyChunk.rect, ref isLeftExpand, ref isRightExpand, ref isDownExpand, ref isUpExpand);
                    }
                }
            }
            var worldRect = new Rect(0, 0, worldConfig.width, worldConfig.height);
            dirtyChunk.rect = dirtyChunk.rect.Clamp(worldRect);

            //处理扩展块
            bool hasExpand = isUpExpand || isDownExpand || isRightExpand || isLeftExpand;
            while (hasExpand )
            {
                bool curLeft, curRight, curUp, curDown;
                curLeft = curDown = curRight = curUp = false;                
                Rect curRect = dirtyChunk.rect;
                if (isUpExpand)
                {
                    int y = curRect.MaxY - 1;
                    for (int x = curRect.x; x < curRect.MaxX; x++)
                    {
                        if (TrySimulate(x, y))
                        {
                            CheckRectExpand(x, y, ref dirtyChunk.rect, ref curLeft, ref curRight, ref curDown, ref curUp);
                        }
                    }
                }
                if (isDownExpand)
                {
                    int y = curRect.y;
                    for (int x = curRect.x; x < curRect.MaxX; x++)
                    {
                        if (TrySimulate(x, y))
                        {
                            CheckRectExpand(x, y, ref dirtyChunk.rect, ref curLeft, ref curRight, ref curDown, ref curUp);
                        }
                    }
                }
                if (isLeftExpand)
                {
                    int x =curRect.x;
                    for (int y = curRect.y; y < curRect.MaxY; y++)
                    {
                        if (TrySimulate(x, y))
                        {
                            CheckRectExpand(x, y, ref dirtyChunk.rect, ref curLeft, ref curRight, ref curDown, ref curUp);
                        }
                    }
                }
                if (isRightExpand)
                {
                    int x = curRect.MaxX-1;
                    for (int y = curRect.y; y < curRect.MaxY; y++)
                    {
                        if (TrySimulate(x, y))
                        {
                            CheckRectExpand(x, y, ref dirtyChunk.rect, ref curLeft, ref curRight, ref curDown, ref curUp);
                        }
                    }
                }
                isUpExpand = curUp;
                isRightExpand = curRight;
                isLeftExpand = curLeft;
                isDownExpand = curDown;
                hasExpand = isUpExpand || isDownExpand || isRightExpand || isLeftExpand;
                dirtyChunk.rect = dirtyChunk.rect.Clamp(worldRect);
            }
            
            bool isDirty = hasChange;
            if (!isDirty)
            {
                dirtyChunk.notDirtyFrame++;
            }
            else
            {
                dirtyChunk.notDirtyFrame = 0;
            }
            dirtyChunk.isDirty = isDirty;            
        }

        /// <summary>
        /// 返回模拟的结果，如果像素在模拟之后被设置返回true，否则为false
        /// </summary>
        [BurstCompile]
        private bool TrySimulate(int x, int y)
        {
            if (bitMap.IsMark(x, y)) return false;
            bitMap.Mark(x, y);
            int idx = worldConfig.CoordsToIdx(x, y);
            if (handler.buffer[idx].frameIdx == handler.frameIdx) return false;
            handler.HandleMove(x, y);            
            return handler.buffer[idx].frameIdx == handler.frameIdx;
        }

        [BurstCompile]
        private void CheckRectExpand(int x, int y, ref Rect rect, ref bool isLeftExpand,
            ref bool isRightExpand, ref bool isDownExpand, ref bool isUpExpand)
        {
            if (x == rect.x && !isLeftExpand)
            {
                rect.x -= 1;
                rect.width += 1;
                isLeftExpand = true;
            }        
            else if (x == rect.MaxX - 1 && !isRightExpand)
            {
                rect.width += 1;
                isRightExpand = true;
            }

            if (y == rect.y && !isDownExpand)
            {
                rect.y -= 1;
                rect.height += 1;
                isDownExpand = true;
            }
            else if (y == rect.MaxY - 1 && !isUpExpand)
            {
                rect.height += 1;
                isUpExpand = true;
            }
        }
    }
}
