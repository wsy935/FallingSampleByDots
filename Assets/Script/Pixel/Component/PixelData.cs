using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    public struct WhiteChunkTag : IComponentData { }
    public struct BlackChunkTag : IComponentData { }

    public struct PixelChunk : IComponentData
    {
        public int2 pos;
        [MarshalAs(UnmanagedType.U1)]
        public bool isDirty;
    }
    
    [InternalBufferCapacity(1024)]
    public struct PixelBuffer : IBufferElementData
    {
        public PixelType type;
        public uint lastFrame;
    }
}
