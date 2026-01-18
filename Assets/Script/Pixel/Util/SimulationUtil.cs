using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    [BurstCompile]
    public static class SimulationUtil
    {
        [BurstCompile]
        public static int CoordsToIdx(int x, int y, int edge)
        {
            return x + y * edge;
        }

        [BurstCompile]
        public static void TryMove(int x, int y, in int2 direction, ref DynamicBuffer<PixelBuffer> buffer)
        {

        }
    }
}