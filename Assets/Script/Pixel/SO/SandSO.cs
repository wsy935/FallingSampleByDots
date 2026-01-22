using Unity.Burst;
using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "Sand", menuName = "SO/Pixel/Sand")]
    public class SandPixelSO : PixelSO
    {
        void OnEnable()
        {
            type = PixelType.Sand;
            interactionMask = PixelType.Empty | PixelType.Water;
        }

        public override void ComplieHandler()
        {
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.SandSimulation);
        }
    }

    public static partial class PixelSimulation
    {
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
                int dir = ctx.random.NextBool() ? -1 : 1;
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
    }
}
