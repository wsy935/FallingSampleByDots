using Unity.Burst;
using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "Empty", menuName = "SO/Pixel/Empty")]
    public class EmptyPixelSO : PixelSO
    {
        void OnEnable()
        {
            type = PixelType.Empty;
            interactionMask = PixelType.Empty;            
        }

        public override void ComplieHandler()
        {
            handler = BurstCompiler.CompileFunctionPointer<SimulationHandler>(PixelSimulation.EmptySimulation);
        }
    }
}
