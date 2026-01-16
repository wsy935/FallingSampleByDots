using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "Pixel", menuName = "SO/Pixel")]
    public class PixelSO : ScriptableObject
    {
        public PixelType type;
        public Color32 color;
        public PixelType interactionMask;
    }

    [CreateAssetMenu(fileName = "PixelSet", menuName = "SO/PixelSet")]
    public class PixelSet : ScriptableObject
    {
        public PixelSO[] pixels;
    }
}