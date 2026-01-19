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
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.EmptySimulation);
        }
    }
}
