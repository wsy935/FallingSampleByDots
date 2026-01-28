using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    [BurstCompile]
    public struct WorldConfig : IComponentData
    {
        public int width;
        public int height;
        public int2 chunkCnt;
        public int chunkEdge;

        [BurstCompile]
        public int2 WorldIdxToChunkPos(int x, int y) => new(x / chunkEdge, y / chunkEdge);


        [BurstCompile]
        public int2 WorldIdxToLocalPos(int x, int y) => new(x % chunkEdge, y % chunkEdge);

        [BurstCompile]
        public int CoordsToChunkIdx(int x, int y)
        {
            return y * chunkEdge + x;
        }

        [BurstCompile]
        public int CoordsToWorldIdx(int x, int y, int2 chunkPos)
        {
            int worldX = chunkPos.x * chunkEdge + x;
            int worldY = chunkPos.y * chunkEdge + y;
            return worldY * width + worldX;
        }

        [BurstCompile]
        public int ChunkPosToIdx(int2 chunkPos)
        {
            return chunkPos.y * chunkCnt.x + chunkPos.x;
        }
    }
}