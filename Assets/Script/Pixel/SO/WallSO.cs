using Unity.Burst;
using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "Wall", menuName = "SO/Pixel/Wall")]
    public class WallPixelSO : PixelSO
    {
        void OnEnable()
        {
            type = PixelType.Wall;
            interactionMask = PixelType.Empty;
        }

        public override void ComplieHandler()
        {
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.EmptySimulation);
        }
    }
}
