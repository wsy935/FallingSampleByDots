using Unity.Burst;
using UnityEngine;

using Random = Unity.Mathematics.Random;
namespace Pixel
{
    [CreateAssetMenu(fileName = "Water", menuName = "SO/Pixel/Water")]
    public class WaterPixelSO : PixelSO
    {
        void OnEnable()
        {
            type = PixelType.Water;
            interactionMask = PixelType.Empty;
        }

        public override void ComplieHandler()
        {
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.WaterSimulation);
        }
    }

    public static partial class PixelSimulation
    {
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
                int dir = ctx.random.NextBool() ? -1 : 1;
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
                int dir = ctx.random.NextBool() ? -1 : 1;
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
