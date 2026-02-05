using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
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

        [BurstCompile]
        public void HandleDirtyChunk(ref DirtyChunk dirtyChunk)
        {
            bool hasChange = false;
            int4 expandDis = new();//左右上下

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
                        CheckRectExpand(j, i, in rect, ref expandDis);
                    }
                }
            }
            var worldRect = new Rect(0, 0, worldConfig.width, worldConfig.height);
            rect.Expand(expandDis);
            dirtyChunk.rect = rect.Clamp(worldRect);

            //处理扩展块
            bool hasExpand = expandDis.w != 0 || expandDis.x != 0 || expandDis.y != 0 || expandDis.z != 0;
            while (hasExpand)
            {
                int4 curExpandDis = new();
                Rect curRect = dirtyChunk.rect;
                if (expandDis.w != 0)
                {
                    for (int i = curRect.y; i < curRect.MaxY; i++)
                    {
                        for (int j = 1; j < expandDis.w; j++)
                        {
                            int curX = curRect.x - j;
                            if (TrySimulate(curX, i))
                            {
                                CheckRectExpand(curX, i, in curRect, ref curExpandDis);
                            }
                        }
                    }
                }
                if (expandDis.x != 0)
                {
                    for (int i = curRect.y; i < curRect.MaxY; i++)
                    {
                        for (int j = 1; j < expandDis.w; j++)
                        {
                            int curX = curRect.MaxX - 1 + j;
                            if (TrySimulate(curX, i))
                            {
                                CheckRectExpand(curX, i, in curRect, ref curExpandDis);
                            }
                        }
                    }
                }

                if (expandDis.y != 0)
                {
                    for (int i = 1; i < expandDis.y; i++)
                    {
                        for (int j = curRect.x; j < curRect.MaxX; j++)
                        {
                            int curY = curRect.MaxY - 1 + i;
                            if (TrySimulate(j, curY))
                            {
                                CheckRectExpand(j, curY, in curRect, ref curExpandDis);
                            }
                        }
                    }
                }
                if (expandDis.z != 0)
                {
                    for (int i = 1; i < expandDis.y; i++)
                    {
                        for (int j = curRect.x; j < curRect.MaxX; j++)
                        {
                            int curY = curRect.y - i;
                            if (TrySimulate(j, curY))
                            {
                                CheckRectExpand(j, curY, in curRect, ref curExpandDis);
                            }
                        }
                    }
                }

                hasExpand = expandDis.w != 0 || expandDis.x != 0 || expandDis.y != 0 || expandDis.z != 0;
                curRect.Expand(curExpandDis);
                dirtyChunk.rect = curRect.Clamp(worldRect);
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
        private bool TrySimulate(int x, int y)
        {
            if (bitMap.IsMark(x, y)) return false;
            bitMap.Mark(x, y);
            int idx = worldConfig.CoordsToIdx(x, y);
            if (handler.buffer[idx].frameIdx == handler.frameIdx) return false;
            handler.HandleMove(x, y);
            return handler.buffer[idx].frameIdx == handler.frameIdx;
        }

        private void CheckRectExpand(int x, int y, in Rect rect, ref int4 expandDis)
        {
            int idx = worldConfig.CoordsToIdx(x, y);
            var config = pixelConfigLookup.GetConfig(handler.buffer[idx].type);
            if (x == rect.x && expandDis.w != 0)
            {
                expandDis.w = math.max(expandDis.w, config.speed.x + 1);
            }
            else if (x == rect.MaxX - 1 && expandDis.x != 0)
            {
                expandDis.x = math.max(expandDis.x, config.speed.x + 1);
            }

            if (y == rect.MaxY - 1 && expandDis.y != 0)
            {
                expandDis.y = math.max(expandDis.y, config.speed.y + 1);
            }
            else if (y == rect.y && expandDis.z != 0)
            {
                expandDis.y = math.max(expandDis.z, config.speed.y + 1);
            }
        }
    }
}
