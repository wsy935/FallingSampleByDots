using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Pixel
{
    [Flags]
    public enum PixelType : int
    {
        /// <summary>
        /// 不可用
        /// </summary>
        Disable = 1 << 0,
        Empty = 1 << 1,
        Sand = 1 << 2,
        Water = 1 << 3,
        Wall = 1 << 4,
        NotReact = Empty | Wall
    }

    public enum MaterialType
    {
        Nothing = 0,
        Solid = 1,
        Liquid = 2,
        Gas = 3
    }

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
