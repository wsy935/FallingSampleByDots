using Unity.Burst;
using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "Sand", menuName = "SO/Pixel/Sand")]
    public class SandPixelSO : PixelSO
    {
        void OnEnable()
        {
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.SandSimulation);
        }
    }
}