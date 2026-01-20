using System;
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
        
        [NonSerialized]
        public FunctionPointer<SimulationHandler> handler;
            
        public abstract void ComplieHandler();
    }
}