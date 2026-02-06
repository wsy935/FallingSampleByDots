using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "PixelSet", menuName = "SO/PixelSet")]
    public class PixelSet : ScriptableObject
    {
        public PixelConfig[] configs;                
    }        
}
