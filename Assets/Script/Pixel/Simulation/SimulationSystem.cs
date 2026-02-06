using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System;

using Random = Unity.Mathematics.Random;
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
            frameIdx = frameIdx == uint.MaxValue ? 1 : frameIdx + 1;
            dirtyChunkManager.Reset();
            var dirtyChunks = dirtyChunkManager.GetDirtyChunks();
            handler.frameIdx = frameIdx;
            bitMap.Clear();
            for (int j = 0; j < dirtyChunks.Length; j++)
            {
                HandleDirtyChunk(ref dirtyChunks.ElementAt(j));
            }
        }

        [BurstCompile]
        public void HandleDirtyChunk(ref DirtyChunk dirtyChunk)
        {
            Rect worldRect = worldConfig.Size;
            BorderExpand expand = new();

            // 第一轮：模拟整个脏块
            bool hasChange = false;
            Rect currentRect = dirtyChunk.rect;
            SimulateRect(currentRect, ref hasChange, ref expand);

            // 迭代扩张：只处理新扩张的边缘条带                        
            while (expand.HasExpansion)
            {
                Rect oldRect = currentRect;
                currentRect = currentRect.Expand(expand).Clamp(worldRect);
                expand.Reset();

                // 模拟四条边缘条带（新扩张出来的区域）
                SimulateEdgeStrips(oldRect, currentRect, ref hasChange, ref expand);
            }

            dirtyChunk.rect = currentRect;
            if (!hasChange)
            {
                dirtyChunk.notDirtyFrame++;
            }
            else
            {
                dirtyChunk.notDirtyFrame = 0;
            }
            dirtyChunk.isDirty = hasChange || true;
        }

        /// <summary>
        /// 模拟指定矩形区域内的所有像素，并记录边界扩张需求
        /// </summary>
        [BurstCompile]
        private void SimulateRect(Rect rect, ref bool hasChange, ref BorderExpand expand)
        {
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
                    if (bitMap.IsMark(j, i)) continue;
                    bitMap.Mark(j, i);

                    int idx = worldConfig.CoordsToIdx(j, i);
                    //如果在当前帧被模拟过则跳过
                    if (handler.buffer[idx].frameIdx == handler.frameIdx) continue;

                    var config = pixelConfigLookup.GetConfig(handler.buffer[idx].type);
                    if (config.moveFlag == MoveFlag.Nothing) continue;

                    int2 target = handler.HandleMove(j, i, config);
                    
                    //如果发生了移动
                    if (target.x != j || target.y != i)
                    {
                        hasChange = true;
                        CheckBorderExpand(target, in rect, ref expand);
                    }                    
                }
            }
        }

        /// <summary>
        /// 模拟新扩张的边缘条带区域
        /// </summary>
        [BurstCompile]
        private void SimulateEdgeStrips(Rect oldRect, Rect newRect, ref bool hasChange, ref BorderExpand expand)
        {
            // 下条带（包含左下、右下角落，跨整个 newRect 宽度）
            if (newRect.y < oldRect.y)
            {
                Rect bottomStrip = new(newRect.x, newRect.y, newRect.width, oldRect.y - newRect.y);
                SimulateRect(bottomStrip, ref hasChange, ref expand);
            }

            // 左条带（不含上下角落）
            if (newRect.x < oldRect.x)
            {
                Rect leftStrip = new(newRect.x, oldRect.y, oldRect.x - newRect.x, oldRect.height);
                SimulateRect(leftStrip, ref hasChange, ref expand);
            }

            // 右条带（不含上下角落）
            if (newRect.MaxX > oldRect.MaxX)
            {
                Rect rightStrip = new(oldRect.MaxX, oldRect.y, newRect.MaxX - oldRect.MaxX, oldRect.height);
                SimulateRect(rightStrip, ref hasChange, ref expand);
            }

            // 上条带（包含左上、右上角落，跨整个 newRect 宽度）
            if (newRect.MaxY > oldRect.MaxY)
            {
                Rect topStrip = new(newRect.x, oldRect.MaxY, newRect.width, newRect.MaxY - oldRect.MaxY);
                SimulateRect(topStrip, ref hasChange, ref expand);
            }
        }
        
        /// <summary>
        /// 根据像素的目标位置判断是否需要扩张脏矩形边界
        /// </summary>
        private void CheckBorderExpand(int2 target, in Rect rect, ref BorderExpand expand)
        {            
            if (target.x <= rect.x)
            {
                expand.left = math.max(expand.left, rect.x-target.x +1);
            }
            if (target.x >= rect.MaxX - 1)
            {
                expand.right = math.max(expand.right,target.x-rect.MaxX+2);
            }
            if (target.y >= rect.MaxY - 1)
            {
                expand.top = math.max(expand.top, target.y-rect.MaxY + 2);
            }
            if (target.y <= rect.y)
            {
                expand.bottom = math.max(expand.bottom, rect.y-target.y + 1);
            }
        }
    }
}
