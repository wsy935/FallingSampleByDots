using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    
    public struct WorldConfig : IComponentData
    {
        public int width;
        public int height;
        public int chunkSize;
        public int2 chunkCnt;
        public Rect Size => new(0, 0, width, height);

        /// <summary>
        /// 世界坐标转化为下标        
        /// </summary>
        public int CoordsToIdx(int x, int y)
        {
            return y * width + x;
        }

        /// <summary>
        /// chunk位置转化为chunk的下标        
        /// </summary>
        public int GetChunkIdx(int x, int y)
        {
            int2 chunkPos = new(x / chunkSize, y / chunkSize);
            return chunkPos.y * chunkCnt.x + chunkPos.x;
        }

        /// <summary>
        /// 根据chunk的局部坐标获取世界位置
        /// </summary>        
        public int2 GetCoordsByChunk(int2 chunkPos,int x,int y)
        {
            int worldX = x + chunkPos.x * chunkSize;
            int worldY = y + chunkPos.y * chunkSize;
            return new(worldX, worldY);
        }

        /// <summary>
        /// 是否在世界中
        /// </summary>        
        public bool IsInWorld(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}