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
            interactionMask = PixelType.Empty & PixelType.Water;
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.SandSimulation);
        }
    }
}