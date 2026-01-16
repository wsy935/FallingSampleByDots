using System;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    [Flags]
    public enum PixelType : int
    {
        Empty = 1 << 0,
        Sand = 1 << 1,
        Water = 1 << 2,
        Wall = 1 << 3
    }

    public partial struct PixelChunk : IComponentData
    {
        public int2 pos;   
        public int borderWidth;
        public int edgeSize;
        [MarshalAs(UnmanagedType.U1)]
        public bool isDirty;        
    }

    public partial struct PixelBuffer : IBufferElementData
    {
        public PixelType type;
    }
}
