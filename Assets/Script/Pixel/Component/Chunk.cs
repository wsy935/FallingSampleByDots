using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    public struct Chunk : IBufferElementData
    {
        public int2 pos;
        public bool isDirty;
        public bool isBlack;
        public int notDirtyFrame;
    }
}