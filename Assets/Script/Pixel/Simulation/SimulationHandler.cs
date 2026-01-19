using Unity.Burst;
using Unity.Entities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Pixel
{
    /// <summary>
    /// 模拟处理委托类型
    /// </summary>
    public delegate void SimulationHandler(int x, int y, ref SimulationContext context);

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

        /// <summary>
        /// 墙像素模拟 - 静态不动
        /// </summary>
        [BurstCompile]
        public static void WallSimulation(int x, int y, ref SimulationContext ctx)
        {
            // Wall 像素是静态的，不需要移动
        }

        /// <summary>
        /// 沙子像素模拟 - 向下掉落，可以斜向滑落
        /// </summary>
        [BurstCompile]
        public static void SandSimulation(int x, int y, ref SimulationContext ctx)
        {
            // 尝试向下移动
            if (ctx.TryMoveOrSwap(x, y, x, y - 1)) return;

            bool canLeft = ctx.CanInteract(x - 1, y - 1);
            bool canRight = ctx.CanInteract(x + 1, y - 1);

            // 尝试斜向下移动
            if (canLeft && canRight)
            {
                int dir = Random.Range(0, 2) == 0 ? -1 : 1;
                ctx.Swap(x, y, x + dir, y - 1);
            }
            else if (canLeft)
            {
                ctx.Swap(x, y, x - 1, y - 1);
            }
            else if (canRight)
            {
                ctx.Swap(x, y, x + 1, y - 1);
            }
        }

        /// <summary>
        /// 水像素模拟 - 向下流动，横向扩散
        /// </summary>
        [BurstCompile]
        public static void WaterSimulation(int x, int y, ref SimulationContext ctx)
        {
            // 尝试向下移动
            if (ctx.TryMoveOrSwap(x, y, x, y - 1)) return;

            // 尝试斜向下移动
            bool canDownLeft = ctx.CanInteract(x - 1, y - 1);
            bool canDownRight = ctx.CanInteract(x + 1, y - 1);

            if (canDownLeft && canDownRight)
            {
                int dir = Random.Range(0, 2) == 0 ? -1 : 1;
                if (ctx.TryMoveOrSwap(x, y, x + dir, y - 1)) return;
            }
            else if (canDownLeft)
            {
                if (ctx.TryMoveOrSwap(x, y, x - 1, y - 1)) return;
            }
            else if (canDownRight)
            {
                if (ctx.TryMoveOrSwap(x, y, x + 1, y - 1)) return;
            }

            // 水平扩散（水的特性）
            bool canLeft = ctx.CanInteract(x - 1, y);
            bool canRight = ctx.CanInteract(x + 1, y);

            if (canLeft && canRight)
            {
                int dir = Random.Range(0, 2) == 0 ? -1 : 1;
                ctx.TryMoveOrSwap(x, y, x + dir, y);
            }
            else if (canLeft)
            {
                ctx.TryMoveOrSwap(x, y, x - 1, y);
            }
            else if (canRight)
            {
                ctx.TryMoveOrSwap(x, y, x + 1, y);
            }
        }
    }
}
