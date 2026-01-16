using Unity.Entities;

namespace Pixel
{
    public partial struct PixelSOConfig : IComponentData
    {
        public PixelType type;
        public PixelType interactionMask;
    }
}