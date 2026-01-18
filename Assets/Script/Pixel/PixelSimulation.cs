using Unity.Burst;
using Unity.Entities;

namespace Pixel
{
    /// <summary>
    /// 模拟处理委托类型
    /// </summary>
    public delegate void SimulationHandler(int x, int y, ref DynamicBuffer<PixelBuffer> buffer, ref PixelChunk chunk);

    [BurstCompile]
    public static partial class PixelSimulation
    {
        [BurstCompile]
        public static void SandSimulation(int x, int y, ref DynamicBuffer<PixelBuffer> buffer, ref PixelChunk chunk)
        {

        }
    }
}