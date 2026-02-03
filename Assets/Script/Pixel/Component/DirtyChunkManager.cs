using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Pixel
{
    [BurstCompile]
    public struct DirtyChunkManager : IComponentData, IDisposable
    {
        private NativeList<DirtyChunk> dirtyChunks;
        //像素缓存数组
        private NativeArray<PixelData> buffer;
        //当块的大小超过maxChunkSize时，尝试拆分块
        private int maxChunkSize;

        private WorldConfig worldConfig;
        //当块与块之间的间距小于chunkBorder时，尝试合并块
        private int chunkBorder;
        //固定网格大小，用于分割大块
        private int gridSize;

        public DirtyChunkManager(Allocator allocator, NativeArray<PixelData> buffer, WorldConfig worldConfig, int maxChunkSize = 128, int chunkBorder = 1, int gridSize = 64)
        {
            dirtyChunks = new(allocator);

            this.buffer = buffer;
            this.worldConfig = worldConfig;
            this.maxChunkSize = maxChunkSize;
            this.chunkBorder = chunkBorder;
            this.gridSize = gridSize;
        }

        public readonly NativeList<DirtyChunk> GetDirtyChunks() => dirtyChunks;

        public void AddChunk(DirtyChunk dirtyChunk)
        {
            dirtyChunks.Add(dirtyChunk);
        }

        /// <summary>
        /// 整理当前脏区块
        /// </summary>
        public void Reset()
        {
            Clear();
            SplitChunk();
            MergeChunk();
        }

        /// <summary>
        /// 移除非脏的区块
        /// </summary>
        [BurstCompile]
        public void Clear()
        {
            for (int i = dirtyChunks.Length-1; i >=0; i--)
            {
                if (!dirtyChunks[i].isDirty && dirtyChunks[i].notDirtyFrame > 1)
                {
                    dirtyChunks.RemoveAtSwapBack(i);
                }
            }
        }

        /// <summary>
        /// 清空所有脏区块
        /// </summary>
        [BurstCompile]
        public void ForceClear()
        {
            dirtyChunks.Clear();
        }

        /// <summary>
        /// 合并相邻的脏区块
        /// </summary>
        [BurstCompile]
        public void MergeChunk()
        {
            if (dirtyChunks.Length < 2) return;
            dirtyChunks.Sort();
            for (int i = 0; i < dirtyChunks.Length; i++)
            {
                int j = i + 1;                
                while (j < dirtyChunks.Length)
                {
                    //在Impl中会被重新设置
                    var chunk1 = dirtyChunks.ElementAt(i);
                    var chunk2 = dirtyChunks.ElementAt(j);
                    if (IsIntersect(in chunk1, in chunk2))
                    {
                        //将j处的矩形移除了，所以j无需变化
                        MergeChunk_Impl(i, j);
                    }
                    else
                        break;
                }
            }
        }

        /// <summary>
        /// 移除下标较大处的区块，将新区块的值赋值到下标较小处
        /// </summary>
        [BurstCompile]
        private void MergeChunk_Impl(int i, int j)
        {
            if (j < i)
                (i, j) = (j, i);
            var chunk1 = dirtyChunks[i];
            var chunk2 = dirtyChunks[j];

            //合并两个矩形
            Rect newRect = Rect.Union(chunk1.rect, chunk2.rect);

            dirtyChunks[i] = new DirtyChunk(newRect, chunk1.isDirty || chunk2.isDirty)
            {
                notDirtyFrame = math.min(chunk1.notDirtyFrame,chunk2.notDirtyFrame)
            };
            
            dirtyChunks.RemoveAt(j);
        }

        /// <summary>
        /// 分割超过最大尺寸的脏区块，按照固定大小划分
        /// </summary>
        [BurstCompile]
        public void SplitChunk()
        {
            NativeList<int> splitChunks = new(4,Allocator.Temp);
            for (int i = 0; i < dirtyChunks.Length; i++)
            {
                var curSize = dirtyChunks[i].rect.Size;
                if (curSize.x >= maxChunkSize || curSize.y >= maxChunkSize)
                {
                    splitChunks.Add(i);
                }
            }
            
            for (int i = splitChunks.Length - 1; i >= 0; i--)
            {
                int idx = splitChunks[i];
                SplitChunk_Impl(idx);
            }

            splitChunks.Dispose();
        }

        /// <summary>
        /// 使用固定网格分割单个chunk
        /// </summary>
        [BurstCompile]
        private void SplitChunk_Impl(int chunkIndex)
        {
            var chunk = dirtyChunks[chunkIndex];
            var chunkRect = chunk.rect;

            // 计算需要划分多少个网格
            int gridCountX = (chunkRect.width + gridSize - 1) / gridSize;
            int gridCountY = (chunkRect.height + gridSize - 1) / gridSize;

            // 存储有效网格的 AABB
            var validAABBs = new NativeList<Rect>(4, Allocator.Temp);

            // 遍历每个网格
            for (int gy = 0; gy < gridCountY; gy++)
            {
                for (int gx = 0; gx < gridCountX; gx++)
                {
                    // 计算当前网格的边界（世界坐标）
                    int gridMinX = chunkRect.x + gx * gridSize;
                    int gridMinY = chunkRect.y + gy * gridSize;
                    int gridMaxX = math.min(gridMinX + gridSize, chunkRect.MaxX);
                    int gridMaxY = math.min(gridMinY + gridSize, chunkRect.MaxY);

                    // 计算该网格内的有效像素 AABB
                    var aabb = CalculateGridAABB(gridMinX, gridMinY, gridMaxX, gridMaxY);

                    // 如果该网格包含有效像素，添加到列表
                    if (aabb.width > 0 && aabb.height > 0)
                    {
                        validAABBs.Add(aabb);
                    }
                }
            }

            // 如果没有有效区域,直接返回
            if (validAABBs.Length == 0)
            {                
                validAABBs.Dispose();
                return;
            }

            // 如果只有一个有效区域，直接替换
            if (validAABBs.Length == 1)
            {
                dirtyChunks[chunkIndex] = new DirtyChunk(validAABBs[0], true);
                validAABBs.Dispose();
                return;
            }

            // 多个有效区域：移除原 chunk，添加新的子 chunks
            var srcChunk = dirtyChunks[chunkIndex];
            dirtyChunks.RemoveAtSwapBack(chunkIndex);
            foreach (var aabb in validAABBs)
            {
                dirtyChunks.Add(new DirtyChunk(aabb, srcChunk.isDirty){notDirtyFrame = srcChunk.notDirtyFrame});
            }

            validAABBs.Dispose();
        }

        /// <summary>
        /// 计算指定矩形区域内有效像素的 AABB（世界坐标）
        /// </summary>
        [BurstCompile]
        private Rect CalculateGridAABB(int minX, int minY, int maxX, int maxY)
        {
            int aabbMinX = int.MaxValue;
            int aabbMinY = int.MaxValue;
            int aabbMaxX = int.MinValue;
            int aabbMaxY = int.MinValue;

            bool hasValidPixel = false;

            // 遍历网格内的所有像素
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    int worldIdx = worldConfig.CoordsToIdx(x, y);
                    PixelType pixelType = buffer[worldIdx].type;

                    // 如果是有效像素，更新 AABB
                    if (pixelType != PixelType.Empty && pixelType != PixelType.Disable)
                    {
                        hasValidPixel = true;
                        aabbMinX = math.min(aabbMinX, x);
                        aabbMinY = math.min(aabbMinY, y);
                        aabbMaxX = math.max(aabbMaxX, x);
                        aabbMaxY = math.max(aabbMaxY, y);
                    }
                }
            }

            // 如果没有有效像素，返回空矩形
            if (!hasValidPixel)
            {
                return new Rect(0, 0, 0, 0);
            }

            // 限制在世界边界内
            var worldBounds = new Rect(0, 0, worldConfig.width, worldConfig.height);
            var aabb = new Rect(
                aabbMinX,
                aabbMinY,
                aabbMaxX - aabbMinX + 1,
                aabbMaxY - aabbMinY + 1
            );

            return aabb.Clamp(worldBounds);
        }

        /// <summary>
        /// 判断两个区块是否相交（考虑 border）
        /// </summary>
        [BurstCompile]
        public bool IsIntersect(in DirtyChunk chunk1, in DirtyChunk chunk2)
        {
            return chunk1.rect.Intersects(chunk2.rect, chunkBorder);
        }

        public void Dispose()
        {
            if (dirtyChunks.IsCreated)
                dirtyChunks.Dispose();
        }
    }
}
