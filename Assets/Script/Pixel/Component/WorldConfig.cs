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

        [BurstCompile]
        public int CoordsToIdx(int x, int y)
        {
            return y * width + x;
        }
        [BurstCompile]
        public bool IsInWorld(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}