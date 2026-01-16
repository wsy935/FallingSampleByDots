using Unity.Burst;
using Unity.Entities;

namespace Pixel
{
    /// <summary>
    /// 模拟处理委托类型
    /// </summary>
    public delegate void SimulationHandler(int x, int y, ref DynamicBuffer<PixelBuffer> buffer, ref PixelChunk chunk);

    /// <summary>
    /// 内置的模拟处理方法
    /// </summary>    
    [BurstCompile]
    public static class SimulationHandlers
    {        
        public static FunctionPointer<SimulationHandler> GetHandler(PixelType type)
        {
            SimulationHandler handler = type switch
            {
                PixelType.Sand => SandHandler,
                PixelType.Water => WaterHandler,
                PixelType.Wall => WallHandler,
                _ => EmptyHandler
            };
            return BurstCompiler.CompileFunctionPointer(handler);
        }
        /// <summary>
        /// 空类型处理（无操作）
        /// </summary>
        [BurstCompile]
        public static void EmptyHandler(int x, int y, ref DynamicBuffer<PixelBuffer> buffer,  ref PixelChunk chunk)
        {
            // 空类型不需要处理
        }

        /// <summary>
        /// 沙子模拟 - 向下掉落
        /// </summary>
        [BurstCompile]
        public static void SandHandler(int x, int y, ref DynamicBuffer<PixelBuffer> buffer,  ref PixelChunk chunk)
        {
            
        }

        /// <summary>
        /// 水模拟 - 流动扩散
        /// </summary>
        [BurstCompile]
        public static void WaterHandler(int x, int y, ref DynamicBuffer<PixelBuffer> buffer,  ref PixelChunk chunk)
        {            
        }

        /// <summary>
        /// 墙壁处理（静态，无操作）
        /// </summary>
        [BurstCompile]
        public static void WallHandler(int x, int y, ref DynamicBuffer<PixelBuffer> buffer,  ref PixelChunk chunk)
        {
            // 墙壁是静态的，不需要处理
        }
    }
}