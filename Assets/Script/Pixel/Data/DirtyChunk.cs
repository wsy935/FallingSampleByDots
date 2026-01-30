using Unity.Mathematics;

namespace Pixel
{
    public struct DirtyChunk
    {
        public int minx, miny;
        public int2 size;

        public bool isDirty;
    }
}