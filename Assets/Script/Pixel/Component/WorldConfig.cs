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
        public int chunkSize;
        public int2 chunkCnt;
        public Rect Size => new(0, 0, width, height);

        [BurstCompile]
        public int CoordsToIdx(int x, int y)
        {
            return y * width + x;
        }
    
        [BurstCompile]           
        public int GetChunkIdx(int x,int y)
        {
            int2 chunkPos = new(x / chunkSize, y  / chunkSize);
            return chunkPos.y * chunkCnt.x + chunkPos.x;
        }

        [BurstCompile]
        public int2 GetCoordsByChunk(int2 chunkPos,int x,int y)
        {
            int worldX = x + chunkPos.x * chunkSize;
            int worldY = y + chunkPos.y * chunkSize;
            return new(worldX, worldY);
        }

        [BurstCompile]
        public bool IsInWorld(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}