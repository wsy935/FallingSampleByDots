using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    [BurstCompile]
    public struct DirtyChunkManager : IComponentData,IDisposable
    {
        private NativeList<DirtyChunk> dirtyChunks;
        //像素缓存数组
        private NativeArray<PixelData> buffer;        
        //当块的大小超过maxChunkSize时，尝试拆分块
        private int maxChunkSize;
        //最小块尺寸，避免过度碎片化
        private int minChunkSize;
        private WorldConfig worldConfig;
        //当块与块之间的间距小于chunkBorder时，尝试合并块
        private int chunkBorder;

        public DirtyChunkManager(Allocator allocator, NativeArray<PixelData> buffer, WorldConfig worldConfig, int maxChunkSize = 256, int minChunkSize = 4, int chunkBorder = 1)
        {
            dirtyChunks = new(allocator);            

            this.buffer = buffer;
            this.worldConfig = worldConfig;
            this.maxChunkSize = maxChunkSize;
            this.minChunkSize = minChunkSize;
            this.chunkBorder = chunkBorder;
        }

        public readonly NativeList<DirtyChunk> GetDirtyChunks() => dirtyChunks;

        public void AddChunk(DirtyChunk dirtyChunk)
        {
            dirtyChunks.Add(dirtyChunk);
        }

        /// <summary>
        /// 移除非脏的区块
        /// </summary>
        [BurstCompile]
        public void Clear()
        {
            for (int i = 0; i < dirtyChunks.Length; i++)
            {
                if (!dirtyChunks[i].isDirty && dirtyChunks[i].notDirtyFrame > 1)
                {
                    dirtyChunks.RemoveAtSwapBack(i);
                    i--;
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
                var chunk1 = dirtyChunks.ElementAt(i);
                while (j < dirtyChunks.Length)
                {
                    ref var chunk2 = ref dirtyChunks.ElementAt(j);
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

            dirtyChunks[i] = new DirtyChunk(newRect, true);
            dirtyChunks.RemoveAt(j);
        }

        /// <summary>
        /// 分割超过最大尺寸的脏区块
        /// </summary>
        [BurstCompile]
        public void SplitChunk()
        {
            NativeList<int> splitChunks = new(Allocator.Temp);
            for (int i = 0; i < dirtyChunks.Length; i++)
            {
                var curSize = dirtyChunks[i].rect.Size;
                if (curSize.x >= maxChunkSize || curSize.y >= maxChunkSize)
                {
                    splitChunks.Add(i);
                }
            }

            // 从后往前处理，避免索引变化问题
            for (int i = splitChunks.Length - 1; i >= 0; i--)
            {
                int idx = splitChunks[i];
                SplitChunk_Impl(idx);
            }

            splitChunks.Dispose();
        }

        /// <summary>
        /// 使用并查集分析连通区域并分割单个chunk
        /// </summary>
        [BurstCompile]
        private void SplitChunk_Impl(int chunkIndex)
        {
            var chunk = dirtyChunks[chunkIndex];
            var chunkRect = chunk.rect;

            int chunkWidth = chunkRect.width;
            int chunkHeight = chunkRect.height;
            int totalPixels = chunkWidth * chunkHeight;

            // 创建并查集和 AABB 追踪器
            var unionFind = new UnionFind(totalPixels, Allocator.Temp);
            var aabbTracker = new NativeHashMap<int, Rect>(16, Allocator.Temp);

            // 八个方向的偏移量
            var directions = new NativeArray<int2>(8, Allocator.Temp);
            directions[0] = new int2(-1, 0);  // 左
            directions[1] = new int2(1, 0);   // 右
            directions[2] = new int2(0, -1);  // 下
            directions[3] = new int2(0, 1);   // 上
            directions[4] = new int2(-1, -1); // 左下
            directions[5] = new int2(-1, 1);  // 左上
            directions[6] = new int2(1, -1);  // 右下
            directions[7] = new int2(1, 1);   // 右上

            // 单次遍历：构建并查集并同时计算 AABB
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkWidth; x++)
                {
                    int localIdx = y * chunkWidth + x;

                    // 获取世界坐标的像素类型
                    int worldX = chunkRect.x + x;
                    int worldY = chunkRect.y + y;
                    int worldIdx = worldConfig.CoordsToIdx(worldX, worldY);
                    PixelType pixelType = buffer[worldIdx].type;

                    // 如果是空像素，跳过
                    if (pixelType == PixelType.Empty || pixelType == PixelType.Disable)
                        continue;

                    // 检查八个方向的邻居
                    for (int d = 0; d < 8; d++)
                    {
                        int2 dir = directions[d];
                        int neighborX = x + dir.x;
                        int neighborY = y + dir.y;

                        // 检查是否在 chunk 范围内
                        if (neighborX >= 0 && neighborX < chunkWidth &&
                            neighborY >= 0 && neighborY < chunkHeight)
                        {
                            int neighborLocalIdx = neighborY * chunkWidth + neighborX;
                            int neighborWorldX = chunkRect.x + neighborX;
                            int neighborWorldY = chunkRect.y + neighborY;
                            int neighborWorldIdx = worldConfig.CoordsToIdx(neighborWorldX, neighborWorldY);
                            PixelType neighborPixelType = buffer[neighborWorldIdx].type;

                            if (neighborPixelType != PixelType.Empty && neighborPixelType != PixelType.Disable)
                            {
                                unionFind.Union(localIdx, neighborLocalIdx);
                            }
                        }
                    }

                    // 获取当前像素的根节点（使用路径压缩后的结果）
                    int root = unionFind.Find(localIdx);

                    // 更新或创建该根节点的 AABB
                    if (aabbTracker.TryGetValue(root, out Rect existingRect))
                    {
                        // 扩展现有 AABB
                        int minX = math.min(existingRect.x, worldX);
                        int minY = math.min(existingRect.y, worldY);
                        int maxX = math.max(existingRect.MaxX, worldX + 1);
                        int maxY = math.max(existingRect.MaxY, worldY + 1);
                        
                        aabbTracker[root] = new Rect(minX, minY, maxX - minX, maxY - minY);
                    }
                    else
                    {
                        // 创建新的 AABB
                        aabbTracker.Add(root, new Rect(worldX, worldY, 1, 1));
                    }
                }
            }

            // 由于并查集的合并操作，需要重新整合 AABB
            // 将所有 AABB 根据最终的根节点重新归并
            var finalAABBMap = new NativeHashMap<int, Rect>(16, Allocator.Temp);

            foreach (var kvp in aabbTracker)
            {
                int originalRoot = kvp.Key;
                Rect aabb = kvp.Value;
                
                // 找到最终的根节点
                int finalRoot = unionFind.Find(originalRoot);
                
                if (finalAABBMap.TryGetValue(finalRoot, out Rect existingRect))
                {
                    // 合并 AABB
                    finalAABBMap[finalRoot] = Rect.Union(existingRect, aabb);
                }
                else
                {
                    finalAABBMap.Add(finalRoot, aabb);
                }
            }

            // 过滤掉不满足最小尺寸的区域
            var validChunks = new NativeList<Rect>(finalAABBMap.Count, Allocator.Temp);
            var worldBounds = new Rect(0, 0, worldConfig.width, worldConfig.height);

            foreach (var kvp in finalAABBMap)
            {
                var aabb = kvp.Value;
                
                // 检查尺寸是否满足最小要求
                if (aabb.width >= minChunkSize && aabb.height >= minChunkSize)
                {
                    // 限制在世界边界内
                    aabb = aabb.Clamp(worldBounds);
                    validChunks.Add(aabb);
                }
            }

            // 如果分割后只有一个或零个有效区域，不进行分割
            if (validChunks.Length <= 1)
            {
                directions.Dispose();
                unionFind.Dispose();
                aabbTracker.Dispose();
                finalAABBMap.Dispose();
                validChunks.Dispose();
                return;
            }

            // 移除原 chunk
            dirtyChunks.RemoveAtSwapBack(chunkIndex);

            // 添加新的子 chunks
            for (int i = 0; i < validChunks.Length; i++)
            {
                var newChunk = new DirtyChunk(validChunks[i], true);
                dirtyChunks.Add(newChunk);
            }

            directions.Dispose();
            unionFind.Dispose();
            aabbTracker.Dispose();
            finalAABBMap.Dispose();
            validChunks.Dispose();
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
