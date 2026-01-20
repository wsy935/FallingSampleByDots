using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    /// <summary>
    /// 模拟处理委托类型    
    /// </summary>
    public delegate void SimulationHandler(int x, int y, ref SimulationContext context);

    /// <summary>
    /// 用于存放像素模拟函数的静态类，使用Partial关键字，以便可以在各个PixelSO文件中实现其模拟函数
    /// </summary>
    [BurstCompile]
    public static partial class PixelSimulation
    {
        /// <summary>
        /// 空像素模拟 - 不做任何处理
        /// </summary>
        [BurstCompile]
        public static void EmptySimulation(int x, int y, ref SimulationContext ctx)
        {
            // Empty 像素不需要任何行为
        }        
    }
}
