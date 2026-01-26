using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using Random = Unity.Mathematics.Random;
namespace Pixel
{
    /// <summary>
    /// 存储当前Chunk模拟所需使用的上下文,以及相关方法
    /// </summary>
    [BurstCompile]
    public struct SimulationContext
    {
        public DynamicBuffer<PixelBuffer> buffer;
        public WorldConfig worldConfig;
        public PixelConfig currentPixelConfig;
        public Random random;
        public uint frameIdx;

        public PixelChunk chunk;
        public NativeArray<Entity> chunkEntities;
        public BufferLookup<PixelBuffer> bufferLookup;
        public ComponentLookup<PixelChunk> chunkLookup;

        [BurstCompile]
        public int GetIndex(int x, int y) => worldConfig.CoordsToChunkIdx(x, y);

        /// <summary>
        /// 通过像素坐标获取像素数据，如果像素不在世界中，返回Disable
        /// </summary>
        [BurstCompile]
        public PixelBuffer GetPixel(int x, int y)
        {
            // 如果在本Chunk范围内，直接返回
            if (IsInBounds(x, y))
                return buffer[GetIndex(x, y)];

            // 计算邻居Chunk位置和相对坐标                        
            int2 neighborChunkPos = GetChunkPos(x, y);
            if (!IsChunkInWorld(neighborChunkPos)) return new PixelBuffer { type = PixelType.Disable };

            int2 localPos = GetNeighbourLocalPos(x, y);

            int entityIdx = worldConfig.ChunkPosToIdx(neighborChunkPos);
            var entity = chunkEntities[entityIdx];

            if (bufferLookup.TryGetBuffer(entity, out var neighborBuffer))
                return neighborBuffer[GetIndex(localPos.x, localPos.y)];

            return new PixelBuffer { type = PixelType.Disable };
        }

        [BurstCompile]
        private void SetPixel(int x, int y, PixelBuffer pixel)
        {
            if (pixel.type == PixelType.Disable) return;

            // 如果在本Chunk范围内，直接写入
            if (IsInBounds(x, y))
            {
                int idx = GetIndex(x, y);
                pixel.lastFrame = frameIdx;
                buffer[idx] = pixel;
                NotifyNeighbour(chunk.pos, x, y);
                return;
            }

            // 超出范围则写入邻居Chunk
            int2 neighborChunkPos = GetChunkPos(x, y);
            if (!IsChunkInWorld(neighborChunkPos)) return;

            int2 localPos = GetNeighbourLocalPos(x, y);

            int entityIdx = worldConfig.ChunkPosToIdx(neighborChunkPos);
            var entity = chunkEntities[entityIdx];

            // 写入邻居Chunk的Buffer
            if (bufferLookup.TryGetBuffer(entity, out var neighborBuffer))
            {
                int neighborIdx = GetIndex(localPos.x, localPos.y);
                pixel.lastFrame = frameIdx;
                neighborBuffer[neighborIdx] = pixel;
                
                var neighborChunk = chunkLookup[entity];
                neighborChunk.isDirty = true;
                chunkLookup[entity] = neighborChunk;
            }
        }

        /// <summary>
        /// 在像素处于边缘位置时，设置邻居Chunk Dirty为true
        /// </summary>
        [BurstCompile]
        private void NotifyNeighbour(int2 chunkPos, int x, int y)
        {
            if (x == 0)
                NotifyNeighbour(new(chunkPos.x - 1, chunkPos.y));
            else if (x == worldConfig.chunkEdge - 1)
                NotifyNeighbour(new(chunkPos.x + 1, chunkPos.y));

            if (y == 0)
                NotifyNeighbour(new(chunkPos.x, chunkPos.y - 1));
            else if (y == worldConfig.chunkEdge - 1)
                NotifyNeighbour(new(chunkPos.x, chunkPos.y + 1));
        }

        [BurstCompile]
        private void NotifyNeighbour(int2 chunkPos)
        {
            if (!IsChunkInWorld(chunkPos)) return;

            var idx = worldConfig.ChunkPosToIdx(chunkPos);
            var entity = chunkEntities[idx];
            var chunk = chunkLookup[entity];
            chunk.isDirty = true;
            chunkLookup[entity] = chunk;
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
            var pixel1 = GetPixel(x1, y1);
            var pixel2 = GetPixel(x2, y2);
            SetPixel(x1, y1, pixel2);
            SetPixel(x2, y2, pixel1);
        }

        /// <summary>
        /// 检查坐标是否在当前Chunk范围内
        /// </summary>
        [BurstCompile]
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < worldConfig.chunkEdge &&
                   y >= 0 && y < worldConfig.chunkEdge;
        }

        /// <summary>
        /// 通过像素坐标计算Chunk坐标，在像素超出当前块范围时为邻居坐标，否则为当前坐标
        /// </summary>
        [BurstCompile]
        private int2 GetChunkPos(int x, int y)
        {
            int2 neighborChunkPos = chunk.pos;

            if (x < 0)
            {
                neighborChunkPos.x--;
            }
            else if (x >= worldConfig.chunkEdge)
            {
                neighborChunkPos.x++;
            }

            if (y < 0)
            {
                neighborChunkPos.y--;
            }
            else if (y >= worldConfig.chunkEdge)
            {
                neighborChunkPos.y++;
            }
            return neighborChunkPos;
        }

        [BurstCompile]
        private int2 GetNeighbourLocalPos(int x, int y)
        {
            int localX = x, localY = y;
            if (x < 0)
                localX = worldConfig.chunkEdge + x;
            else if (x >= worldConfig.chunkEdge)
                localX = x - worldConfig.chunkEdge;

            if (y < 0)
                localY = worldConfig.chunkEdge + y;
            else if (y >= worldConfig.chunkEdge)
                localY = y - worldConfig.chunkEdge;
            return new(localX, localY);
        }

        [BurstCompile]
        public bool IsChunkInWorld(int2 chunkPos)
        {
            return chunkPos.x >= 0 && chunkPos.x < worldConfig.chunkCnt.x &&
                chunkPos.y >= 0 && chunkPos.y < worldConfig.chunkCnt.y;
        }

        /// <summary>
        /// 检查是否可以与目标像素交互
        /// </summary>
        [BurstCompile]
        public bool CanInteract(int x, int y)
        {
            var targetPixel = GetPixel(x, y);
            if (targetPixel.type == PixelType.Disable) return false;

            return (currentPixelConfig.interactionMask & targetPixel.type) != 0;
        }
    }
}
