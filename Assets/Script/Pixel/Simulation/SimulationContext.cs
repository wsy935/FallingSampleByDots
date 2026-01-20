using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    /// <summary>
    /// 存储当前Chunk模拟所需使用的上下文,以及相关方法
    /// </summary>
    [BurstCompile]
    public struct SimulationContext
    {
        public DynamicBuffer<PixelBuffer> buffer;
        public ChunkConfig chunkConfig;
        public PixelConfig currentPixelConfig;
        public Random random;

        // 用于跨Chunk访问
        public int2 chunkPos;
        public NativeArray<Entity> chunkEntities;
        public BufferLookup<PixelBuffer> bufferLookup;
        public ComponentLookup<PixelChunk> chunkLookup; // 用于标记邻居Chunk为dirty

        [BurstCompile]
        public int GetIndex(int x, int y) => chunkConfig.CoordsToIdx(x, y);

        [BurstCompile]
        public PixelBuffer GetPixel(int x, int y)
        {
            // 如果在本Chunk范围内，直接返回
            if (x >= 0 && x < chunkConfig.edge && y >= 0 && y < chunkConfig.edge)
                return buffer[GetIndex(x, y)];

            // 计算邻居Chunk位置和相对坐标
            int2 neighborChunkPos = chunkPos;
            int localX = x, localY = y;

            if (x < 0)
            {
                neighborChunkPos.x--;
                localX = chunkConfig.edge + x;
            }
            else if (x >= chunkConfig.edge)
            {
                neighborChunkPos.x++;
                localX = x - chunkConfig.edge;
            }

            if (y < 0)
            {
                neighborChunkPos.y--;
                localY = chunkConfig.edge + y;
            }
            else if (y >= chunkConfig.edge)
            {
                neighborChunkPos.y++;
                localY = y - chunkConfig.edge;
            }

            // 边界检查
            if (neighborChunkPos.x < 0 || neighborChunkPos.x >= chunkConfig.chunkCount.x ||
                neighborChunkPos.y < 0 || neighborChunkPos.y >= chunkConfig.chunkCount.y)
                return new PixelBuffer { type = PixelType.Empty };

            // 使用Array索引直接访问
            int entityIdx = chunkConfig.ChunkPosToIdx(neighborChunkPos);
            var entity = chunkEntities[entityIdx];

            if (bufferLookup.TryGetBuffer(entity, out var neighborBuffer))
                return neighborBuffer[localY * chunkConfig.edge + localX];

            return new PixelBuffer { type = PixelType.Empty };
        }

        [BurstCompile]
        public void SetPixel(int x, int y, PixelBuffer pixel)
        {
            // 如果在本Chunk范围内，直接写入
            if (x >= 0 && x < chunkConfig.edge && y >= 0 && y < chunkConfig.edge)
            {
                buffer[GetIndex(x, y)] = pixel;
                return;
            }

            // 超出范围则写入邻居Chunk
            int2 neighborChunkPos = chunkPos;
            int localX = x, localY = y;

            if (x < 0)
            {
                neighborChunkPos.x--;
                localX = chunkConfig.edge + x;
            }
            else if (x >= chunkConfig.edge)
            {
                neighborChunkPos.x++;
                localX = x - chunkConfig.edge;
            }

            if (y < 0)
            {
                neighborChunkPos.y--;
                localY = chunkConfig.edge + y;
            }
            else if (y >= chunkConfig.edge)
            {
                neighborChunkPos.y++;
                localY = y - chunkConfig.edge;
            }

            // 边界检查
            if (neighborChunkPos.x < 0 || neighborChunkPos.x >= chunkConfig.chunkCount.x ||
                neighborChunkPos.y < 0 || neighborChunkPos.y >= chunkConfig.chunkCount.y)
                return;

            // 使用Array索引直接访问
            int entityIdx = chunkConfig.ChunkPosToIdx(neighborChunkPos);
            var entity = chunkEntities[entityIdx];

            // 写入邻居Chunk的Buffer
            if (bufferLookup.TryGetBuffer(entity, out var neighborBuffer))
            {
                neighborBuffer[localY * chunkConfig.edge + localX] = pixel;

                // 标记邻居Chunk为dirty，确保下一帧会继续模拟
                if (chunkLookup.HasComponent(entity))
                {
                    var neighborChunk = chunkLookup[entity];
                    neighborChunk.isDirty = true;
                    chunkLookup[entity] = neighborChunk;
                }
            }
        }

        [BurstCompile]
        public bool TryMoveOrSwap(int x1, int y1, int x2, int y2)
        {
            // 允许跨Chunk移动
            if (!CanInteract(x2, y2)) return false;
            Swap(x1, y1, x2, y2);
            return true;
        }

        [BurstCompile]
        public void Swap(int x1, int y1, int x2, int y2)
        {
            // 如果两个位置都在本Chunk内，直接交换
            bool inBounds1 = x1 >= 0 && x1 < chunkConfig.edge && y1 >= 0 && y1 < chunkConfig.edge;
            bool inBounds2 = x2 >= 0 && x2 < chunkConfig.edge && y2 >= 0 && y2 < chunkConfig.edge;

            if (inBounds1 && inBounds2)
            {
                int idx1 = GetIndex(x1, y1);
                int idx2 = GetIndex(x2, y2);
                (buffer[idx1], buffer[idx2]) = (buffer[idx2], buffer[idx1]);
            }
            else
            {
                // 跨Chunk交换：先读取两个像素，然后分别写入
                var pixel1 = GetPixel(x1, y1);
                var pixel2 = GetPixel(x2, y2);
                SetPixel(x1, y1, pixel2);
                SetPixel(x2, y2, pixel1);
            }
        }

        /// <summary>
        /// 检查坐标是否在当前Chunk范围内
        /// </summary>
        [BurstCompile]
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < chunkConfig.edge &&
                   y >= 0 && y < chunkConfig.edge;
        }

        /// <summary>
        /// 检查是否可以与目标像素交互（支持跨Chunk）
        /// </summary>
        [BurstCompile]
        public bool CanInteract(int x, int y)
        {
            // 不再检查IsInBounds，允许跨Chunk交互
            var targetPixel = GetPixel(x, y);
            return (currentPixelConfig.interactionMask & targetPixel.type) != 0;
        }
    }
}
