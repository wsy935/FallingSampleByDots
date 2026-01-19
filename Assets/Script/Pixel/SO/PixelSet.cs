using Unity.Burst;
using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "PixelSet", menuName = "SO/PixelSet")]
    public class PixelSet : ScriptableObject
    {
        public PixelSO[] pixels;                
    }

    public abstract class PixelSO : ScriptableObject
    {
        public PixelType type;
        public Color32 color;
        public PixelType interactionMask;
        public FunctionPointer<SimulationHandler> handler;
    }
}