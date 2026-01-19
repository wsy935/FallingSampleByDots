using Unity.Burst;
using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "Water", menuName = "SO/Pixel/Water")]
    public class WaterPixelSO : PixelSO
    {
        void OnEnable()
        {
            type = PixelType.Water;
            interactionMask = PixelType.Empty;            
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.WaterSimulation);
        }
    }
}
